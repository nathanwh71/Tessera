using System.Collections.Immutable;
using Tessera.Kernel;
using Tessera.Sql;

namespace Tessera.Catalogue;

/// <summary>
/// One table or view the service may serve as map tiles: a layer (US-03).
/// </summary>
/// <param name="Table">The schema and table, quoted for SQL; its <c>Id</c> is the layer's id, such as <c>food.cafes</c>.</param>
/// <param name="Geometry">The column that holds each place's shape.</param>
/// <param name="Srid">
/// The number that names the layer's coordinate system: 4326 for longitude and latitude in
/// degrees (WGS 84), 3857 for metres on the web map's flat square (Web Mercator).
/// </param>
/// <param name="GeometryType">The kind of shape, such as POINT.</param>
/// <param name="Properties">The other columns, which each tile carries beside the shape (US-07).</param>
public sealed record Layer(TableName Table, QuotedName Geometry, int Srid, string GeometryType, ImmutableArray<QuotedName> Properties)
{
    public string Id => Table.Id;
}

/// <summary>
/// One row of the database's own list of tables, before its names are quoted. Dapper fills it by
/// matching each column's name to a parameter's name.
/// </summary>
/// <remarks>
/// Columns arrives as a JSON list of names, such as <c>["layer","name"]</c>: Dapper cannot fill a
/// list from a database array, but it can fill text, and JSON keeps any name intact.
/// </remarks>
public sealed record CatalogueRow(string Schema, string Name, string GeometryColumn, int Srid, string GeometryType, string Columns);

/// <summary>
/// The list of layers the service builds at start-up: every table and view granted to its role
/// (US-03). The database keeps the guest list; the service only reads it.
/// </summary>
public sealed record LayerCatalogue(ImmutableDictionary<string, Layer> Layers)
{
    /// <summary>
    /// The query that lists every table and view with a typed geometry column that the signed-in
    /// role may read.
    /// </summary>
    /// <remarks>
    /// pg_class lists tables and views; relkind tells them apart: 'r' a table, 'p' a partitioned
    /// table, 'v' a view, 'm' a view that stores its rows. A function never appears in pg_class, so
    /// no function becomes a layer (US-03 c5). has_table_privilege asks the database, as the
    /// service's own role, whether a grant lets it read the table, so the database decides what is
    /// published. A typed column carries its SRID in its "type modifier", atttypmod, and an untyped
    /// one carries 0, so the last condition leaves untyped columns out.
    /// DISTINCT ON keeps one geometry column for each table: the first, by column number.
    /// </remarks>
    public static readonly Query ListQuery = Query.Of($"""
        SELECT DISTINCT ON (n.nspname, c.relname)
               n.nspname::text AS schema,
               c.relname::text AS name,
               a.attname::text AS geometrycolumn,
               postgis_typmod_srid(a.atttypmod) AS srid,
               postgis_typmod_type(a.atttypmod) AS geometrytype,
               to_json(ARRAY(SELECT p.attname::text
                     FROM pg_attribute p
                     JOIN pg_type pt ON pt.oid = p.atttypid
                     WHERE p.attrelid = c.oid AND p.attnum > 0 AND NOT p.attisdropped AND p.attnum <> a.attnum
                       AND pt.typname IN ('text', 'varchar', 'bpchar', 'int2', 'int4', 'int8', 'float4', 'float8', 'numeric', 'bool')
                     ORDER BY p.attnum))::text AS columns
        FROM pg_class c
        JOIN pg_namespace n ON n.oid = c.relnamespace
        JOIN pg_attribute a ON a.attrelid = c.oid AND a.attnum > 0 AND NOT a.attisdropped
        JOIN pg_type t ON t.oid = a.atttypid AND t.typname = 'geometry'
        WHERE c.relkind IN ('r', 'p', 'v', 'm')
          AND n.nspname NOT IN ('pg_catalog', 'information_schema')
          AND has_schema_privilege(n.oid, 'USAGE')
          AND has_table_privilege(c.oid, 'SELECT')
          AND postgis_typmod_srid(a.atttypmod) > 0
        ORDER BY n.nspname, c.relname, a.attnum
        """);

    /// <summary>
    /// Turns one row into a layer, quoting every name on the way in, so each name that later
    /// reaches SQL has passed through <see cref="QuotedName.Quote"/> (US-03, S-02).
    /// </summary>
    public static Result<Layer, string> ToLayer(CatalogueRow row) =>
        from table in TableName.Of(row.Schema, row.Name)
        from geometry in QuotedName.Quote(row.GeometryColumn)
        from names in ColumnNames(row.Columns)
        from properties in names.Traverse(QuotedName.Quote)
        select new Layer(table, geometry, row.Srid, row.GeometryType, properties);

    private static Result<ImmutableArray<string>, string> ColumnNames(string json)
    {
        try
        {
            return System.Text.Json.JsonSerializer.Deserialize<ImmutableArray<string>>(json) is { IsDefault: false } names
                ? Result.Ok<ImmutableArray<string>, string>(names)
                : Result.Refuse<ImmutableArray<string>, string>("expected a list of column names");
        }
        catch (System.Text.Json.JsonException error)
        {
            // System.Text.Json reports bad JSON by throwing; this is the one place that catches it.
            return Result.Refuse<ImmutableArray<string>, string>(error.Message);
        }
    }

    /// <summary>Builds the catalogue from rows; a row whose names will not quote is left out.</summary>
    public static LayerCatalogue FromRows(IEnumerable<CatalogueRow> rows) =>
        new(rows
            .SelectMany(row => ToLayer(row).Match(layer => new[] { layer }, _ => []))
            .ToImmutableDictionary(layer => layer.Id, StringComparer.Ordinal));

    /// <summary>
    /// Finds a layer by id. The match is exact and case-sensitive: <c>food.cafes</c> is a layer,
    /// <c>Food.Cafes</c> is not, as the database itself treats quoted names.
    /// </summary>
    public Option<Layer> Find(string id) => Layers.GetValueOrDefault(id).ToOption();

    /// <summary>Reads the database's list of tables as the service's role.</summary>
    public static async Task<Result<LayerCatalogue, DatabaseError>> Load(Database database, CancellationToken cancel = default) =>
        (await database.Rows<CatalogueRow>(ListQuery, cancel).ConfigureAwait(false)).Map(rows => FromRows(rows));
}
