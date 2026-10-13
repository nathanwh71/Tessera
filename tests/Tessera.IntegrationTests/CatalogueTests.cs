using Tessera.Catalogue;
using Tessera.Settings;
using Tessera.Sql;
using Tessera.Tiles;

namespace Tessera.IntegrationTests;

/// <summary>US-03 test 4: publish a table by granting it, signed in as the real role.</summary>
public sealed class CatalogueTests(DatabaseFixture database) : IClassFixture<DatabaseFixture>, IAsyncLifetime
{
    private static readonly TileSettings Tiles = new(10000, 64, 60, 64);

    private static TileServer Server(LayerCatalogue catalogue, Database reader) =>
        new(catalogue, reader.Single<byte[]>, Tiles, new TileCache(Tiles, TimeProvider.System));

    // The stage: one of each kind US-03 names. xUnit runs this before each test, so every step may
    // run again on a stage it has already set. The test makes its own tables, as US-03 asks;
    // the seed is GeoJSON, and holds none of these.
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

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    private async Task<LayerCatalogue> Load(Database reader) =>
        (await LayerCatalogue.Load(reader, TestContext.Current.CancellationToken))
            .Match(found => found, error => throw new Xunit.Sdk.XunitException(error.Message));

    [Fact]
    public async Task Publishes_what_is_granted_and_nothing_else()
    {
        await using var reader = Database.Open(database.Reader);
        var ids = (await Load(reader)).Layers.Keys.Order(StringComparer.Ordinal).ToArray();

        // c1, c2 and c4 publish; c3 (never granted), c5 (a function) and an untyped column do not.
        Assert.Equal(["stage.granted", "stage.granted_view", "stage.mercator", "stage.odd \"name\"", "stage.slow", "stage.stored_view"], ids);
    }

    // c1: a layer carries its SRID and geometry type.
    [Fact]
    public async Task Gives_each_layer_its_srid_and_type()
    {
        await using var reader = Database.Open(database.Reader);
        var catalogue = await Load(reader);

        Assert.Equal((4326, "Point"), catalogue.Find("stage.granted").Match(layer => (layer.Srid, layer.GeometryType), () => (0, "")));
        Assert.Equal((3857, "Point"), catalogue.Find("stage.mercator").Match(layer => (layer.Srid, layer.GeometryType), () => (0, "")));
    }

    // c4: the awkward name is published and served.
    [Fact]
    public async Task Serves_the_awkward_name()
    {
        await using var reader = Database.Open(database.Reader);
        var catalogue = await Load(reader);

        var answer = await Server(catalogue, reader).Answer("stage.odd \"name\"", 0, 0, 0, TileRequest.Plain, TestContext.Current.CancellationToken);

        Assert.Equal(200, await Http.Status(answer));
    }

    // c6: a table revoked after start-up answers 404, as a problem document.
    [Fact]
    public async Task Answers_404_for_a_table_revoked_after_start_up()
    {
        await database.Owner("CREATE TABLE IF NOT EXISTS stage.revoked (id int PRIMARY KEY, geom geometry(Point, 4326)); GRANT SELECT ON stage.revoked TO tessera_reader;");
        await using var reader = Database.Open(database.Reader);
        var catalogue = await Load(reader);
        await database.Owner("REVOKE SELECT ON stage.revoked FROM tessera_reader;");

        var answer = await Server(catalogue, reader).Answer("stage.revoked", 0, 0, 0, TileRequest.Plain, TestContext.Current.CancellationToken);

        Assert.Equal((404, "application/problem+json"), await Http.StatusAndType(answer));
    }

    // N-22 c1 and test 2: a database that cannot be reached answers 503 with Retry-After, not 500.
    [Fact]
    public async Task Answers_503_when_the_database_cannot_be_reached()
    {
        await using var reader = Database.Open(database.Reader);
        var catalogue = await Load(reader);
        await using var gone = Database.Open(database.Reader with { Port = Http.FreePort() }, PoolLimits.Default with { ConnectionTimeout = TimeSpan.FromSeconds(1) });

        var answer = await Server(catalogue, gone).Answer("stage.granted", 0, 0, 0, TileRequest.Plain, TestContext.Current.CancellationToken);
        var (status, headers) = await Http.StatusAndHeaders(answer);

        Assert.Equal(503, status);
        Assert.Equal("5", headers.RetryAfter.ToString());
        Assert.Equal("no-store", headers.CacheControl.ToString());
    }

    // N-22 c2 and test 3: a query past DbTimeout is cancelled and answers 503; here a view that
    // sleeps three seconds meets a limit of one.
    [Fact]
    public async Task Answers_503_when_a_query_runs_past_its_time()
    {
        await using var reader = Database.Open(database.Reader, PoolLimits.Default with { CommandTimeout = TimeSpan.FromSeconds(1) });
        var catalogue = await Load(reader);
        var clock = System.Diagnostics.Stopwatch.StartNew();

        var answer = await Server(catalogue, reader).Answer("stage.slow", 0, 0, 0, TileRequest.Plain, TestContext.Current.CancellationToken);

        Assert.Equal(503, await Http.Status(answer));
        Assert.True(clock.Elapsed < TimeSpan.FromSeconds(3), $"took {clock.Elapsed}");
    }
}
