using System.ComponentModel.DataAnnotations;
using CSharpFunctionalExtensions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Npgsql;

// The Settings module: every setting the service and the import read, with its default and its
// limits (US-00, ADR 0003). ASP.NET Core reads tessera.json and the environment, fills these records
// in, and checks them all when the program starts.

namespace Tessera.Settings;

/// <summary>
/// The limits on the service's use of the database (N-22). A pool keeps a few connections open for
/// requests to share, since opening a fresh one for each request is slow.
/// </summary>
public sealed record DatabaseSettings
{
    /// <summary>The most connections the pool holds open at once.</summary>
    [Range(1, 1000)]
    public int PoolSize { get; init; } = 4;

    /// <summary>How long a connection lives before the pool replaces it.</summary>
    [Range(1, int.MaxValue)]
    public int ConnectionLifetimeSeconds { get; init; } = 3600;

    /// <summary>The longest a query may run before the database cancels it; 0 means no limit.</summary>
    [Range(0, int.MaxValue)]
    public int QueryTimeoutSeconds { get; init; } = 10;

    /// <summary>The longest a request waits to open a connection or to borrow a free one.</summary>
    [Range(1, 600)]
    public int ConnectionWaitSeconds { get; init; } = 3;
}

/// <summary>
/// One tile's limits and how long a browser and Nginx may keep it (US-07, N-18). A tile is
/// Resolution units wide; the buffer is a margin drawn beyond each edge, so a symbol near the edge
/// shows on both tiles.
/// </summary>
public sealed record TileSettings : IValidatableObject
{
    [Range(1, int.MaxValue)]
    public int Resolution { get; init; } = 4096;

    [Range(0, int.MaxValue)]
    public int Buffer { get; init; } = 256;

    [Range(1, int.MaxValue)]
    public int MaxFeatures { get; init; } = 10_000;

    [Range(0, 30)]
    public int MinZoom { get; init; }

    [Range(0, 30)]
    public int MaxZoom { get; init; } = 22;

    /// <summary>How long a browser and Nginx may keep a tile; 0 means they check each time.</summary>
    [Range(0, int.MaxValue)]
    public int CacheSeconds { get; init; } = 60;

    public IEnumerable<ValidationResult> Validate(ValidationContext context) =>
        MaxZoom >= MinZoom ? [] : [new ValidationResult("MaxZoom must not be below MinZoom.")];
}

/// <summary>Whether the service shows its own map at <c>/</c> (US-23 c5).</summary>
public sealed record MapSettings
{
    public bool ShowPreview { get; init; } = true;
}

/// <summary>
/// How often the service rereads the list of tables it may read, so a table granted later appears
/// without a restart (N-23). 0 means it reads the list once.
/// </summary>
public sealed record CatalogueSettings
{
    [Range(0, int.MaxValue)]
    public int RefreshSeconds { get; init; } = 30;
}

/// <summary>
/// The place the tiles cover, in degrees, and where the map opens. The office sets it to Macau.
/// Web Mercator, the web map's projection, stops near 85 degrees north and south.
/// </summary>
public sealed record AreaSettings : IValidatableObject
{
    [Required(AllowEmptyStrings = false)]
    public string Name { get; init; } = "Macau";

    [Range(-180, 180)]
    public double MinLongitude { get; init; } = 113.52;

    [Range(-85.06, 85.06)]
    public double MinLatitude { get; init; } = 22.10;

    [Range(-180, 180)]
    public double MaxLongitude { get; init; } = 113.61;

    [Range(-85.06, 85.06)]
    public double MaxLatitude { get; init; } = 22.22;

    [Range(-180, 180)]
    public double HomeLongitude { get; init; } = 113.55;

    [Range(-85.06, 85.06)]
    public double HomeLatitude { get; init; } = 22.19;

    /// <summary>The zoom level the map opens at, from 0, the whole world, to 22.</summary>
    [Range(0, 22)]
    public double HomeLevel { get; init; } = 15;

    /// <summary>True when a point lies inside the box, edges included; the import refuses any other.</summary>
    public bool Contains(double longitude, double latitude) =>
        longitude >= MinLongitude && longitude <= MaxLongitude && latitude >= MinLatitude && latitude <= MaxLatitude;

    public IEnumerable<ValidationResult> Validate(ValidationContext context) =>
        MinLongitude < MaxLongitude && MinLatitude < MaxLatitude ? [] : [new ValidationResult("The box must run west to east and south to north.")];
}

/// <summary>The database's connection, from the URL in <see cref="Variable"/>, checked at start-up.</summary>
public sealed class DatabaseConnection : IValidatableObject
{
    /// <summary>The environment variable that held the URL, named in any problem.</summary>
    public string Variable { get; set; } = TesseraSettings.ReaderDatabase;

    public string? Url { get; set; }

    /// <summary>Npgsql's connection string, or empty text while the URL fails the check.</summary>
    public string Text => TesseraSettings.ConnectionString(Url).GetValueOrDefault("");

