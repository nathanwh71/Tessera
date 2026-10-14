using System.Collections.Immutable;
using Tessera.Kernel;
using Tessera.Sql;

// The Catalogue module's list of layers: the tables and views the service may publish as map tiles
// (US-03). LayerCatalogue builds the list from the database's own records; LiveCatalogue, in this
// module, rereads it now and then, and the Tiles module looks up each requested layer in it.

namespace Tessera.Catalogue;

/// <summary>
/// One table or view (a saved query that reads like a table) the service may serve as map tiles: a
/// layer (US-03).
/// </summary>
/// <param name="Table">
/// The schema, a named folder of tables, and the table, quoted for SQL; its <c>Id</c> is the layer's
/// id, such as <c>food.cafes</c>.
/// </param>
/// <param name="Geometry">The column that holds each place's shape.</param>
/// <param name="Srid">
/// The number that names the layer's coordinate system: 4326 for longitude and latitude in
/// degrees (WGS 84), 3857 for metres on the web map's flat square (Web Mercator).
/// </param>
/// <param name="GeometryType">The kind of shape, such as POINT.</param>
/// <param name="Properties">The other columns, which each tile carries beside the shape (US-07).</param>
/// <param name="Order">
/// The primary key's columns, those whose values name each row, which fix the order of features in a
/// tile; empty for a view.
/// </param>
public sealed record Layer(TableName Table, QuotedName Geometry, int Srid, string GeometryType, ImmutableArray<QuotedName> Properties, ImmutableArray<QuotedName> Order)
{
    public string Id => Table.Id;

    /// <summary>The layer as the tile query in the Sql module needs it.</summary>
    public LayerSource Source => new(Table, Geometry, Srid, Properties, Id, Order);
}

/// <summary>
/// One row of the database's own list of tables, before its names are quoted. Dapper, the library
/// that turns rows into C# values, fills it by matching each column's name to a parameter's name.
/// </summary>
/// <remarks>
/// Columns arrives as a JSON list of names, such as <c>["layer","name"]</c>: Dapper cannot fill a
/// list from a database array, but it can fill text, and JSON keeps any name intact. KeyColumns
/// arrives the same way.
/// </remarks>
public sealed record CatalogueRow(string Schema, string Name, string GeometryColumn, int Srid, string GeometryType, string Columns, string KeyColumns = "[]");

/// <summary>
/// The list of layers: every table and view granted to the service's role, the user name it signs in
/// as (US-03). The database keeps the guest list; the service only reads it.
/// </summary>
/// <remarks>
/// Each list, once built, never changes; <see cref="LiveCatalogue"/> builds a new one to take in a
/// change.
/// </remarks>
public sealed record LayerCatalogue(ImmutableDictionary<string, Layer> Layers)
{
    /// <summary>The query that lists the layers: <see cref="CatalogueQueries.List"/>, in Sql.</summary>
    public static Query ListQuery => CatalogueQueries.List;

    /// <summary>
    /// Turns one row into a layer, quoting every name on the way in, so each name that later
    /// reaches SQL has passed through <see cref="QuotedName.Quote"/> (US-03, S-02). Gives the reason
    /// when a name will not quote or a list of names is not JSON.
    /// </summary>
    public static Result<Layer, string> ToLayer(CatalogueRow row) =>
        from table in TableName.Of(row.Schema, row.Name)
        from geometry in QuotedName.Quote(row.GeometryColumn)
        from names in ColumnNames(row.Columns)
        from properties in names.Traverse(QuotedName.Quote)
        from keys in ColumnNames(row.KeyColumns)
        from order in keys.Traverse(QuotedName.Quote)
        select new Layer(table, geometry, row.Srid, row.GeometryType, properties, order);

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
            // System.Text.Json reports text that is not JSON by throwing an exception, an error that
            // jumps out of the normal flow, so the parse alone sits in a try.
            return Result.Refuse<ImmutableArray<string>, string>(error.Message);
        }
    }

    /// <summary>Builds the catalogue from rows; a row whose names will not quote is left out.</summary>
    public static LayerCatalogue FromRows(IEnumerable<CatalogueRow> rows) =>
        new(rows
            .SelectMany(row => ToLayer(row).Match(layer => new[] { layer }, _ => []))
            .ToImmutableDictionary(layer => layer.Id, StringComparer.Ordinal));

    /// <summary>
    /// Finds a layer by id. The match is exact and case-sensitive, as the database itself treats
    /// quoted names: <c>food.cafes</c> is a layer, <c>Food.Cafes</c> is not.
    /// </summary>
    public Option<Layer> Find(string id) => Layers.GetValueOrDefault(id).ToOption();

    /// <summary>
    /// Reads the database's list of tables as the service's role, and builds the catalogue from it.
    /// </summary>
    public static async Task<Result<LayerCatalogue, DatabaseError>> Load(Database database, CancellationToken cancel = default) =>
        (await database.Rows<CatalogueRow>(ListQuery, cancel).ConfigureAwait(false)).Map(rows => FromRows(rows));
}
