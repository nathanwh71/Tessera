using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Modules.Tessera;
using Modules.Tessera.Domain;
using Modules.Tessera.Infrastructure;
using Modules.Tessera.Infrastructure.Sql;

namespace Tessera.UnitTests;

/// <summary>
/// US-00 c4, N-21 and ADR 0003: settings come from the <b><i>settings file</i></b>, tessera.json,
/// and from each <b><i>environment variable</i></b>, checked at start-up.
/// </summary>
public sealed class SettingsTests
{
    /// <summary>
    /// A database URL whose every part differs from its default: a password holding an encoded '@'
    /// and the port 5433, not 5432, so a test sees each part read, not assumed (US-00 c4).
    /// </summary>
    private const string Url = "postgresql://tessera_reader:s%40cret@db.office:5433/tessera";

    /// <summary>
    /// The settings file the release ships, copied beside the tests, so the tests check
    /// the real file rather than a copy that could drift from it (N-21).
    /// </summary>
    private static string Shipped => File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "tessera.json"));

    // A host as the program builds one, with the file's text and the environment given here.
    private static IHost Host(string json, string? url = Url, Dictionary<string, string?>? environment = null)
    {
        var builder = Microsoft.Extensions.Hosting.Host.CreateEmptyApplicationBuilder(new HostApplicationBuilderSettings());
        builder.Configuration
            .AddJsonStream(new MemoryStream(Encoding.UTF8.GetBytes(json)))
            .AddInMemoryCollection(new Dictionary<string, string?>(environment ?? []) { [DatabaseConnections.ReaderDatabase] = url });
        TesseraModule.AddSettings(builder.Services);
        DatabaseConnections.Register(builder.Services, builder.Configuration, DatabaseConnections.ReaderDatabase);
        return builder.Build();
    }

    /// <summary>
    /// One section's settings, read after the start-up check, as the program reads them; a value
    /// the check would refuse fails the test here rather than passing unseen.
    /// </summary>
    private static T Settings<T>(string json, Dictionary<string, string?>? environment = null) where T : class
    {
        using var host = Host(json, environment: environment);
        Common.Infrastructure.Settings.Check(host.Services);
        return host.Services.GetRequiredService<IOptions<T>>().Value;
    }

    // Every problem the start-up check finds, as the program prints them.
    private static string Problems(string json, string? url = Url)
    {
        using var host = Host(json, url);
        try
        {
            Common.Infrastructure.Settings.Check(host.Services);
            return "";
        }
        catch (Exception error)
        {
            return string.Join("; ", Common.Infrastructure.Settings.Problems(error));
        }
    }

    [Fact]
    public void Reads_the_shipped_file()
    {
        Assert.Equal(60, Settings<TileSettings>(Shipped).CacheSeconds);
        Assert.Equal(15, Settings<AreaSettings>(Shipped).HomeLevel);
        Assert.Equal(30, Settings<CatalogueSettings>(Shipped).RefreshSeconds);
    }

    // A key or a whole section left out takes its default; an empty file runs on the defaults.
    [Fact]
    public void Gives_each_key_left_out_its_default()
    {
        Assert.Equal(4, Settings<DatabaseSettings>("{}").PoolSize);
        Assert.Equal("Macau", Settings<AreaSettings>("{}").Name);
    }

    // %40 is '@', percent-encoded so it cannot be mistaken for the '@' before the host.
    [Fact]
    public void Reads_the_database_url_into_a_connection_string() =>
        Assert.Equal(
            "Host=db.office;Port=5433;Database=tessera;Username=tessera_reader;Password=s@cret",
            Settings<DatabaseConnection>("{}").Parsed.Value.Text);

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

    // A key the service does not read is refused: it is most often a misspelling. A misspelt
    // section name is not checked; its keys bind to nothing (ADR 0003).
    [Fact]
    public void Refuses_a_key_it_does_not_know() =>
        Assert.Contains("'CacheSecond'", Problems("""{"Tiles":{"CacheSecond":60}}"""), StringComparison.Ordinal);

    [Fact]
    public void Leaves_a_misspelt_section_at_its_defaults() =>
        Assert.Equal(60, Settings<TileSettings>("""{"Tile":{"CacheSeconds":0}}""").CacheSeconds);

    // Every value out of range is named at once, so one failed start shows everything to fix.
    [Fact]
    public void Names_every_value_out_of_range_at_once()
    {
        var problems = Problems("""{"Tiles":{"CacheSeconds":-1},"Database":{"PoolSize":0},"Area":{"HomeLevel":23}}""", url: null);

        Assert.Contains("CacheSeconds", problems, StringComparison.Ordinal);
        Assert.Contains("PoolSize", problems, StringComparison.Ordinal);
        Assert.Contains("HomeLevel", problems, StringComparison.Ordinal);
        Assert.Contains("DATABASE_URL", problems, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("""{"Area":{"MinLongitude":113.7}}""", "west to east")]
    [InlineData("""{"Tiles":{"MinZoom":10,"MaxZoom":5}}""", "MaxZoom must not be below MinZoom")]
    [InlineData("""{"Tiles":{"CacheSeconds":"soon"}}""", "CacheSeconds")]
    public void Refuses_values_that_disagree(string json, string problem) =>
        Assert.Contains(problem, Problems(json), StringComparison.Ordinal);

    // Tiles__CacheSeconds in the environment reaches the configuration as Tiles:CacheSeconds, and wins over the file.
    [Fact]
    public void Lets_a_variable_override_a_key() =>
        Assert.Equal(0, Settings<TileSettings>(Shipped, new() { ["Tiles:CacheSeconds"] = "0" }).CacheSeconds);

    // ADR 0003: the shipped tessera.json holds no password, and listens on this machine alone.
    [Fact]
    public void Ships_a_file_with_no_password_that_listens_on_this_machine_alone()
    {
        Assert.DoesNotContain("assword", Shipped, StringComparison.Ordinal);
        Assert.DoesNotContain("postgresql://", Shipped, StringComparison.Ordinal);
        Assert.Contains("\"Urls\": \"http://127.0.0.1:7800\"", Shipped, StringComparison.Ordinal);
    }
}
