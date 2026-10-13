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

/// <summary>Whether the service shows its own map at <c>/</c> (US-23 c5).</summary>
public sealed record MapSettings(bool Preview);

/// <summary>
/// The most features one tile may carry, and how far past its edge it reaches, in tile units
/// (US-07). A tile is 4096 units wide, so a buffer of 64 reaches 1/64 of a tile beyond each edge.
/// </summary>
public sealed record TileSettings(int MaxFeatures, int Buffer);

/// <summary>Everything the service needs to start, read from the environment and from tessera.json (ADR 0003).</summary>
public sealed record ServiceSettings(Area Area, MapSettings Map, TileSettings Tiles, int Port, DatabaseAddress Database);

/// <summary>Everything the import needs: the area's box, to check each place, and the publisher's database.</summary>
public sealed record ImportSettings(AreaBox Box, DatabaseAddress Database);

/// <summary>One thing wrong with the settings, named so the maintainer can fix it.</summary>
public sealed record SettingsError(string Name, string Problem)
{
    public override string ToString() => $"{Name}: {Problem}";
}
