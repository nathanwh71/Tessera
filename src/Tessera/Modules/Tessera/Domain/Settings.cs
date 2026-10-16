using System.ComponentModel.DataAnnotations;

// The settings that describe what Tessera serves: the area it covers and the tiles it builds
// (US-00, ADR 0003). They are plain records with their own limits, so they belong to the Domain.

namespace Modules.Tessera.Domain;

/// <summary>
/// One tile's limits and how long a browser and Nginx may keep it (US-07, N-18). A tile is
/// Resolution units wide; the buffer is a margin drawn beyond each edge, so a symbol near the edge
/// shows on both tiles. The section <c>Tiles</c> in tessera.json.
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

/// <summary>
/// The place the tiles cover, in degrees, and where the map opens. The office sets it to Macau.
/// Web Mercator, the web map's projection, stops near 85 degrees north and south. The section
/// <c>Area</c> in tessera.json.
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
