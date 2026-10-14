using Tessera.Catalogue;
using Tessera.Import;
using Tessera.Settings;
using Tessera.Sql;

namespace Tessera.IntegrationTests;

/// <summary>US-24 tests 1, 5 and 6: nine typed tables, published by grant alone, replaced whole.</summary>
public sealed class ImportTests(DatabaseFixture database) : IClassFixture<DatabaseFixture>
{
    private static readonly AreaBox Macau = new(113.52, 22.10, 113.61, 22.22);

    private async Task<ImportSummary> Import(string json)
    {
        await using var publisher = Database.Open(database.Publisher);
        return (await Importer.Run(publisher, json, Macau, TestContext.Current.CancellationToken))
            .Match(summary => summary, error => throw new Xunit.Sdk.XunitException(error.Message));
    }

    private async Task<ImmutableArray<string>> Layers()
    {
        await using var reader = Database.Open(database.Reader);
        return (await LayerCatalogue.Load(reader, TestContext.Current.CancellationToken))
            .Match(found => found.Layers.Keys.Order(StringComparer.Ordinal).ToImmutableArray(), error => throw new Xunit.Sdk.XunitException(error.Message));
    }

    // Tests 1, 5 and 6 share one database and run in this order, as a publisher would work.
    [Fact]
    public async Task Loads_publishes_and_replaces_the_places()
    {
        // Test 1: nine typed tables from a small sample.
        var summary = await Import(Samples.Sample());
        Assert.Equal(9, summary.PlacesInLayer.Count);
        Assert.All(summary.PlacesInLayer.Values, count => Assert.Equal(2, count));
        Assert.Equal(9L, await database.OwnerValue<long>(
            "SELECT count(*) FROM geometry_columns WHERE f_table_schema IN ('food', 'outdoors', 'culture', 'transport') AND srid = 4326 AND type = 'POINT'"));
        Assert.Equal(9L, await database.OwnerValue<long>(
            "SELECT count(*) FROM pg_indexes WHERE schemaname IN ('food', 'outdoors', 'culture', 'transport') AND indexdef LIKE '%USING gist%'"));
        Assert.Equal(9L, await database.OwnerValue<long>(
            "SELECT count(*) FROM pg_stat_user_tables WHERE schemaname IN ('food', 'outdoors', 'culture', 'transport') AND last_analyze IS NOT NULL"));
        Assert.Equal(9L, await database.OwnerValue<long>(
            "SELECT count(*) FROM information_schema.key_column_usage k JOIN information_schema.table_constraints c USING (constraint_schema, constraint_name) WHERE c.constraint_type = 'PRIMARY KEY' AND k.column_name = 'id' AND k.table_schema IN ('food', 'outdoors', 'culture', 'transport')"));
        Assert.Equal("boolean", await database.OwnerValue<string>(
            "SELECT data_type FROM information_schema.columns WHERE table_schema = 'food' AND table_name = 'cafes' AND column_name = 'sample'"));

        // Test 5: with no grant, the catalogue lists none of the nine; after the grant, all nine.
        Assert.Empty(await Layers());
        await database.GrantPlaces();
        Assert.Equal(
            ["culture.libraries", "culture.museums", "food.cafes", "food.markets", "food.restaurants", "outdoors.parks", "outdoors.playgrounds", "transport.buses", "transport.trains"],
            await Layers());

        // Test 6: a second, smaller sample leaves its own rows in all nine tables, and none of the first.
        await Import(Samples.Sample(copies: 1));
        Assert.Equal(9L, await database.OwnerValue<long>(
            "SELECT (SELECT count(*) FROM food.cafes) + (SELECT count(*) FROM food.restaurants) + (SELECT count(*) FROM food.markets) + (SELECT count(*) FROM outdoors.parks) + (SELECT count(*) FROM outdoors.playgrounds) + (SELECT count(*) FROM culture.museums) + (SELECT count(*) FROM culture.libraries) + (SELECT count(*) FROM transport.trains) + (SELECT count(*) FROM transport.buses)"));
        Assert.Equal(0L, await database.OwnerValue<long>("SELECT count(*) FROM food.cafes WHERE id = 'cafes-002'"));
    }

