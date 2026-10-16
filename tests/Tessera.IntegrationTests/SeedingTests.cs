using Modules.Tessera.Infrastructure;
using Modules.Tessera.Infrastructure.Seeding;
using Modules.Tessera.Infrastructure.Sql;

namespace Tessera.IntegrationTests;

/// <summary>
/// US-24 and ADR 0009: DbUp runs each <b><i>seed script</i></b> once, as tessera_publisher, each in
/// a transaction of its own, and a table waits for the publisher's grant. Each test names its
/// scripts and schemas apart from the others', so the tests share one database in any order.
/// </summary>
public sealed class SeedingTests(DatabaseFixture database) : IClassFixture<DatabaseFixture>
{
    /// <summary>
    /// Runs a folder of scripts as <c>Tessera seed</c> does and returns the names it ran. A failure
    /// throws, so a test that expects success stops with the script's own error.
    /// </summary>
    private static IReadOnlyList<string> Run(string connection, string folder) =>
        SeedScripts.Run(connection, folder).Match(ran => ran, error => throw new Xunit.Sdk.XunitException(error));

    // A folder of scripts for one test, written as the converter would leave them.
    private static string Folder(params (string Name, string Sql)[] scripts)
    {
        var folder = Directory.CreateTempSubdirectory("tessera-seed-").FullName;
        foreach (var (name, sql) in scripts)
            File.WriteAllText(Path.Combine(folder, name), sql);
        return folder;
    }

    /// <summary>
    /// The ids of the layers the service would publish now, read as tessera_reader, so a test sees
    /// what a <b><i>grant</i></b> has opened to the service, not what the owner can see.
    /// </summary>
    private async Task<string[]> Published()
    {
        await using var reader = Database.Open(database.Reader, new DatabaseSettings());
        return (await CatalogueReader.Load(reader, TestContext.Current.CancellationToken))
            .Match(found => found.Layers.Keys.Order(StringComparer.Ordinal).ToArray(), error => throw new Xunit.Sdk.XunitException(error.Message));
    }

    // US-24 test 1 and c1: the shipped script makes nine tables of 100 places, each with a spatial
    // index; a second run finds nothing new.
    [Fact]
    public async Task Loads_the_shipped_seed_once()
    {
        Run(database.Publisher, SeedScripts.Folder);

        Assert.Equal(900L, await database.OwnerValue<long>("""
            SELECT (SELECT count(*) FROM food.cafes) + (SELECT count(*) FROM food.restaurants) + (SELECT count(*) FROM food.markets)
                 + (SELECT count(*) FROM outdoors.parks) + (SELECT count(*) FROM outdoors.playgrounds)
                 + (SELECT count(*) FROM culture.museums) + (SELECT count(*) FROM culture.libraries)
                 + (SELECT count(*) FROM transport.trains) + (SELECT count(*) FROM transport.buses)
            """));
        Assert.Equal(100L, await database.OwnerValue<long>("SELECT count(*) FROM transport.buses"));
        Assert.Equal(9L, await database.OwnerValue<long>("""
            SELECT count(*) FROM pg_indexes
            WHERE schemaname IN ('food', 'outdoors', 'culture', 'transport') AND indexdef LIKE '%USING gist%'
            """));
        Assert.Equal("0001-macau-places.sql", await database.OwnerValue<string>("SELECT scriptname FROM tessera.schemaversions WHERE scriptname LIKE '0001-macau%'"));
        Assert.Empty(Run(database.Publisher, SeedScripts.Folder));
    }

    // US-24 c5: the seed never grants. The nine layers appear only after the publisher's grant, each
    // in longitude and latitude.
    [Fact]
    public async Task Publishes_nothing_until_the_grant()
    {
        Run(database.Publisher, SeedScripts.Folder);
        Assert.DoesNotContain("food.cafes", await Published());

        await database.GrantPlaces();
        await using var reader = Database.Open(database.Reader, new DatabaseSettings());
        var catalogue = (await CatalogueReader.Load(reader, TestContext.Current.CancellationToken)).Value;
        Assert.Equal((4326, "Point"), catalogue.Find("transport.buses").Map(layer => (layer.Srid, layer.GeometryType)).Value);
        Assert.Equal(["\"fid\""], catalogue.Find("food.cafes").Value.Order);
    }

    // US-24 c6: a script that fails changes nothing, and stays out of the journal, so it runs again
    // once fixed. The failure names the script.
    [Fact]
    public async Task Undoes_a_script_that_fails()
    {
        var folder = Folder(
            ("0001-trial-a.sql", "CREATE SCHEMA trial_a; CREATE TABLE trial_a.places (fid serial PRIMARY KEY); INSERT INTO trial_a.places DEFAULT VALUES;"),
            ("0002-trial-a-broken.sql", "DROP TABLE trial_a.places; SELECT no_such_function();"));

        var failed = SeedScripts.Run(database.Publisher, folder);

        Assert.StartsWith("0002-trial-a-broken.sql failed", failed.Error, StringComparison.Ordinal);
        Assert.Equal(1L, await database.OwnerValue<long>("SELECT count(*) FROM trial_a.places"));
        Assert.Equal(0L, await database.OwnerValue<long>("SELECT count(*) FROM tessera.schemaversions WHERE scriptname = '0002-trial-a-broken.sql'"));
    }

    // A new script that replaces a table takes the old grant with it, so the new table, too, waits
    // for the publisher (US-24 c5).
    [Fact]
    public async Task Withdraws_a_replaced_table_until_the_grant()
    {
        const string table = "CREATE TABLE trial_b.places (fid serial PRIMARY KEY, geom geometry(Point, 4326)); INSERT INTO trial_b.places (geom) VALUES (ST_SetSRID(ST_MakePoint(113.55, 22.19), 4326));";
        var folder = Folder(("0001-trial-b.sql", "CREATE SCHEMA trial_b; " + table));
        Run(database.Publisher, folder);
        await database.Owner("GRANT USAGE ON SCHEMA trial_b TO tessera_reader; GRANT SELECT ON trial_b.places TO tessera_reader;");
        Assert.Contains("trial_b.places", await Published());

        File.WriteAllText(Path.Combine(folder, "0002-trial-b-again.sql"), "DROP TABLE IF EXISTS trial_b.places CASCADE; " + table);

        Assert.Equal(["0002-trial-b-again.sql"], Run(database.Publisher, folder));
        Assert.DoesNotContain("trial_b.places", await Published());
    }
}
