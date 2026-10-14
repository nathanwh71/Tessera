using System.Collections.Immutable;
using System.ComponentModel.DataAnnotations;
using System.Net;
using Microsoft.Extensions.Configuration;
using Npgsql;
using Tessera.Kernel;

// The Settings module: every setting the service and the import read, with its default and its
// limits (US-00, ADR 0003). ASP.NET Core's configuration reads tessera.json and the environment and
// fills these records in; SettingsLoader checks them and names every problem at once.

namespace Tessera.Settings;

/// <summary>Where the service listens. Nginx, in front, reaches it on this machine alone (ADR 0008).</summary>
public sealed record HttpSettings : IValidatableObject
{
    public string Host { get; init; } = "127.0.0.1";

    [Range(1, 65_535)]
    public int Port { get; init; } = 7800;

    public IEnumerable<ValidationResult> Validate(ValidationContext context) =>
        IPAddress.TryParse(Host, out _) ? [] : [new ValidationResult("Host must be an address such as 127.0.0.1.")];
}

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

/// <summary>Everything in tessera.json. Each section may be left out, and then takes its defaults.</summary>
public sealed record ServiceSettings
{
    public HttpSettings Http { get; init; } = new();

    public DatabaseSettings Database { get; init; } = new();

    public TileSettings Tiles { get; init; } = new();

    public MapSettings Map { get; init; } = new();

    public CatalogueSettings Catalogue { get; init; } = new();

    public AreaSettings Area { get; init; } = new();
}

/// <summary>The settings and the database's connection string, both checked.</summary>
public sealed record LoadedSettings(ServiceSettings Settings, string Database);

/// <summary>
/// Reads tessera.json and the environment, checks every value, and names every problem at once, so
/// one failed start tells the maintainer everything to fix (US-00 c4).
/// </summary>
public static class SettingsLoader
{
    /// <summary>The path of the settings file, when it is not tessera.json beside the program.</summary>
    public const string ConfigVariable = "TESSERA_CONFIG";

    /// <summary>
    /// A variable that starts with this overrides one key: <c>TESSERA__TILES__CACHESECONDS=0</c> sets
    /// <c>Tiles.CacheSeconds</c>. Each '__' steps down a level.
    /// </summary>
    public const string OverridePrefix = "TESSERA__";

    /// <summary>The database URL the service reads tiles with; pg_tileserv uses the same name.</summary>
    public const string ReaderDatabase = "DATABASE_URL";

    /// <summary>The database URL the import writes places with.</summary>
    public const string PublisherDatabase = "TESSERA_PUBLISHER_DATABASE_URL";

    /// <summary>
    /// Reads the real file and environment. <paramref name="databaseVariable"/> names the variable
    /// that holds the database URL: the service's or the import's.
    /// </summary>
    public static Result<LoadedSettings, ImmutableArray<string>> Load(string databaseVariable)
    {
        var path = Environment.GetEnvironmentVariable(ConfigVariable) is { Length: > 0 } named
            ? named
            : Path.Combine(AppContext.BaseDirectory, "tessera.json");
        try
        {
            var configuration = new ConfigurationBuilder()
                .AddJsonFile(path, optional: true)
                .AddEnvironmentVariables(OverridePrefix)
                .Build();
            return Read(configuration, databaseVariable, Environment.GetEnvironmentVariable(databaseVariable));
        }
        catch (Exception error) when (error is FormatException or InvalidDataException)
        {
            // A file that is not JSON fails as it is read.
            return Result.Refuse<LoadedSettings, ImmutableArray<string>>([$"{path}: {error.Message}"]);
        }
    }

    /// <summary>
    /// Checks the settings in <paramref name="configuration"/> and the database URL. A key the
    /// service does not read is refused: it is most often a misspelling of one it does.
    /// </summary>
    public static Result<LoadedSettings, ImmutableArray<string>> Read(IConfiguration configuration, string databaseVariable, string? databaseUrl)
    {
        var problems = ImmutableArray.CreateBuilder<string>();
        var settings = new ServiceSettings();
        try
        {
            settings = configuration.Get<ServiceSettings>(binder => binder.ErrorOnUnknownConfiguration = true) ?? settings;
        }
        catch (InvalidOperationException error)
        {
            problems.Add(error.Message);
        }

        problems.AddRange(Check("Http", settings.Http));
        problems.AddRange(Check("Database", settings.Database));
        problems.AddRange(Check("Tiles", settings.Tiles));
        problems.AddRange(Check("Catalogue", settings.Catalogue));
        problems.AddRange(Check("Area", settings.Area));

        var database = ConnectionString(databaseUrl);
        database.Match(_ => 0, problem => { problems.Add($"{databaseVariable}: {problem}"); return 0; });

        return problems.Count == 0
            ? Result.Ok<LoadedSettings, ImmutableArray<string>>(new LoadedSettings(settings, database.Match(text => text, _ => "")))
            : Result.Refuse<LoadedSettings, ImmutableArray<string>>(problems.ToImmutable());
    }

    // The attributes on each record, such as [Range], and its own Validate method.
    private static IEnumerable<string> Check(string section, object values)
    {
        var results = new List<ValidationResult>();
        Validator.TryValidateObject(values, new ValidationContext(values), results, validateAllProperties: true);
        return results.Select(result => $"{section}: {result.ErrorMessage}");
    }

    /// <summary>
    /// Turns <c>postgresql://user:password@host:port/database</c> into Npgsql's connection string;
    /// with no port, PostgreSQL's usual 5432. A password may hold any character, so a character such
    /// as '@' arrives percent-encoded, here %40.
    /// </summary>
    public static Result<string, string> ConnectionString(string? url)
    {
        if (string.IsNullOrEmpty(url))
            return Result.Refuse<string, string>("missing or empty");
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri) || uri.Scheme is not ("postgresql" or "postgres")
            || uri.UserInfo.Split(':', 2) is not [{ Length: > 0 } user, var password] || uri.AbsolutePath.Length < 2)
            return Result.Refuse<string, string>("expected postgresql://user:password@host:port/database");
        return Result.Ok<string, string>(new NpgsqlConnectionStringBuilder
        {
            Host = uri.Host,
            Port = uri.IsDefaultPort || uri.Port < 0 ? 5432 : uri.Port,
            Database = Uri.UnescapeDataString(uri.AbsolutePath[1..]),
            Username = Uri.UnescapeDataString(user),
            Password = Uri.UnescapeDataString(password),
        }.ConnectionString);
    }
}
