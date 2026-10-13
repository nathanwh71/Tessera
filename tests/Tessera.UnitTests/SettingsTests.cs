using Tessera.Settings;

namespace Tessera.UnitTests;

/// <summary>US-00 c4, ADR 0003 and N-21: settings come from the environment and tessera.json, in pg_tileserv's names, checked at start-up.</summary>
public sealed class SettingsTests
{
    private const string File = """
        {
          "ShowPreview": true,
          "MaxFeaturesPerTile": 10000,
          "DefaultBuffer": 64,
          "Area": {
            "Name": "Macau",
            "Box": { "West": 113.52, "South": 22.10, "East": 113.61, "North": 22.22 },
            "Home": { "Longitude": 113.55, "Latitude": 22.19, "Zoom": 14 }
          }
        }
        """;

    private static readonly ImmutableDictionary<string, string> Machines = new Dictionary<string, string>
    {
        ["TESSERA_HTTPPORT"] = "8080",
        ["DATABASE_URL"] = "postgresql://tessera_reader:s%40cret@db.office:5433/tessera",
    }.ToImmutableDictionary();

    private static Result<ServiceSettings, ImmutableArray<SettingsError>> Read(IReadOnlyDictionary<string, string> environment, string? file = File) =>
        SettingsReader.ReadService(environment, "tessera.json", file.ToOption());

    private static ServiceSettings Settings(IReadOnlyDictionary<string, string> environment, string? file = File) =>
        Read(environment, file).Match(found => found, errors => throw new Xunit.Sdk.XunitException(string.Join("; ", errors)));

    private static ImmutableArray<string> Problems(IReadOnlyDictionary<string, string> environment, string? file = File) =>
        Read(environment, file).Match(_ => [], errors => errors.Select(error => error.Name).ToImmutableArray());

    // A file with one more key, written before the Area block.
    private static string With(string key, string value) => File.Replace("\"ShowPreview\": true,", $"\"ShowPreview\": true, \"{key}\": {value},", StringComparison.Ordinal);

    [Fact]
    public void Reads_every_setting()
    {
        var settings = Settings(Machines);

        Assert.Equal("Macau", settings.Area.Name);
        Assert.Equal(new AreaBox(113.52, 22.10, 113.61, 22.22), settings.Area.Box);
        Assert.Equal(8080, settings.Http.Port);
        // %40 is '@', percent-encoded so it cannot be mistaken for the '@' before the host.
        Assert.Equal(new DatabaseAddress("db.office", 5433, "tessera", "tessera_reader", "s@cret"), settings.Db.Address);
        Assert.Equal(new TileSettings(10000, 64, 60, 64), settings.Tiles);
    }

    // N-21 c2: a key the file leaves out takes the value the stories name.
    [Fact]
    public void Gives_each_key_the_file_leaves_out_its_default()
    {
        var settings = Settings(Machines, """{ "Area": { "Name": "Macau", "Box": { "West": 113.52, "South": 22.10, "East": 113.61, "North": 22.22 }, "Home": { "Longitude": 113.55, "Latitude": 22.19, "Zoom": 15 } } }""");

        Assert.True(settings.Map.Preview);
        Assert.Equal(new TileSettings(10000, 64, 60, 64), settings.Tiles);
        Assert.Equal((4, TimeSpan.FromHours(1), TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(3)),
            (settings.Db.PoolMaxConns, settings.Db.PoolMaxConnLifeTime, settings.Db.Timeout, settings.Db.PoolConnectionTimeout));
        Assert.Equal(TimeSpan.FromSeconds(30), settings.Catalogue.Refresh);
    }

    // US-00 test 2: started with no variables set, the service names each one it lacks.
    [Fact]
    public void Names_each_missing_variable()
    {
        var errors = Read(ImmutableDictionary<string, string>.Empty).Match(_ => [], found => found);

        Assert.Contains(errors, error => error.Name == "HttpPort" && error.Problem.Contains("TESSERA_HTTPPORT", StringComparison.Ordinal));
        Assert.Contains(errors, error => error.Name == "DATABASE_URL");
    }

    [Fact]
    public void Names_the_database_even_when_the_file_is_missing() =>
        Assert.Equal(["tessera.json", "DATABASE_URL"], Problems(Machines.Remove("DATABASE_URL"), file: null));

    [Fact]
    public void Treats_an_empty_variable_as_missing() =>
        Assert.Contains("DATABASE_URL", Problems(Machines.SetItem("DATABASE_URL", "")));

    [Fact]
    public void Names_a_missing_file() =>
        Assert.Contains("tessera.json", Problems(Machines, file: null));

    // N-21 c3: TESSERA_ and the key, in any case, overrides it, as pg_tileserv's TS_HTTPPORT does.
    [Theory]
    [InlineData("TESSERA_DEFAULTBUFFER")]
    [InlineData("TESSERA_DefaultBuffer")]
    [InlineData("tessera_defaultbuffer")]
    public void Lets_a_variable_override_a_key_in_any_case(string name) =>
        Assert.Equal(32, Settings(Machines.SetItem(name, "32")).Tiles.Buffer);

