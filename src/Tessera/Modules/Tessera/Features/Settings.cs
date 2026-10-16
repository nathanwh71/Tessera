namespace Modules.Tessera.Features;

/// <summary>
/// Whether the service shows its own map at <c>/</c> (US-23 c5). The section <c>Map</c> in
/// tessera.json.
/// </summary>
public sealed record MapSettings
{
    public bool ShowPreview { get; init; } = true;
}