    // The whole seed: 900 places, in longitude and latitude, stored as they arrive.
    [Fact]
    public async Task Loads_the_whole_seed()
    {
        var seed = await File.ReadAllTextAsync(Path.Combine(AppContext.BaseDirectory, "data", "macau-pois.geojson"), TestContext.Current.CancellationToken);
        var summary = await Import(seed);

        Assert.Equal(900, summary.PlacesInLayer.Values.Sum());
        Assert.Equal(113.577677, await database.OwnerValue<double>("SELECT ST_X(geom) FROM food.cafes WHERE id = 'cafes-001'"), 6);
    }

    // US-24 c6: when the database fails half-way through a load, the transaction rolls back and
    // every table keeps the rows it held. Here one name holds a NUL character, which PostgreSQL text
    // cannot store, so the database refuses that row after the old rows are deleted.
    [Fact]
    public async Task Rolls_back_a_load_the_database_refuses_half_way()
    {
        await Import(Samples.Sample());
        var before = await database.OwnerValue<long>("SELECT count(*) FROM food.cafes");

        await using var publisher = Database.Open(database.Publisher);
        var result = await Importer.Run(publisher, Samples.Sample(file =>
        {
            ((System.Text.Json.Nodes.JsonObject)file["features"]![17]!)["properties"]!["name"] = "bad\u0000name";
            return file;
        }), Macau, TestContext.Current.CancellationToken);

        Assert.Contains("The database refused the import", result.Match(_ => "", error => error.Message), StringComparison.Ordinal);
        Assert.Equal(before, await database.OwnerValue<long>("SELECT count(*) FROM food.cafes"));
        Assert.Equal(2L, before);
    }

    // A property whose values change kind between two files changes its column's type rather than
    // failing the load: 'sample' was true or false, and now one file gives it a word. The sample's
    // layers come in name order, so features 2 and 3 are the two cafes.
    [Fact]
    public async Task Changes_a_columns_type_when_its_values_change_kind()
    {
        await Import(Samples.Sample());
        await Import(Samples.Sample(file =>
        {
            ((System.Text.Json.Nodes.JsonObject)file["features"]![2]!)["properties"]!["sample"] = "yes";
            return file;
        }));

        Assert.Equal("text", await database.OwnerValue<string>(
            "SELECT data_type FROM information_schema.columns WHERE table_schema = 'food' AND table_name = 'cafes' AND column_name = 'sample'"));
        Assert.Equal("yes", await database.OwnerValue<string>("SELECT sample FROM food.cafes WHERE id = 'cafes-001'"));
        Assert.Equal("true", await database.OwnerValue<string>("SELECT sample FROM food.cafes WHERE id = 'cafes-002'"));

        // And back again: a file of true and false values returns the column to boolean.
        await Import(Samples.Sample());
        Assert.Equal("boolean", await database.OwnerValue<string>(
            "SELECT data_type FROM information_schema.columns WHERE table_schema = 'food' AND table_name = 'cafes' AND column_name = 'sample'"));
    }

    private Task<string?> TypeOfCafes(string column) => database.OwnerValue<string?>(
        $"SELECT data_type FROM information_schema.columns WHERE table_schema = 'food' AND table_name = 'cafes' AND column_name = '{column}'");

    // The likely fault of 14 October 2026: a text column holding a word, such as 'closed', could not
    // become a number column, because the old rows were still there, and the whole import rolled back.
    [Fact]
    public async Task Turns_a_text_column_of_words_into_numbers()
    {
        await Import(Samples.Sample(file =>
        {
            ((System.Text.Json.Nodes.JsonObject)file["features"]![2]!)["properties"]!["seats"] = "closed";
            return file;
        }));
        Assert.Equal("text", await TypeOfCafes("seats"));

        await Import(Samples.Sample(file =>
        {
            ((System.Text.Json.Nodes.JsonObject)file["features"]![2]!)["properties"]!["seats"] = 12;
            return file;
        }));

        Assert.Equal("double precision", await TypeOfCafes("seats"));
        Assert.Equal(12.0, await database.OwnerValue<double>("SELECT seats FROM food.cafes WHERE id = 'cafes-001'"));
    }
}
