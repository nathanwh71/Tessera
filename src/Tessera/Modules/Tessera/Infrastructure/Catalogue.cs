using System.Collections.Immutable;
using System.Text.Json;
using CSharpFunctionalExtensions;
using Modules.Tessera.Domain;
using Modules.Tessera.Infrastructure.Sql;

// Reads the list of tables and views the service may publish (US-03) from the database, and turns
// each row into a Layer. The quoting of names lives here, beside the queries, since only SQL needs it.

namespace Modules.Tessera.Infrastructure;

/// <summary>
/// One row of <see cref="Queries.Layers"/>; Dapper fills it by matching column names. KeyColumns
/// defaults to '[]', an empty list, so a row built without a key, such as a view's in a test,
/// still reads.
/// </summary>
public sealed record CatalogueRow(
    string Schema, string Name, string GeometryColumn, int Srid, string GeometryType,
    string Columns, string KeyColumns = "[]");

/// <summary>Builds the <b><i>catalogue</i></b> from the database's guest list.</summary>
public static class CatalogueReader
{
    /// <summary>Turns a row into a <b><i>layer</i></b>, quoting every name on the way in (S-01 c3).</summary>
    public static Layer ToLayer(CatalogueRow row) => new(
        $"{row.Schema}.{row.Name}",
        SqlName.Qualified(row.Schema, row.Name),
        SqlName.Quote(row.GeometryColumn),
        row.Srid,
        row.GeometryType,
        Names(row.Columns),
        Names(row.KeyColumns));

    // The query writes each list of names as JSON, so this reads its own output. The reader gives
    // null for the JSON text 'null', which the query never writes; none then means no names.
    private static ImmutableArray<SqlName> Names(string json) =>
        JsonSerializer.Deserialize<string[]>(json) is { } names ? [.. names.Select(SqlName.Quote)] : [];

    /// <summary>
    /// Builds the catalogue from the query's rows, keyed by id. The ordinal comparer matches ids
    /// letter for letter, case included, as <see cref="LayerCatalogue.Find"/> promises.
    /// </summary>
    public static LayerCatalogue FromRows(IEnumerable<CatalogueRow> rows) =>
        new(rows.Select(ToLayer).ToImmutableDictionary(layer => layer.Id, StringComparer.Ordinal));

    /// <summary>
    /// Reads the list as the service's <b><i>role</i></b>, the user name it signs in as, since the
    /// query asks the database which tables that role may read (US-03).
    /// </summary>
    public static async Task<Result<LayerCatalogue, DatabaseError>> Load(
        Database database, CancellationToken cancel = default) =>
        (await database.Rows<CatalogueRow>(Queries.Layers, cancel).ConfigureAwait(false)).Map(rows => FromRows(rows));
}
