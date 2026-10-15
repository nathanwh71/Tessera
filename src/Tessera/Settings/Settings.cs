using System.ComponentModel.DataAnnotations;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

// The Settings module: every setting the service and the seed read, with its default and its
// limits (US-00, ADR 0003). ASP.NET Core reads tessera.json and the environment, fills these records
// in, and checks them all when the program starts.

namespace Tessera.Settings;

/// <summary>
/// The limits on the service's use of the database (N-22). A <b><i>connection pool</i></b> keeps a few
/// connections open for requests to share, since opening a fresh one for each request is slow.
/// </summary>
public sealed record DatabaseSettings
{
    /// <summary>
    /// The most connections the pool holds open at once. Four let <b><i>tiles</i></b> build side by
    /// side yet keep the database's load small: two copies of the service hold eight in all (N-24).
    /// The cap of 1000 catches a slip of the keyboard.
    /// </summary>
    [Range(1, 1000)]
    public int PoolSize { get; init; } = 4;

    /// <summary>
    /// How long a connection lives before the pool replaces it, so no connection grows old holding
    /// the server's memory; an hour makes the cost of a fresh one small.
    /// </summary>
    [Range(1, int.MaxValue)]
    public int ConnectionLifetimeSeconds { get; init; } = 3600;

    /// <summary>
    /// The longest a query may run before the database cancels it, so a runaway query cannot hold a
    /// connection for ever, and its request answers 503 (N-22 c2); 0 means no limit.
    /// </summary>
    [Range(0, int.MaxValue)]
    public int QueryTimeoutSeconds { get; init; } = 10;

    /// <summary>
    /// The longest a request waits to open a connection or to borrow a free one; past it, the request
    /// answers 503 rather than waiting on, so the browser tries again later (N-22 c2).
    /// </summary>
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

    /// <summary>
    /// The most features one tile carries (US-07), so a crowded square at a low zoom level stays
    /// small and quick to build.
    /// </summary>
    [Range(1, int.MaxValue)]
    public int MaxFeatures { get; init; } = 10_000;

    /// <summary>
    /// The zoom levels served. 22 matches the map's last level (UI-09); 30 is the ceiling, since level
    /// 31 would overflow the 32-bit number that counts a level's columns (TileAddress).
    /// </summary>
    [Range(0, 30)]
    public int MinZoom { get; init; }

    [Range(0, 30)]
    public int MaxZoom { get; init; } = 22;

    /// <summary>How long a browser and Nginx may keep a tile; 0 means they check each time.</summary>
    [Range(0, int.MaxValue)]
    public int CacheSeconds { get; init; } = 60;

    /// <summary>
    /// Refuses a range that serves no level at all, so the slip shows at start (US-00 c4).
    /// </summary>
    public IEnumerable<ValidationResult> Validate(ValidationContext context) =>
        MaxZoom >= MinZoom ? [] : [new ValidationResult("MaxZoom must not be below MinZoom.")];
}

/// <summary>Whether the service shows its own map at <c>/</c> (US-23 c5).</summary>
public sealed record MapSettings
{
    public bool ShowPreview { get; init; } = true;
}

/// <summary>
/// How often the service rereads the list of tables it may read, so a table <b><i>granted</i></b>
/// later appears without a restart (N-23). 0 means it reads the list once.
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
    /// <summary>
    /// The area's name, which the page receives with the box; empty, it would name nothing.
    /// </summary>
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

    /// <summary>
    /// The zoom level the map opens at, from 0, the whole world, to 22, the map's last level (UI-09).
    /// </summary>
    [Range(0, 22)]
    public double HomeLevel { get; init; } = 15;

    /// <summary>
    /// Refuses a box turned inside out, so a swapped pair of values shows at start, not as an empty map.
    /// </summary>
    public IEnumerable<ValidationResult> Validate(ValidationContext context) =>
        MinLongitude < MaxLongitude && MinLatitude < MaxLatitude
            ? []
            : [new ValidationResult("The box must run west to east and south to north.")];
}

/// <summary>
/// Reads tessera.json, the <b><i>settings file</i></b>, and each <b><i>environment variable</i></b>
/// through ASP.NET Core's own configuration, binds each section to its record, and checks every
/// value when the program starts (US-00 c4).
/// </summary>
public static class TesseraSettings
{
    /// <summary>The database URL the service reads tiles with; pg_tileserv uses the same name.</summary>
    public const string ReaderDatabase = "DATABASE_URL";

    /// <summary>
    /// The database URL the seed writes places with. It signs in as a second <b><i>role</i></b>,
    /// tessera_publisher, so the service never holds the right to write while it serves tiles
    /// (ADR 0001).
    /// </summary>
    public const string PublisherDatabase = "TESSERA_PUBLISHER_DATABASE_URL";

    /// <summary>
    /// Adds tessera.json, beside the program, then the environment again, so a variable such as
    /// <c>Tiles__CacheSeconds=0</c> wins over the file; each '__' steps down a level.
    /// </summary>
    public static void AddFile(IConfigurationBuilder configuration) =>
        configuration
            .AddJsonFile(Path.Combine(AppContext.BaseDirectory, "tessera.json"), optional: true)
            .AddEnvironmentVariables();

    /// <summary>
    /// Binds each section to its record, with the record's own limits. A key the service does not
    /// read is refused, since it is most often a misspelling. A misspelt section name passes unseen:
    /// its keys bind to nothing, so the section keeps its defaults. The database's URL is the Sql
    /// <b><i>module</i></b>'s to read (DatabaseConnections.Register), since only Sql knows the
    /// database client.
    /// </summary>
    public static void Register(IServiceCollection services)
    {
        Section<DatabaseSettings>(services, "Database");
        Section<TileSettings>(services, "Tiles");
        Section<MapSettings>(services, "Map");
        Section<CatalogueSettings>(services, "Catalogue");
        Section<AreaSettings>(services, "Area");
    }

    /// <summary>
    /// Binds one section by the rules Register sets out: an unknown key refused, and every limit
    /// checked as the program starts, so one failed start names every problem (US-00 c4).
    /// </summary>
    private static void Section<T>(IServiceCollection services, string name) where T : class =>
        services.AddOptions<T>()
            .BindConfiguration(name, binder => binder.ErrorOnUnknownConfiguration = true)
            .ValidateDataAnnotations()
            .ValidateOnStart();

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
}