    public IEnumerable<ValidationResult> Validate(ValidationContext context) =>
        TesseraSettings.ConnectionString(Url) is { IsFailure: true } refused ? [new ValidationResult($"{Variable}: {refused.Error}")] : [];
}

/// <summary>
/// Reads tessera.json and the environment through ASP.NET Core's own configuration, binds each
/// section to its record, and checks every value when the program starts (US-00 c4).
/// </summary>
public static class TesseraSettings
{
    /// <summary>The database URL the service reads tiles with; pg_tileserv uses the same name.</summary>
    public const string ReaderDatabase = "DATABASE_URL";

    /// <summary>The database URL the import writes places with.</summary>
    public const string PublisherDatabase = "TESSERA_PUBLISHER_DATABASE_URL";

    // The sections tessera.json may hold. Urls and Logging are ASP.NET Core's own.
    private static readonly string[] Sections = ["Database", "Tiles", "Map", "Catalogue", "Area", "Logging", "Urls"];

    /// <summary>
    /// Adds tessera.json, beside the program, then the environment again, so a variable such as
    /// <c>Tiles__CacheSeconds=0</c> wins over the file; each '__' steps down a level.
    /// </summary>
    public static void AddFile(IConfigurationBuilder configuration) =>
        configuration.AddJsonFile(Path.Combine(AppContext.BaseDirectory, "tessera.json"), optional: true).AddEnvironmentVariables();

    /// <summary>
    /// Binds each section to its record, with the record's own limits, and the database URL from
    /// <paramref name="databaseVariable"/>. A key the service does not read is refused, since it is
    /// most often a misspelling; so is a section.
    /// </summary>
    public static void Register(IServiceCollection services, IConfiguration configuration, string databaseVariable)
    {
        Section<DatabaseSettings>(services, "Database");
        Section<TileSettings>(services, "Tiles");
        Section<MapSettings>(services, "Map");
        Section<CatalogueSettings>(services, "Catalogue");
        Section<AreaSettings>(services, "Area");
        services.AddOptions<DatabaseConnection>()
            .Configure(connection => (connection.Variable, connection.Url) = (databaseVariable, configuration[databaseVariable]))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        // The sections the JSON itself names; the environment holds many other names, all left alone.
        var unknown = ((IConfigurationRoot)configuration).Providers
            .Where(provider => provider is JsonConfigurationProvider or JsonStreamConfigurationProvider)
            .SelectMany(provider => provider.GetChildKeys([], null))
            .Where(name => !Sections.Contains(name, StringComparer.OrdinalIgnoreCase))
            .Distinct()
            .ToArray();
        services.AddOptions<SectionCheck>()
            .Validate(_ => unknown.Length == 0, $"tessera.json: no section is named {string.Join(", ", unknown.Select(name => $"'{name}'"))}.")
            .ValidateOnStart();
    }

    private static void Section<T>(IServiceCollection services, string name) where T : class =>
        services.AddOptions<T>()
            .BindConfiguration(name, binder => binder.ErrorOnUnknownConfiguration = true)
            .ValidateDataAnnotations()
            .ValidateOnStart();

    private sealed class SectionCheck;

    /// <summary>Checks every section at once; throws with every problem found.</summary>
    public static void Check(IServiceProvider services) => services.GetRequiredService<IStartupValidator>().Validate();

    /// <summary>
    /// Each problem a failed check names: a value out of its limits, from the record's checks, or a
    /// key that will not bind, from the binder.
    /// </summary>
    public static IEnumerable<string> Problems(Exception error) => error switch
    {
        OptionsValidationException failed => failed.Failures,
        AggregateException many => many.InnerExceptions.SelectMany(Problems),
        _ => [error.Message],
    };

    /// <summary>
    /// Turns <c>postgresql://user:password@host:port/database</c> into Npgsql's connection string;
    /// with no port, PostgreSQL's usual 5432. A password may hold any character, so a character such
    /// as '@' arrives percent-encoded, here %40.
    /// </summary>
    public static Result<string, string> ConnectionString(string? url)
    {
        if (string.IsNullOrEmpty(url))
            return Result.Failure<string, string>("missing or empty");
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri) || uri.Scheme is not ("postgresql" or "postgres")
            || uri.UserInfo.Split(':', 2) is not [{ Length: > 0 } user, var password] || uri.AbsolutePath.Length < 2)
            return Result.Failure<string, string>("expected postgresql://user:password@host:port/database");
        return Result.Success<string, string>(new NpgsqlConnectionStringBuilder
        {
            Host = uri.Host,
            Port = uri.IsDefaultPort || uri.Port < 0 ? 5432 : uri.Port,
            Database = Uri.UnescapeDataString(uri.AbsolutePath[1..]),
            Username = Uri.UnescapeDataString(user),
            Password = Uri.UnescapeDataString(password),
        }.ConnectionString);
    }
}
