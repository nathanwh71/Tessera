using Modules.Tessera.Domain;
using Modules.Tessera.Features;
using Modules.Tessera.Infrastructure;
using Modules.Tessera.Infrastructure.Sql;

namespace Tessera.IntegrationTests;

/// <summary>
/// US-03 test 4: publish a table by a <b><i>grant</i></b>, signed in as the real <b><i>role</i></b>.
/// </summary>
public sealed class CatalogueTests(DatabaseFixture database) : IClassFixture<DatabaseFixture>, IAsyncLifetime
{
    /// <summary>
    /// A buffer fixed here, apart from the shipped 256, so these tests do not shift when that
    /// default changes; the unit tests pin the same 64.
    /// </summary>
    private static readonly TileSettings Tiles = new() { Buffer = 64 };

    /// <summary>
    /// The tile route's own code, called directly with a fixed <b><i>catalogue</i></b>, so a test
    /// controls which list and which database it meets, with no web server to start.
    /// </summary>
    private static TileServer Server(LayerCatalogue catalogue, Database reader) =>
        new(() => catalogue, reader.Single<byte[]>, Tiles);

    // The stage: one of each kind US-03 names. xUnit runs this before each test, so every step may
    // run again on a stage it has already set. The test makes its own tables, as US-03 asks;
    // the shipped seed script holds none of these.
    public async ValueTask InitializeAsync() => await database.Owner(""""
        CREATE SCHEMA IF NOT EXISTS stage;
        CREATE TABLE IF NOT EXISTS stage.granted (id int PRIMARY KEY, name text, geom geometry(Point, 4326));
        INSERT INTO stage.granted VALUES (1, 'a', ST_SetSRID(ST_MakePoint(113.54, 22.19), 4326)) ON CONFLICT DO NOTHING;
        CREATE TABLE IF NOT EXISTS stage.mercator (id int PRIMARY KEY, geom geometry(Point, 3857));
        CREATE OR REPLACE VIEW stage.granted_view AS SELECT id, name, geom FROM stage.granted;
        CREATE MATERIALIZED VIEW IF NOT EXISTS stage.stored_view AS SELECT id, name, geom FROM stage.granted;
        CREATE TABLE IF NOT EXISTS stage.never_granted (id int PRIMARY KEY, geom geometry(Point, 4326));
        CREATE TABLE IF NOT EXISTS stage.untyped (id int PRIMARY KEY, geom geometry);
        CREATE TABLE IF NOT EXISTS stage."odd ""name""" (id int PRIMARY KEY, geom geometry(Point, 4326));
        INSERT INTO stage."odd ""name""" VALUES (1, ST_SetSRID(ST_MakePoint(113.54, 22.19), 4326)) ON CONFLICT DO NOTHING;
        CREATE OR REPLACE VIEW stage.slow AS SELECT g.id, g.geom FROM stage.granted g CROSS JOIN LATERAL (SELECT pg_sleep(3)) AS pause;
        CREATE OR REPLACE FUNCTION stage.places_near(z int, x int, y int) RETURNS bytea LANGUAGE sql AS 'SELECT NULL::bytea';
        GRANT USAGE ON SCHEMA stage TO tessera_reader;
        GRANT SELECT ON stage.granted, stage.mercator, stage.granted_view, stage.stored_view, stage.untyped, stage."odd ""name""", stage.slow TO tessera_reader;
        GRANT EXECUTE ON FUNCTION stage.places_near(int, int, int) TO tessera_reader;
        """");

    /// <summary>
    /// Nothing to undo: the stage stays for the next test, and the fixture's container, with every
    /// table in it, vanishes once the class ends.
    /// </summary>
    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    /// <summary>
    /// Loads the list as the service does. A failure throws, so the test stops with the database's
    /// own message rather than failing later on an empty list.
    /// </summary>
    private async Task<LayerCatalogue> Load(Database reader) =>
        (await CatalogueReader.Load(reader, TestContext.Current.CancellationToken))
            .Match(found => found, error => throw new Xunit.Sdk.XunitException(error.Message));

    [Fact]
    public async Task Publishes_what_is_granted_and_nothing_else()
    {
        await using var reader = Database.Open(database.Reader, new DatabaseSettings());
        var ids = (await Load(reader)).Layers.Keys.Order(StringComparer.Ordinal).ToArray();

        // c1, c2 and c4 publish; c3 (never granted), c5 (a function) and an untyped column do not.
        Assert.Equal(["stage.granted", "stage.granted_view", "stage.mercator", "stage.odd \"name\"", "stage.slow", "stage.stored_view"], ids);
    }

    // c1: a layer carries its SRID and geometry type.
    [Fact]
    public async Task Gives_each_layer_its_srid_and_type()
    {
        await using var reader = Database.Open(database.Reader, new DatabaseSettings());
        var catalogue = await Load(reader);

        Assert.Equal((4326, "Point"), catalogue.Find("stage.granted") is { } granted ? (granted.Srid, granted.GeometryType) : default);
        Assert.Equal((3857, "Point"), catalogue.Find("stage.mercator") is { } mercator ? (mercator.Srid, mercator.GeometryType) : default);
    }

    // US-24 test 2: a table's primary key fixes the order of its features; a view has no key,
    // so no order. The same key always gives the same tile.
    [Fact]
    public async Task Gives_each_table_its_key_and_each_view_none()
    {
        await using var reader = Database.Open(database.Reader, new DatabaseSettings());
        var catalogue = await Load(reader);

        Assert.Equal(["\"id\""], catalogue.Find("stage.granted")!.Order.Select(name => name.Sql));
        Assert.Empty(catalogue.Find("stage.granted_view")!.Order);
    }

    // c4: the awkward name is published and served.
    [Fact]
    public async Task Serves_the_awkward_name()
    {
        await using var reader = Database.Open(database.Reader, new DatabaseSettings());
        var catalogue = await Load(reader);

        var answer = await Server(catalogue, reader).Answer("stage.odd \"name\"", 0, 0, 0, TestContext.Current.CancellationToken);

        Assert.Equal(200, await Http.Status(answer));
    }

    // c6: a table revoked after start-up answers 404, as a problem document.
    [Fact]
    public async Task Answers_404_for_a_table_revoked_after_start_up()
    {
        await database.Owner("CREATE TABLE IF NOT EXISTS stage.revoked (id int PRIMARY KEY, geom geometry(Point, 4326)); GRANT SELECT ON stage.revoked TO tessera_reader;");
        await using var reader = Database.Open(database.Reader, new DatabaseSettings());
        var catalogue = await Load(reader);
        await database.Owner("REVOKE SELECT ON stage.revoked FROM tessera_reader;");

        var answer = await Server(catalogue, reader).Answer("stage.revoked", 0, 0, 0, TestContext.Current.CancellationToken);

        Assert.Equal((404, "application/problem+json"), await Http.StatusAndType(answer));
    }

    // N-22 c1 and test 2: a database that cannot be reached answers 503 with Retry-After, not 500.
    [Fact]
    public async Task Answers_503_when_the_database_cannot_be_reached()
    {
        await using var reader = Database.Open(database.Reader, new DatabaseSettings());
        var catalogue = await Load(reader);
        // A port FreePort has just found unused, so nothing answers there: a database that cannot be
        // reached. A one-second wait keeps the test short.
        await using var gone = Database.Open($"Host=127.0.0.1;Port={Http.FreePort()};Database=tessera;Username=tessera_reader;Password=x", new DatabaseSettings { ConnectionWaitSeconds = 1 });

        var answer = await Server(catalogue, gone).Answer("stage.granted", 0, 0, 0, TestContext.Current.CancellationToken);
        var (status, headers) = await Http.StatusAndHeaders(answer);

        Assert.Equal(503, status);
        Assert.Equal("5", headers.RetryAfter.ToString());
        Assert.Equal("no-store", headers.CacheControl.ToString());
    }

    // N-22 c2 and test 3: a query past Database.QueryTimeoutSeconds is cancelled and answers 503;
    // here a view that sleeps three seconds meets a limit of one.
    [Fact]
    public async Task Answers_503_when_a_query_runs_past_its_time()
    {
        await using var reader = Database.Open(database.Reader, new DatabaseSettings { QueryTimeoutSeconds = 1 });
        var catalogue = await Load(reader);
        var clock = System.Diagnostics.Stopwatch.StartNew();

        var answer = await Server(catalogue, reader).Answer("stage.slow", 0, 0, 0, TestContext.Current.CancellationToken);

        Assert.Equal(503, await Http.Status(answer));
        Assert.True(clock.Elapsed < TimeSpan.FromSeconds(3), $"took {clock.Elapsed}");
    }

    // N-23 c1 and c2, and test 2: a table granted after start-up joins the list within one
    // refresh, and leaves it once revoked.
    [Fact]
    public async Task Sees_a_grant_and_a_revoke_within_one_refresh()
    {
        await database.Owner("CREATE TABLE IF NOT EXISTS stage.late (id int PRIMARY KEY, geom geometry(Point, 4326)); REVOKE SELECT ON stage.late FROM tessera_reader;");
        await using var reader = Database.Open(database.Reader, new DatabaseSettings());
        // A refresh every 200 ms and a retry every 100 ms, in place of the service's 30 and 5
        // seconds, so each change shows within the test's five-second wait.
        using var live = new LiveCatalogue(cancel => CatalogueReader.Load(reader, cancel), TimeSpan.FromMilliseconds(200), TimeSpan.FromMilliseconds(100), Microsoft.Extensions.Logging.Abstractions.NullLogger.Instance);
        await live.StartAsync(TestContext.Current.CancellationToken);

        await Until(() => live.Current?.Layers.ContainsKey("stage.granted") == true);
        Assert.False(live.Current!.Layers.ContainsKey("stage.late"));

        await database.Owner("GRANT SELECT ON stage.late TO tessera_reader;");
        await Until(() => live.Current?.Layers.ContainsKey("stage.late") == true);

        await database.Owner("REVOKE SELECT ON stage.late FROM tessera_reader;");
        await Until(() => live.Current?.Layers.ContainsKey("stage.late") == false);

        await live.StopAsync(TestContext.Current.CancellationToken);
    }

    /// <summary>
    /// Waits for the list to change, checking every 100 ms for up to five seconds. A refresh runs
    /// in the background, so no single moment is sure to see it; polling ends as soon as it shows.
    /// </summary>
    private static async Task Until(Func<bool> condition)
    {
        for (var attempt = 0; attempt < 50 && !condition(); attempt++)
            await Task.Delay(100, TestContext.Current.CancellationToken);
        Assert.True(condition(), "the list did not change within five seconds");
    }
}