    [Fact]
    public void Lets_a_variable_override_a_nested_key() =>
        Assert.Equal(17, Settings(Machines.SetItem("TESSERA_AREA__HOME__ZOOM", "17")).Area.Home.Zoom);

    [Fact]
    public void Leaves_a_variable_that_names_no_key_alone() =>
        Assert.Equal(64, Settings(Machines.SetItem("TESSERA_DB_PASSWORD", "x").SetItem("TESSERA_Tiles__Buffer", "32")).Tiles.Buffer);

    [Fact]
    public void Names_a_bad_key() =>
        Assert.Contains("Area.Home.Zoom", Problems(Machines.SetItem("TESSERA_AREA__HOME__ZOOM", "40")));

    [Fact]
    public void Refuses_a_key_written_twice() =>
        Assert.Contains("tessera.json", Problems(Machines, """{ "Area": { "Name": "a", "Name": "b" } }"""));

    // A password in the address is never printed: a log line may reach someone it should not.
    [Fact]
    public void Hides_the_password_when_printed() =>
        Assert.DoesNotContain("secret", new DatabaseAddress("db", 5432, "tessera", "reader", "secret").ToString());

    // ADR 0003: the shipped tessera.json names no machine, no port and no password; the environment does.
    [Fact]
    public void Ships_a_file_with_no_machine_and_no_password()
    {
        var shipped = System.IO.File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "tessera.json"));
        var keys = System.Text.Json.Nodes.JsonNode.Parse(shipped)!.AsObject().SelectMany(Keys);

        Assert.DoesNotContain("://", shipped, StringComparison.Ordinal);
        Assert.DoesNotContain(keys, key => new[] { "password", "host", "url", "httpport", "user" }.Any(word => key.Contains(word, StringComparison.OrdinalIgnoreCase)));
        Assert.True(Read(Machines, shipped).Match(_ => true, _ => false));
    }

    // N-21 c1: every key in the shipped file is one the service reads, spelt the same way.
    [Fact]
    public void Ships_only_keys_the_service_reads()
    {
        var shipped = System.IO.File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "tessera.json"));
        var leaves = System.Text.Json.Nodes.JsonNode.Parse(shipped)!.AsObject().SelectMany(entry => Leaves(entry, ""));

        Assert.All(leaves, key => Assert.Contains(key, SettingsReader.Keys));
    }

    private static IEnumerable<string> Keys(KeyValuePair<string, System.Text.Json.Nodes.JsonNode?> entry) =>
        entry.Value is System.Text.Json.Nodes.JsonObject child
            ? child.SelectMany(Keys).Prepend(entry.Key)
            : [entry.Key];

    private static IEnumerable<string> Leaves(KeyValuePair<string, System.Text.Json.Nodes.JsonNode?> entry, string parent) =>
        entry.Value is System.Text.Json.Nodes.JsonObject child
            ? child.SelectMany(inner => Leaves(inner, $"{parent}{entry.Key}."))
            : [$"{parent}{entry.Key}"];

    // N-18 c1 and c5: CacheTTL may be left out, and then a browser keeps a tile for a minute.
    [Theory]
    [InlineData("0", 0)]
    [InlineData("300", 300)]
    public void Reads_cache_ttl(string value, int expected) =>
        Assert.Equal(expected, Settings(Machines, With("CacheTTL", value)).Tiles.CacheSeconds);

    [Fact]
    public void Reads_tile_cache_megabytes() =>
        Assert.Equal(8, Settings(Machines, With("TileCacheMegabytes", "8")).Tiles.CacheMegabytes);

    // N-18 c5 and N-21: a number below its range, a fraction, or a word refuses the start, and names the key.
    [Theory]
    [InlineData("CacheTTL", "-1")]
    [InlineData("CacheTTL", "1.5")]
    [InlineData("CacheTTL", "\"soon\"")]
    [InlineData("DbPoolMaxConns", "0")]
    [InlineData("DbTimeout", "0")]
    [InlineData("CatalogueRefresh", "-5")]
    [InlineData("HttpPort", "70000")]
    public void Refuses_a_number_out_of_range_and_names_the_key(string key, string value) =>
        Assert.Contains(key, Problems(key == "HttpPort" ? Machines.SetItem("TESSERA_HTTPPORT", value) : Machines, With(key, value)));

    // DbPoolMaxConnLifeTime takes pg_tileserv's form of a time.
    [Theory]
    [InlineData("\"1h\"", 3600)]
    [InlineData("\"30m\"", 1800)]
    [InlineData("\"1h30m\"", 5400)]
    [InlineData("\"90s\"", 90)]
    public void Reads_a_time_in_pg_tileservs_form(string value, int seconds) =>
        Assert.Equal(TimeSpan.FromSeconds(seconds), Settings(Machines, With("DbPoolMaxConnLifeTime", value)).Db.PoolMaxConnLifeTime);

    [Theory]
    [InlineData("\"an hour\"")]
    [InlineData("\"\"")]
    [InlineData("\"0s\"")]
    [InlineData("\"25h\"")]
    [InlineData("3600")]
    public void Refuses_a_time_in_any_other_form(string value) =>
        Assert.Contains("DbPoolMaxConnLifeTime", Problems(Machines, With("DbPoolMaxConnLifeTime", value)));
}
