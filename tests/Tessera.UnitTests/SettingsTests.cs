using Tessera.Settings;

namespace Tessera.UnitTests;

/// <summary>US-00 c4 and ADR 0003: settings come from the environment and tessera.json, checked at start-up.</summary>
public sealed class SettingsTests
{
    private const string File = """
        {
          "Area": {
            "Name": "Macau",
            "Box": { "West": 113.52, "South": 22.10, "East": 113.61, "North": 22.22 },
            "Home": { "Longitude": 113.55, "Latitude": 22.19, "Zoom": 14 }
          },
          "Map": { "Preview": true },
          "Tiles": { "MaxFeatures": 10000, "Buffer": 64 }
        }
        """;

    private static readonly ImmutableDictionary<string, string> Machines = new Dictionary<string, string>
    {
        ["TESSERA_HTTP_PORT"] = "8080",
        ["DATABASE_URL"] = "postgresql://tessera_reader:s%40cret@db.office:5433/tessera",
    }.ToImmutableDictionary();

    private static Result<ServiceSettings, ImmutableArray<SettingsError>> Read(IReadOnlyDictionary<string, string> environment, string? file = File) =>
        SettingsReader.ReadService(environment, "tessera.json", file.ToOption());

    [Fact]
    public void Reads_every_setting()
    {
        var settings = Read(Machines).Match(found => found, errors => throw new Xunit.Sdk.XunitException(string.Join("; ", errors)));

        Assert.Equal("Macau", settings.Area.Name);
        Assert.Equal(new AreaBox(113.52, 22.10, 113.61, 22.22), settings.Area.Box);
        Assert.Equal(8080, settings.Port);
        // %40 is '@', percent-encoded so it cannot be mistaken for the '@' before the host.
        Assert.Equal(new DatabaseAddress("db.office", 5433, "tessera", "tessera_reader", "s@cret"), settings.Database);
        Assert.Equal(new TileSettings(10000, 64, 60, 64), settings.Tiles);
    }

    // US-00 test 2: started with no variables set, the service names each one it lacks.
    [Fact]
    public void Names_each_missing_variable()
    {
        var errors = Read(ImmutableDictionary<string, string>.Empty).Match(_ => [], found => found);

        Assert.Contains(errors, error => error.Name == "TESSERA_HTTP_PORT");
        Assert.Contains(errors, error => error.Name == "DATABASE_URL");
    }

    [Fact]
    public void Treats_an_empty_variable_as_missing() =>
        Assert.Contains(
            Read(Machines.SetItem("DATABASE_URL", "")).Match(_ => [], found => found),
            error => error.Name == "DATABASE_URL");

    [Fact]
    public void Names_a_missing_file() =>
        Assert.Contains(Read(Machines, file: null).Match(_ => [], found => found), error => error.Name == "tessera.json");

    [Fact]
    public void Lets_a_variable_override_a_key_in_the_file() =>
        Assert.Equal(
            32,
            Read(Machines.SetItem("TESSERA_Tiles__Buffer", "32")).Match(found => found.Tiles.Buffer, _ => -1));

    [Fact]
    public void Names_a_bad_key() =>
        Assert.Contains(
            Read(Machines.SetItem("TESSERA_Area__Home__Zoom", "40")).Match(_ => [], found => found),
            error => error.Name == "Area.Home.Zoom");

    [Fact]
    public void Refuses_a_key_written_twice() =>
        Assert.Contains(
            Read(Machines, """{ "Area": { "Name": "a", "Name": "b" } }""").Match(_ => [], found => found),
            error => error.Name == "tessera.json");

    // A password in the address is never printed: a log line may reach someone it should not.
    [Fact]
    public void Hides_the_password_when_printed() =>
        Assert.DoesNotContain("secret", new DatabaseAddress("db", 5432, "tessera", "reader", "secret").ToString());

    // ADR 0003: the shipped tessera.json names no machine and holds no password; the environment does.
    [Fact]
    public void Ships_a_file_with_no_machine_and_no_password()
    {
        var shipped = System.IO.File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "tessera.json"));
        var keys = System.Text.Json.Nodes.JsonNode.Parse(shipped)!.AsObject().SelectMany(Keys);

        Assert.DoesNotContain("://", shipped, StringComparison.Ordinal);
        Assert.DoesNotContain(keys, key => new[] { "password", "host", "url", "port", "user" }.Any(word => key.Contains(word, StringComparison.OrdinalIgnoreCase)));
        Assert.True(Read(Machines, shipped).Match(_ => true, _ => false));
    }

    private static IEnumerable<string> Keys(KeyValuePair<string, System.Text.Json.Nodes.JsonNode?> entry) =>
        entry.Value is System.Text.Json.Nodes.JsonObject child
            ? child.SelectMany(Keys).Prepend(entry.Key)
            : [entry.Key];

    // N-18 c1 and c5: CacheSeconds may be left out, and then a browser keeps a tile for a minute;
    // a negative or broken number refuses the start.
    [Theory]
    [InlineData("\"MaxFeatures\": 10000, \"Buffer\": 64", 60)]
    [InlineData("\"MaxFeatures\": 10000, \"Buffer\": 64, \"CacheSeconds\": 0", 0)]
    [InlineData("\"MaxFeatures\": 10000, \"Buffer\": 64, \"CacheSeconds\": 300", 300)]
    public void Reads_cache_seconds_with_a_minute_as_the_default(string tiles, int expected) =>
        Assert.Equal(expected, Read(Machines, File.Replace("\"MaxFeatures\": 10000, \"Buffer\": 64", tiles, StringComparison.Ordinal))
            .Match(found => found.Tiles.CacheSeconds, _ => -1));

    // N-19 c5: the room for kept copies is 64 MB unless the file says otherwise.
    [Fact]
    public void Reads_cache_megabytes_with_64_as_the_default() =>
        Assert.Equal((64, 8), Read(Machines, File.Replace("\"Buffer\": 64", "\"Buffer\": 64, \"CacheMegabytes\": 8", StringComparison.Ordinal))
            .Match(found => (Read(Machines).Match(plain => plain.Tiles.CacheMegabytes, _ => -1), found.Tiles.CacheMegabytes), _ => (-1, -1)));

    [Theory]
    [InlineData("-1")]
    [InlineData("1.5")]
    [InlineData("\"soon\"")]
    public void Refuses_a_cache_seconds_that_is_not_a_whole_number_from_zero(string value) =>
        Assert.Contains(
            Read(Machines, File.Replace("\"Buffer\": 64", $"\"Buffer\": 64, \"CacheSeconds\": {value}", StringComparison.Ordinal))
                .Match(_ => [], errors => errors.Select(error => error.Name)),
            name => name.StartsWith("Tiles", StringComparison.Ordinal));
}
