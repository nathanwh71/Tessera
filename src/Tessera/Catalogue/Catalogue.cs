using System.Collections.Immutable;
using System.Text.Json;
using CSharpFunctionalExtensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Tessera.Sql;

// The Catalogue module: the tables and views the service may publish as map tiles (US-03). The
// database keeps the guest list; the service reads it at start-up and every RefreshSeconds after.

namespace Tessera.Catalogue;

/// <summary>
/// One table or view the service may serve as map <b><i>tiles</i></b>: a <b><i>layer</i></b> (US-03).
/// Every name here, but the id, is already quoted for SQL, so it can go into a query's text as it is.
/// </summary>
/// <param name="Id">
/// The layer's id: its <b><i>schema</i></b>, the named folder that holds the table, and its name, such
/// as <c>food.cafes</c>.
/// </param>
/// <param name="Table">The quoted table, such as <c>"food"."cafes"</c>.</param>
/// <param name="Geometry">The quoted column that holds each place's shape.</param>
/// <param name="Srid">
/// The layer's <b><i>SRID</i></b>, the number of its coordinate system: 4326 for degrees, 3857 for
/// web-map metres.
/// </param>
/// <param name="GeometryType">The kind of shape, such as POINT.</param>
/// <param name="Properties">The quoted columns each tile carries beside the shape (US-07).</param>
/// <param name="Order">The quoted primary key, which fixes the order of features; empty for a view.</param>
public sealed record Layer(
    string Id, string Table, string Geometry, int Srid, string GeometryType,
    ImmutableArray<string> Properties, ImmutableArray<string> Order)
{
    /// <summary>
    /// What the tile query needs of this layer (Queries.Tile). Sql keeps a type of its own for it,
    /// so Sql needs nothing of Catalogue's and the <b><i>modules</i></b> depend in one direction
    /// (ADR 0001, rule 2).
    /// </summary>
    public TileSource Source => new(Id, Table, Geometry, Srid, Properties, Order);
}

/// <summary>
/// One row of <see cref="Queries.Layers"/>; Dapper fills it by matching column names. KeyColumns
/// defaults to '[]', an empty list, so a row built without a key, such as a view's in a test, still reads.
/// </summary>
public sealed record CatalogueRow(
    string Schema, string Name, string GeometryColumn, int Srid, string GeometryType,
    string Columns, string KeyColumns = "[]");

/// <summary>
/// The <b><i>catalogue</i></b>: the list of layers at one moment. Once built it never changes, so a
/// request can read it safely while a refresh builds the next one.
/// </summary>
public sealed record LayerCatalogue(ImmutableDictionary<string, Layer> Layers)
{
    /// <summary>Turns a row into a layer, quoting every name on the way in (S-01 c3).</summary>
    public static Layer ToLayer(CatalogueRow row) => new(
        $"{row.Schema}.{row.Name}",
        $"{Queries.Quote(row.Schema)}.{Queries.Quote(row.Name)}",
        Queries.Quote(row.GeometryColumn),
        row.Srid,
        row.GeometryType,
        Names(row.Columns),
        Names(row.KeyColumns));

    // The query writes each list of names as JSON, so this reads its own output.
    private static ImmutableArray<string> Names(string json) =>
        [.. (JsonSerializer.Deserialize<string[]>(json) ?? []).Select(Queries.Quote)];

    /// <summary>
    /// Builds the catalogue from the query's rows, keyed by id. The ordinal comparer matches ids
    /// letter for letter, case included, as <see cref="Find"/> promises.
    /// </summary>
    public static LayerCatalogue FromRows(IEnumerable<CatalogueRow> rows) =>
        new(rows.Select(ToLayer).ToImmutableDictionary(layer => layer.Id, StringComparer.Ordinal));

    /// <summary>
    /// The layer with this id, or null. The match is exact and case-sensitive, as the database treats
    /// quoted names: <c>food.cafes</c> is a layer, <c>Food.Cafes</c> is not.
    /// </summary>
    public Layer? Find(string id) => Layers.GetValueOrDefault(id);

    /// <summary>
    /// Reads the list as the service's <b><i>role</i></b>, the user name it signs in as, since the
    /// database shows a role only the tables it may read (US-03).
    /// </summary>
    public static async Task<Result<LayerCatalogue, DatabaseError>> Load(
        Database database, CancellationToken cancel = default) =>
        (await database.Rows<CatalogueRow>(Queries.Layers, cancel).ConfigureAwait(false)).Map(rows => FromRows(rows));
}

/// <summary>
/// The list of layers as it stands now, reread every <paramref name="every"/> so a table given a
/// <b><i>grant</i></b>, the database's word for a permission, after start-up is served without a
/// restart (N-23). .NET runs it as a background service, a loop that starts and stops with the program.
/// </summary>
/// <remarks>
/// A request must read one whole list, never half of two, so the class swaps the whole list in one
/// step. Until the first load succeeds there is no list, and tiles answer 503 (N-23 c3); it then tries
/// every <paramref name="retry"/>. A failed refresh keeps the last good list (N-23 c4), so a database
/// that blinks does not unpublish every layer.
/// </remarks>
/// <param name="load">Reads the list; a test passes a stand-in.</param>
/// <param name="every">The wait between refreshes; zero reads the list once.</param>
/// <param name="retry">The wait between tries before the first load succeeds.</param>
/// <param name="log">Where the class reports a new list or a failed read.</param>
public sealed class LiveCatalogue(
    Func<CancellationToken, Task<Result<LayerCatalogue, DatabaseError>>> load,
    TimeSpan every,
    TimeSpan retry,
    ILogger log) : BackgroundService
{
    private LayerCatalogue? _current;

    /// <summary>
    /// The list as it stands, or null before the first load. Volatile.Read makes each request see the
    /// list another thread swapped in last, never a stale copy held in a processor's memory.
    /// </summary>
    public LayerCatalogue? Current => Volatile.Read(ref _current);

    /// <summary>
    /// Reads the list once; on success it becomes the current list. It is public so the tile route can
    /// ask for a reread when a query meets a column a <b><i>seed script</i></b> has dropped.
    /// </summary>
    public async Task Refresh(CancellationToken cancel)
    {
        var loaded = await load(cancel).ConfigureAwait(false);
        if (loaded.IsFailure)
        {
            log.LogWarning(
                "Could not read the list of layers ({SqlState}): {Message}",
                loaded.Error.SqlState,
                loaded.Error.Message);
            return;
        }

        var old = Interlocked.Exchange(ref _current, loaded.Value);
        // The log speaks only when the set of layers changes, not at every refresh, so it stays short.
        if (old is null || !old.Layers.Keys.ToHashSet().SetEquals(loaded.Value.Layers.Keys))
            log.LogInformation("The service publishes {Count} layers.", loaded.Value.Layers.Count);
    }

    /// <summary>
    /// The loop .NET runs in the background. It tries every <c>retry</c> until the first load succeeds
    /// (N-23 c3), then rereads every <c>every</c> (N-23 c1), or stops once a zero says read only once.
    /// </summary>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                await Refresh(stoppingToken).ConfigureAwait(false);
                if (Current is not null && every <= TimeSpan.Zero)
                    return;
                await Task.Delay(Current is null ? retry : every, stoppingToken).ConfigureAwait(false);
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            // The service is stopping; the loop ends with it.
        }
    }
}
