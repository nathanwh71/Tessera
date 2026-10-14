using System.Text;
using Microsoft.Extensions.Configuration;
using Tessera.Settings;

namespace Tessera.UnitTests;

/// <summary>US-00 c4 and ADR 0003: settings come from tessera.json and the environment, checked at start-up.</summary>
public sealed class SettingsTests
{
    private const string Url = "postgresql://tessera_reader:s%40cret@db.office:5433/tessera";

    private static Result<LoadedSettings, ImmutableArray<string>> Read(string json, string? url = Url, IDictionary<string, string?>? overrides = null) =>
        SettingsLoader.Read(
            new ConfigurationBuilder()
                .AddJsonStream(new MemoryStream(Encoding.UTF8.GetBytes(json)))
                .AddInMemoryCollection(overrides ?? new Dictionary<string, string?>())
                .Build(),
            SettingsLoader.ReaderDatabase,
            url);

    private static ServiceSettings Settings(string json, IDictionary<string, string?>? overrides = null) =>
        Read(json, overrides: overrides).Match(found => found.Settings, errors => throw new Xunit.Sdk.XunitException(string.Join("; ", errors)));

    private static string Problems(string json, string? url = Url) =>
        Read(json, url).Match(_ => "", errors => string.Join("; ", errors));

    private static string Shipped => File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "tessera.json"));

    [Fact]
    public void Reads_the_shipped_file()
    {
        var settings = Settings(Shipped);

        Assert.Equal(60, settings.Tiles.CacheSeconds);
        Assert.Equal(15, settings.Area.HomeLevel);
        Assert.Equal(30, settings.Catalogue.RefreshSeconds);
    }

    // A key or a whole section left out takes its default; an empty file runs on the defaults.
    [Fact]
    public void Gives_each_key_left_out_its_default()
    {
        var settings = Settings("{}");

        Assert.Equal(7800, settings.Http.Port);
        Assert.Equal("127.0.0.1", settings.Http.Host);
        Assert.Equal(4, settings.Database.PoolSize);
        Assert.Equal("Macau", settings.Area.Name);
    }

    // %40 is '@', percent-encoded so it cannot be mistaken for the '@' before the host.
    [Fact]
    public void Reads_the_database_url_into_a_connection_string() =>
        Assert.Equal(
            "Host=db.office;Port=5433;Database=tessera;Username=tessera_reader;Password=s@cret",
            Read("{}").Match(found => found.Database, _ => ""));

    // US-00 test 2: started with no database URL, the service names it.
    [Theory]
    [InlineData(null, "missing or empty")]
    [InlineData("", "missing or empty")]
    [InlineData("http://db/tessera", "expected postgresql://")]
    public void Names_a_missing_or_bad_database_url(string? url, string problem)
    {
        var problems = Problems("{}", url);

        Assert.Contains("DATABASE_URL", problems, StringComparison.Ordinal);
        Assert.Contains(problem, problems, StringComparison.Ordinal);
    }

    // A key the service does not read is refused: it is most often a misspelling of one it does.
    [Theory]
    [InlineData("""{"Tiles":{"CacheSecond":60}}""", "CacheSecond")]
    [InlineData("""{"Tile":{}}""", "Tile")]
    public void Refuses_a_key_it_does_not_know(string json, string key) =>
        Assert.Contains($"'{key}'", Problems(json), StringComparison.Ordinal);

    // Every problem is named at once, with its section, so one failed start shows everything to fix.
    [Fact]
    public void Names_every_value_out_of_range_at_once()
    {
        var problems = Problems("""{"Tiles":{"CacheSeconds":-1},"Database":{"PoolSize":0},"Area":{"HomeLevel":23}}""", url: null);

        Assert.Contains("Tiles: The field CacheSeconds", problems, StringComparison.Ordinal);
        Assert.Contains("Database: The field PoolSize", problems, StringComparison.Ordinal);
        Assert.Contains("Area: The field HomeLevel", problems, StringComparison.Ordinal);
        Assert.Contains("DATABASE_URL", problems, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("""{"Area":{"MinLongitude":113.7}}""", "west to east")]
    [InlineData("""{"Tiles":{"MinZoom":10,"MaxZoom":5}}""", "MaxZoom must not be below MinZoom")]
    [InlineData("""{"Http":{"Host":"office"}}""", "Host must be an address")]
    [InlineData("""{"Tiles":{"CacheSeconds":"soon"}}""", "CacheSeconds")]
    public void Refuses_values_that_disagree(string json, string problem) =>
        Assert.Contains(problem, Problems(json), StringComparison.Ordinal);

    // TESSERA__TILES__CACHESECONDS reaches the configuration as Tiles:CacheSeconds, and wins over the file.
    [Fact]
    public void Lets_a_variable_override_a_key() =>
        Assert.Equal(0, Settings(Shipped, new Dictionary<string, string?> { ["Tiles:CacheSeconds"] = "0" }).Tiles.CacheSeconds);

    // ADR 0003: the shipped tessera.json names no machine, no port and no password; the environment does.
    [Fact]
    public void Ships_a_file_with_no_machine_and_no_password()
    {
        Assert.DoesNotContain("://", Shipped, StringComparison.Ordinal);
        Assert.DoesNotContain("Http", Shipped, StringComparison.Ordinal);
        Assert.DoesNotContain("assword", Shipped, StringComparison.Ordinal);
    }
}
