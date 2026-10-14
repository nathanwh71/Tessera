using Tessera.Settings;

namespace Tessera.UnitTests;

/// <summary>
/// US-00 c4, ADR 0003, N-21 and ADR 0006 rule 5: settings come from the environment and the one
/// tessera.json both builds read, in pg_tileserv's names, checked at start-up.
/// </summary>
public sealed class SettingsTests
{
    private const string File = """
        {
          "ShowPreview": true,
          "MaxFeaturesPerTile": 10000,
          "DefaultBuffer": 64,
          "Area": {
            "Name": "Macau",
            "MinLongitude": 113.52, "MinLatitude": 22.10, "MaxLongitude": 113.61, "MaxLatitude": 22.22,
            "HomeLongitude": 113.55, "HomeLatitude": 22.19, "HomeLevel": 14
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

    private static string Shipped => System.IO.File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "tessera.json"));

    [Fact]
    public void Reads_every_setting()
    {
        var settings = Settings(Machines);

        Assert.Equal("Macau", settings.Area.Name);
        Assert.Equal(new AreaBox(113.52, 22.10, 113.61, 22.22), settings.Area.Box);
        Assert.Equal(14, settings.Area.Home.Zoom);
        Assert.Equal(8080, settings.Http.Port);
        // %40 is '@', percent-encoded so it cannot be mistaken for the '@' before the host.
        Assert.Equal(new DatabaseAddress("db.office", 5433, "tessera", "tessera_reader", "s@cret"), settings.Db.Address);
        Assert.Equal(new TileSettings(10000, 64, 0, 64), settings.Tiles);
    }

    // ADR 0006 rule 5: a key left out takes the Java build's default, which is pg_tileserv's where it has one.
    [Fact]
    public void Gives_each_key_the_file_leaves_out_the_java_builds_default()
    {
        var settings = Settings(Machines.Remove("TESSERA_HTTPPORT"), "{}");

        Assert.True(settings.Map.Preview);
        Assert.Equal(new TileSettings(10000, 256, 0, 64, 4096, 0, 22), settings.Tiles);
        Assert.Equal((System.Net.IPAddress.Any, 7800, 8192, 100), (settings.Http.Host, settings.Http.Port, settings.Http.MaxConnections, settings.Http.AcceptCount));
        Assert.Equal((4, TimeSpan.FromHours(1), TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(3)),
            (settings.Db.PoolMaxConns, settings.Db.PoolMaxConnLifeTime, settings.Db.Timeout, settings.Db.PoolConnectionTimeout));
        Assert.Equal(TimeSpan.FromSeconds(30), settings.Catalogue.Refresh);
        Assert.Equal(new Area("Macau", new AreaBox(113.52, 22.10, 113.61, 22.22), new HomeView(113.55, 22.19, 15)), settings.Area);
    }

    // As in pg_tileserv, a missing file is no fault: the service runs on the defaults.
    [Fact]
    public void Runs_on_the_defaults_when_the_file_is_missing() =>
        Assert.Equal(7800, Settings(Machines.Remove("TESSERA_HTTPPORT"), file: null).Http.Port);

    // US-00 test 2: started with no variables set, the service names each one it lacks.
    [Fact]
    public void Names_the_missing_database() =>
        Assert.Contains("DATABASE_URL", Problems(ImmutableDictionary<string, string>.Empty));

    [Fact]
    public void Treats_an_empty_variable_as_missing() =>
        Assert.Contains("DATABASE_URL", Problems(Machines.SetItem("DATABASE_URL", "")));

    // ADR 0006 rule 5: a key neither build reads is refused, since it is most often a misspelling.
    [Theory]
    [InlineData("CacheTtl")]
    [InlineData("Tiles")]
    public void Refuses_a_key_it_does_not_know(string key) =>
        Assert.Contains(key, Problems(Machines, With(key, "1")));

    [Fact]
    public void Refuses_an_area_key_it_does_not_know() =>
        Assert.Contains("Area.Zoom", Problems(Machines, File.Replace("\"HomeLevel\": 14", "\"Zoom\": 14", StringComparison.Ordinal)));

    // N-21 c3: TESSERA_ and the key, in any case, overrides it, as pg_tileserv's TS_HTTPPORT does.
    [Theory]
    [InlineData("TESSERA_DEFAULTBUFFER")]
    [InlineData("TESSERA_DefaultBuffer")]
    [InlineData("tessera_defaultbuffer")]
    public void Lets_a_variable_override_a_key_in_any_case(string name) =>
        Assert.Equal(32, Settings(Machines.SetItem(name, "32")).Tiles.Buffer);

    [Fact]
    public void Lets_a_variable_override_an_area_key() =>
        Assert.Equal(17, Settings(Machines.SetItem("TESSERA_AREA__HOMELEVEL", "17")).Area.Home.Zoom);

    [Fact]
    public void Leaves_a_variable_that_names_no_key_alone() =>
        Assert.Equal(64, Settings(Machines.SetItem("TESSERA_DB_PASSWORD", "x").SetItem("TESSERA_HTTP_PORT", "9")).Tiles.Buffer);

    [Fact]
    public void Names_a_bad_area() =>
        Assert.Contains("Area.HomeLevel", Problems(Machines.SetItem("TESSERA_AREA__HOMELEVEL", "40")));

    [Fact]
    public void Refuses_a_box_that_runs_backwards() =>
        Assert.Contains("Area", Problems(Machines.SetItem("TESSERA_AREA__MINLONGITUDE", "114")));

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
        var keys = System.Text.Json.Nodes.JsonNode.Parse(Shipped)!.AsObject().SelectMany(Keys);

        Assert.DoesNotContain("://", Shipped, StringComparison.Ordinal);
        Assert.DoesNotContain(keys, key => new[] { "password", "httphost", "url", "httpport", "user" }.Any(word => key.Contains(word, StringComparison.OrdinalIgnoreCase)));
        Assert.True(Read(Machines, Shipped).Match(_ => true, _ => false));
    }

    // ADR 0006 rule 5: the shipped file is the Java build's, byte for byte; this is its fingerprint.
    [Fact]
    public void Ships_the_same_file_as_the_java_build() =>
        Assert.Equal(
            "2757f82b",
            Convert.ToHexStringLower(System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(Shipped.ReplaceLineEndings("\n"))))[..8]);

    private static IEnumerable<string> Keys(KeyValuePair<string, System.Text.Json.Nodes.JsonNode?> entry) =>
        entry.Value is System.Text.Json.Nodes.JsonObject child
            ? child.SelectMany(Keys).Prepend(entry.Key)
            : [entry.Key];

    // N-18 c1 and c5: CacheTTL may be left out, and then a browser is told to check each time.
    [Theory]
    [InlineData("0", 0)]
    [InlineData("300", 300)]
    public void Reads_cache_ttl(string value, int expected) =>
        Assert.Equal(expected, Settings(Machines, With("CacheTTL", value)).Tiles.CacheSeconds);

    [Fact]
    public void Reads_tile_cache_megabytes() =>
        Assert.Equal(8, Settings(Machines, With("TileCacheMegabytes", "8")).Tiles.CacheMegabytes);

    [Fact]
    public void Reads_the_zoom_range_and_resolution() =>
        Assert.Equal((512, 10, 18), Settings(Machines, With("DefaultResolution", "512").Replace("\"ShowPreview\": true,", "\"ShowPreview\": true, \"DefaultMinZoom\": 10, \"DefaultMaxZoom\": 18,", StringComparison.Ordinal))
            .Tiles is var tiles ? (tiles.Resolution, tiles.MinZoom, tiles.MaxZoom) : default);

    [Fact]
    public void Refuses_a_highest_zoom_below_the_lowest() =>
        Assert.Contains("DefaultMaxZoom", Problems(Machines, With("DefaultMinZoom", "12").Replace("\"ShowPreview\": true,", "\"ShowPreview\": true, \"DefaultMaxZoom\": 10,", StringComparison.Ordinal)));

    // N-18 c5 and N-21: a number below its range, a fraction, or a word refuses the start, and names the key.
    [Theory]
    [InlineData("CacheTTL", "-1")]
    [InlineData("CacheTTL", "1.5")]
    [InlineData("CacheTTL", "\"soon\"")]
    [InlineData("DbPoolMaxConns", "0")]
    [InlineData("DbTimeout", "-1")]
    [InlineData("DbPoolConnectionTimeout", "100")]
    [InlineData("CatalogueRefresh", "-5")]
    [InlineData("HttpPort", "70000")]
    [InlineData("HttpHost", "\"office\"")]
    public void Refuses_a_value_out_of_range_and_names_the_key(string key, string value) =>
        Assert.Contains(key, Problems(key == "HttpPort" ? Machines.SetItem("TESSERA_HTTPPORT", value) : Machines, With(key, value)));

    // The pool's wait is in milliseconds, as the Java build measured it.
    [Fact]
    public void Reads_the_pools_wait_in_milliseconds() =>
        Assert.Equal(TimeSpan.FromMilliseconds(1500), Settings(Machines, With("DbPoolConnectionTimeout", "1500")).Db.PoolConnectionTimeout);

    // DbPoolMaxConnLifeTime takes a whole number of seconds, or pg_tileserv's form of a time.
    [Theory]
    [InlineData("3600", 3600)]
    [InlineData("\"1h\"", 3600)]
    [InlineData("\"30m\"", 1800)]
    [InlineData("\"1h30m\"", 5400)]
    [InlineData("\"90s\"", 90)]
    public void Reads_a_time_in_either_form(string value, int seconds) =>
        Assert.Equal(TimeSpan.FromSeconds(seconds), Settings(Machines, With("DbPoolMaxConnLifeTime", value)).Db.PoolMaxConnLifeTime);

    [Theory]
    [InlineData("\"an hour\"")]
    [InlineData("\"\"")]
    [InlineData("-1")]
    public void Refuses_a_time_in_any_other_form(string value) =>
        Assert.Contains("DbPoolMaxConnLifeTime", Problems(Machines, With("DbPoolMaxConnLifeTime", value)));
}
