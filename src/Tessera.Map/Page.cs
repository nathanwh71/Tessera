using System.Collections.Immutable;
using System.Globalization;
using System.Text.Json;
using Tessera.Kernel;
using Tessera.Settings;

namespace Tessera.Map;

/// <summary>
/// The map's page, with the area written in (US-23 c1), or the reason there is none to serve.
/// Every function here is pure: it reads text and settings, never a file.
/// </summary>
/// <remarks>
/// The map learns its area from the page itself, in a block of JSON the browser never runs, so it
/// sends no request for it. ADR 0006 fixes the block's shape as the contract between the map and
/// any service that serves it: the Java build writes the same block.
/// </remarks>
public abstract record Page
{
    private Page() { }

    /// <summary>The page, ready to serve.</summary>
    public sealed record Shown(string Html) : Page;

    /// <summary>The preview is switched off in tessera.json, so the map's addresses answer 404 (US-23 c5).</summary>
    public sealed record Off : Page;

    /// <summary>The release carries no built map, so there is no page to serve.</summary>
    public sealed record Missing : Page;

    public TOut Match<TOut>(Func<string, TOut> shown, Func<TOut> off, Func<TOut> missing) => this switch
    {
        Shown(var html) => shown(html),
        Off => off(),
        Missing => missing(),
        _ => throw new System.Diagnostics.UnreachableException(),
    };

    /// <summary>The nine layers the map names in its own code (US-23 c7); <c>/{layer}.html</c> opens on one of them.</summary>
    public static readonly ImmutableHashSet<string> LayerIds =
    [
        "food.cafes", "food.restaurants", "food.markets",
        "outdoors.parks", "outdoors.playgrounds",
        "culture.museums", "culture.libraries",
        "transport.trains", "transport.buses",
    ];

    /// <summary>Builds the page from the built map's text, if any, the area, and the preview switch.</summary>
    public static Page Of(Option<string> html, Area area, MapSettings settings) =>
        !settings.Preview ? new Off()
            : html.Match<Page>(text => new Shown(Render(text, area)), () => new Missing());

    /// <summary>
    /// Writes the area into the page as the <c>tessera-area</c> block, before <c>&lt;/head&gt;</c>.
    /// The block's shape is ADR 0006's: <c>{name, bounds: [west, south, east, north], home: {center: {lng, lat}, zoom}}</c>.
    /// </summary>
    public static string Render(string html, Area area)
    {
        var block = $"""<script id="tessera-area" type="application/json">{Json(area)}</script>""";
        var head = html.IndexOf("</head>", StringComparison.OrdinalIgnoreCase);
        return head < 0 ? html : html[..head] + block + html[head..];
    }

    /// <summary>The layer a page's name opens, such as <c>food.cafes</c> for <c>food.cafes.html</c>, or none.</summary>
    public static Option<string> LayerOf(string file) =>
        file.EndsWith(".html", StringComparison.Ordinal) && LayerIds.Contains(file[..^".html".Length])
            ? Option.Some(file[..^".html".Length])
            : Option.None<string>();

    // JsonSerializer writes the numbers in the invariant culture, with a dot, whatever the machine's language.
    private static string Json(Area area) => JsonSerializer.Serialize(new
    {
        name = area.Name,
        bounds = new[] { area.Box.West, area.Box.South, area.Box.East, area.Box.North },
        home = new { center = new { lng = area.Home.Longitude, lat = area.Home.Latitude }, zoom = area.Home.Zoom },
    });
}
