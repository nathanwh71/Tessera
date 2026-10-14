using Tessera.Catalogue;
using Tessera.Sql;

namespace Tessera.IntegrationTests;

/// <summary>
/// N-26: queries wait for a connection in the order they arrive, and the health check's ping never
/// waits behind them (ADR 0006 rule 7).
/// </summary>
/// <remarks>
/// The Java build keeps the order with a gate in front of its pool. The .NET build relies on
/// Npgsql's pool, which hands each freed connection to the request that has waited longest; these
/// tests prove that it does, so a later Npgsql that changed it would fail the build.
/// </remarks>
public sealed class PoolTests(DatabaseFixture database) : IClassFixture<DatabaseFixture>, IAsyncLifetime
{
    // One place in the tile, and two views of it that sleep: a short pause for the queue, a long
    // hold for the health check. pg_sleep runs once for each row the view reads.
    public async ValueTask InitializeAsync() => await database.Owner("""
        CREATE SCHEMA IF NOT EXISTS stage;
        CREATE TABLE IF NOT EXISTS stage.queue_place (id int PRIMARY KEY, geom geometry(Point, 4326));
        INSERT INTO stage.queue_place VALUES (1, ST_SetSRID(ST_MakePoint(113.54, 22.19), 4326)) ON CONFLICT DO NOTHING;
        CREATE OR REPLACE VIEW stage.queue_pause AS SELECT q.id, q.geom FROM stage.queue_place q CROSS JOIN LATERAL (SELECT pg_sleep(0.4)) AS pause;
        CREATE OR REPLACE VIEW stage.queue_hold AS SELECT q.id, q.geom FROM stage.queue_place q CROSS JOIN LATERAL (SELECT pg_sleep(3)) AS pause;
        GRANT USAGE ON SCHEMA stage TO tessera_reader;
        GRANT SELECT ON stage.queue_place, stage.queue_pause, stage.queue_hold TO tessera_reader;
        """);

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    // One connection, and a wait long enough that no request gives up while the others run.
    private static readonly PoolLimits OneConnection = new(1, TimeSpan.FromHours(1), TimeSpan.FromSeconds(30), TimeSpan.FromSeconds(30));

    // N-26 c1 and test 1: five tiles asked for a moment apart, on a pool of one connection, finish
    // in the order they were asked for. Each is a different tile, at levels 0 to 4, so the
    // one-query-per-tile rule of N-24 cannot merge them.
    [Fact]
    public async Task Runs_queries_in_the_order_they_arrive()
    {
        await using var reader = Database.Open(database.Reader, OneConnection);
        var layer = await Layer(reader, "stage.queue_pause");
        var finished = new System.Collections.Concurrent.ConcurrentQueue<int>();

        var asks = new List<Task>();
        for (var zoom = 0; zoom < 5; zoom++)
        {
            var (x, y) = TileOf(113.54, 22.19, zoom);
            var mine = zoom;
            asks.Add(reader.Single<byte[]>(TileQueries.Tile(layer.Source, new TileCell(zoom, x, y), new TileLimits(10000, 64)), TestContext.Current.CancellationToken)
                .ContinueWith(_ => finished.Enqueue(mine), TaskScheduler.Default));
            // Long enough for each request to join the queue before the next; far shorter than one query.
            await Task.Delay(50, TestContext.Current.CancellationToken);
        }
        await Task.WhenAll(asks);

        Assert.Equal([0, 1, 2, 3, 4], finished.ToArray());
    }

    // N-26 c3 and test 2: with the only tile connection busy for three seconds, the ping still
    // answers within the health check's one second.
    [Fact]
    public async Task Answers_the_ping_while_every_tile_connection_is_busy()
    {
        await using var reader = Database.Open(database.Reader, OneConnection);
        var layer = await Layer(reader, "stage.queue_hold");
        var (x, y) = TileOf(113.54, 22.19, 0);
        var hold = reader.Single<byte[]>(TileQueries.Tile(layer.Source, new TileCell(0, x, y), new TileLimits(10000, 64)), TestContext.Current.CancellationToken);
        await Task.Delay(300, TestContext.Current.CancellationToken);

        using var limit = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
        limit.CancelAfter(TimeSpan.FromSeconds(1));
        var ping = await reader.Ping(limit.Token);

        Assert.True(ping.Match(_ => true, _ => false));
        Assert.False(hold.IsCompleted, "the tile query should still hold its connection");
        await hold;
    }

    // N-26 c2: a query whose own token is cancelled while it waits leaves the queue at once, rather
    // than waiting out the pool's whole timeout. Tile queries pass the service's stop token instead,
    // so this covers the other queries, such as the list of layers.
    [Fact]
    public async Task Lets_a_cancelled_request_leave_the_queue()
    {
        await using var reader = Database.Open(database.Reader, OneConnection);
        var layer = await Layer(reader, "stage.queue_hold");
        var (x, y) = TileOf(113.54, 22.19, 0);
        var hold = reader.Single<byte[]>(TileQueries.Tile(layer.Source, new TileCell(0, x, y), new TileLimits(10000, 64)), TestContext.Current.CancellationToken);
        await Task.Delay(300, TestContext.Current.CancellationToken);

        using var leave = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
        leave.CancelAfter(TimeSpan.FromMilliseconds(200));
        var clock = System.Diagnostics.Stopwatch.StartNew();
        var (x1, y1) = TileOf(113.54, 22.19, 1);

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            reader.Single<byte[]>(TileQueries.Tile(layer.Source, new TileCell(1, x1, y1), new TileLimits(10000, 64)), leave.Token));
        Assert.True(clock.Elapsed < TimeSpan.FromSeconds(1), $"took {clock.Elapsed}");
        await hold;
    }

    private async Task<Layer> Layer(Database reader, string id) =>
        (await LayerCatalogue.Load(reader, TestContext.Current.CancellationToken))
            .Match(found => found, error => throw new Xunit.Sdk.XunitException(error.Message))
            .Find(id)
            .Match(found => found, () => throw new Xunit.Sdk.XunitException($"{id} is not in the list"));

    // The web map's tile that holds a point: x counts columns from 180° west, y rows from the north.
    private static (int X, int Y) TileOf(double longitude, double latitude, int zoom)
    {
        var tiles = 1 << zoom;
        var radians = latitude * Math.PI / 180;
        return ((int)((longitude + 180) / 360 * tiles),
                (int)((1 - Math.Log(Math.Tan(radians) + (1 / Math.Cos(radians))) / Math.PI) / 2 * tiles));
    }
}
