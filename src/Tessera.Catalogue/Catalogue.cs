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

    /// <summary>The layer as the tile query needs it.</summary>
    public LayerSource Source => new(Table, Geometry, Srid, Properties, Id);
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
    /// <summary>The query that lists the layers: <see cref="CatalogueQueries.List"/>, in Sql.</summary>
    public static Query ListQuery => CatalogueQueries.List;

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
            // System.Text.Json reports text that is not JSON by throwing, so the parse alone sits in a try.
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
