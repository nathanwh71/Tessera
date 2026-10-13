using Tessera.Kernel;

namespace Tessera.Settings;

/// <summary>The area's edges in degrees of longitude (west, east) and latitude (south, north).</summary>
public sealed record AreaBox(double West, double South, double East, double North)
{
    /// <summary>True when a point in longitude and latitude lies inside the box, edges included.</summary>
    public bool Contains(double longitude, double latitude) =>
        longitude >= West && longitude <= East && latitude >= South && latitude <= North;
}

/// <summary>Where the map opens: a centre in degrees and a zoom level from 0 to 22.</summary>
public sealed record HomeView(double Longitude, double Latitude, double Zoom);

/// <summary>The place the tiles cover. The stories call it 'the area'; the office sets it to Macau.</summary>
public sealed record Area(string Name, AreaBox Box, HomeView Home);

/// <summary>Whether the service shows its own map at <c>/</c> (US-23 c5): pg_tileserv's <c>ShowPreview</c>.</summary>
public sealed record MapSettings(bool Preview);

/// <summary>
/// One tile's limits and how long a copy of it lives (US-07, N-18, N-19). Each field reads a key
/// with pg_tileserv's name: <c>MaxFeaturesPerTile</c>, <c>DefaultBuffer</c>, <c>CacheTTL</c>, and
/// Tessera's own <c>TileCacheMegabytes</c>.
/// </summary>
/// <remarks>
/// A tile is 4096 units wide, so a buffer of 64 reaches 1/64 of a tile beyond each edge.
/// CacheSeconds is how long a browser may keep a tile, and how long the service keeps its own copy;
/// 0 means neither keeps one. CacheMegabytes caps the memory the service's copies take together.
/// </remarks>
public sealed record TileSettings(int MaxFeatures, int Buffer, int CacheSeconds, int CacheMegabytes);

/// <summary>Where the service listens: pg_tileserv's <c>HttpPort</c>.</summary>
public sealed record HttpSettings(int Port);

/// <summary>
/// The database, and the limits on the service's use of it (N-22). The keys are pg_tileserv's
/// <c>DbPoolMaxConns</c>, <c>DbPoolMaxConnLifeTime</c> and <c>DbTimeout</c>, and Tessera's own
/// <c>DbPoolConnectionTimeout</c>; the address comes from <c>DATABASE_URL</c>, as in pg_tileserv.
/// </summary>
/// <param name="Address">The database and the sign-in, from <c>DATABASE_URL</c>.</param>
/// <param name="PoolMaxConns">The most connections the pool holds open at once.</param>
/// <param name="PoolMaxConnLifeTime">How long a connection lives before the pool closes and replaces it.</param>
/// <param name="Timeout">The longest a query may run before the database cancels it.</param>
/// <param name="PoolConnectionTimeout">The longest a request waits for a free connection.</param>
public sealed record DbSettings(DatabaseAddress Address, int PoolMaxConns, TimeSpan PoolMaxConnLifeTime, TimeSpan Timeout, TimeSpan PoolConnectionTimeout);

/// <summary>How often the service rereads the list of granted tables: Tessera's own <c>CatalogueRefresh</c> (N-23). Zero means never.</summary>
public sealed record CatalogueSettings(TimeSpan Refresh);

/// <summary>Everything the service needs to start, read from the environment and from tessera.json (ADR 0003).</summary>
public sealed record ServiceSettings(Area Area, MapSettings Map, TileSettings Tiles, HttpSettings Http, DbSettings Db, CatalogueSettings Catalogue);

/// <summary>Everything the import needs: the area's box, to check each place, and the publisher's database.</summary>
public sealed record ImportSettings(AreaBox Box, DatabaseAddress Database);

/// <summary>One thing wrong with the settings, named so the maintainer can fix it.</summary>
public sealed record SettingsError(string Name, string Problem)
{
    public override string ToString() => $"{Name}: {Problem}";
}
