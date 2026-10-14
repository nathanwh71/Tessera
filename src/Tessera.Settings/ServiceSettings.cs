using Tessera.Kernel;

namespace Tessera.Settings;

/// <summary>
/// The area's edges in degrees of longitude (west, east) and latitude (south, north). Like every type
/// in this file it is a record, a bundle of named values that cannot change once made.
/// </summary>
public sealed record AreaBox(double West, double South, double East, double North)
{
    /// <summary>
    /// True when a point in longitude and latitude lies inside the box, edges included. The import
    /// uses it to refuse a place outside the area.
    /// </summary>
    public bool Contains(double longitude, double latitude) =>
        longitude >= West && longitude <= East && latitude >= South && latitude <= North;
}

/// <summary>Where the map opens: a centre in degrees and a zoom level from 0, the whole world, to 22.</summary>
public sealed record HomeView(double Longitude, double Latitude, double Zoom);

/// <summary>The place the tiles cover. The stories call it 'the area'; the office sets it to Macau.</summary>
public sealed record Area(string Name, AreaBox Box, HomeView Home);

/// <summary>
/// Whether the service shows its own map at <c>/</c> (US-23 c5): the <c>ShowPreview</c> key of
/// pg_tileserv, an older tile server whose setting names Tessera copies.
/// </summary>
public sealed record MapSettings(bool Preview);

/// <summary>
/// One tile's limits and how long a copy of it lives (US-07, N-18, N-19). A tile is one square of the
/// map, sent to the browser as a single file. Each field reads a key with pg_tileserv's name:
/// <c>MaxFeaturesPerTile</c>, <c>DefaultBuffer</c>, <c>CacheTTL</c>, <c>DefaultResolution</c>,
/// <c>DefaultMinZoom</c> and <c>DefaultMaxZoom</c>, and Tessera's own <c>TileCacheMegabytes</c>.
/// </summary>
/// <remarks>
/// A tile is Resolution units wide, 4096 by default. The buffer is a margin drawn beyond each edge,
/// so a line that crosses from one tile to the next shows no gap; 256 units reaches 1/16 of a tile.
/// CacheSeconds is how long a browser may keep a tile, and how long the service keeps its own copy in
/// its cache, a store of answers kept ready for the next request; 0 means neither keeps one.
/// CacheMegabytes caps the memory the service's copies take together. MinZoom and MaxZoom bound the
/// zoom levels the service serves.
/// </remarks>
public sealed record TileSettings(int MaxFeatures, int Buffer, int CacheSeconds, int CacheMegabytes, int Resolution = 4096, int MinZoom = 0, int MaxZoom = 22);

/// <summary>
/// Where and how the service listens for web requests: pg_tileserv's <c>HttpHost</c> and
/// <c>HttpPort</c>, and the Java build's <c>HttpMaxConnections</c>, the most connections open at
/// once, and <c>HttpAcceptCount</c>, how many more may wait in line. The Java build is Tessera's twin,
/// written in Java, whose keys this file shares (ADR 0006 rule 5).
/// </summary>
public sealed record HttpSettings(System.Net.IPAddress Host, int Port, int MaxConnections = 8192, int AcceptCount = 100)
{
    /// <summary>Listens on every network card of the machine, on <paramref name="port"/>.</summary>
    public HttpSettings(int port) : this(System.Net.IPAddress.Any, port) { }
}

/// <summary>
/// The database, and the limits on the service's use of it (N-22). The service keeps a pool, a set of
/// open connections it reuses, since opening a fresh connection for each request is slow. The keys are
/// pg_tileserv's <c>DbPoolMaxConns</c>, <c>DbPoolMaxConnLifeTime</c> and <c>DbTimeout</c>, and
/// Tessera's own <c>DbPoolConnectionTimeout</c>; the address comes from <c>DATABASE_URL</c>, as in
/// pg_tileserv.
/// </summary>
/// <param name="Address">The database and the sign-in, from <c>DATABASE_URL</c>.</param>
/// <param name="PoolMaxConns">The most connections the pool holds open at once.</param>
/// <param name="PoolMaxConnLifeTime">
/// How long a connection lives before the pool closes and replaces it.
/// </param>
/// <param name="Timeout">
/// The longest a query may run before the database cancels it; zero means no limit.
/// </param>
/// <param name="PoolConnectionTimeout">The longest a request waits for a free connection.</param>
public sealed record DbSettings(DatabaseAddress Address, int PoolMaxConns, TimeSpan PoolMaxConnLifeTime, TimeSpan Timeout, TimeSpan PoolConnectionTimeout);

/// <summary>
/// How often the service rereads the list of tables the database lets it read, so a table granted
/// later appears without a restart: Tessera's own <c>CatalogueRefresh</c> (N-23). Zero means it reads
/// the list once, at start-up, and never again.
/// </summary>
public sealed record CatalogueSettings(TimeSpan Refresh);

/// <summary>
/// Everything the service needs to start, read from the environment and from tessera.json (ADR 0003).
/// This file holds the Settings module's answer, the values every other module is built from;
/// <see cref="SettingsReader"/> fills it in, and the program's start-up hands it to each module.
/// </summary>
public sealed record ServiceSettings(Area Area, MapSettings Map, TileSettings Tiles, HttpSettings Http, DbSettings Db, CatalogueSettings Catalogue);

/// <summary>
/// Everything the import needs: the area's box, to check each place, and the publisher's database.
/// </summary>
public sealed record ImportSettings(AreaBox Box, DatabaseAddress Database);

/// <summary>One thing wrong with the settings, named so the maintainer can fix it.</summary>
public sealed record SettingsError(string Name, string Problem)
{
    public override string ToString() => $"{Name}: {Problem}";
}
