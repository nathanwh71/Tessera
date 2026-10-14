using System.Collections.Immutable;
using System.Globalization;
using System.Text.Json;
using Tessera.Kernel;
using Tessera.Settings;

namespace Tessera.Map;

/// <summary>
/// The map's web page, with the area it shows written in (US-23 c1), or the reason there is no page
/// to serve. This type sits in the Map module; <see cref="MapRoutes"/> builds one at start-up and
/// serves it. Every function here is pure, one whose answer depends on its inputs alone: it reads
/// text and settings it is given, never a file, so a test can check it with no files at hand.
/// </summary>
/// <remarks>
/// The map learns its area from the page itself, in a block of JSON, a plain text format for data,
/// that the browser stores and never runs, so the map sends no extra request for it. ADR 0006 fixes
/// the block's shape as the contract between the map and any service that serves it: the Java
/// build, this service's twin written in Java, writes the same block.
/// </remarks>
public abstract record Page
{
    private Page() { }

    /// <summary>The page, as HTML text with the area written in, ready to serve.</summary>
    public sealed record Shown(string Html) : Page;

    /// <summary>
    /// The preview is switched off in tessera.json, the settings file, so the map's addresses answer
    /// 404 'not found' (US-23 c5).
    /// </summary>
    public sealed record Off : Page;

    /// <summary>The release carries no built map, so there is no page to serve.</summary>
    public sealed record Missing : Page;

    /// <summary>Calls the one function that fits this page's case and returns its answer.</summary>
    public TOut Match<TOut>(Func<string, TOut> shown, Func<TOut> off, Func<TOut> missing) => this switch
    {
        Shown(var html) => shown(html),
        Off => off(),
        Missing => missing(),
        _ => throw new System.Diagnostics.UnreachableException(),
    };

    /// <summary>
    /// The nine layers the map names in its own code (US-23 c7); <c>/{layer}.html</c> opens the map
    /// on one of them. This list must match the map's own.
    /// </summary>
    public static readonly ImmutableHashSet<string> LayerIds =
    [
        "food.cafes", "food.restaurants", "food.markets",
        "outdoors.parks", "outdoors.playgrounds",
        "culture.museums", "culture.libraries",
        "transport.trains", "transport.buses",
    ];

    /// <summary>
    /// Builds the page from the built map's text, if any, the area, and the preview switch. The
    /// switch wins: with the preview off, the page is <see cref="Off"/> even when the text exists.
    /// </summary>
    public static Page Of(Option<string> html, Area area, MapSettings settings) =>
        !settings.Preview ? new Off()
            : html.Match<Page>(text => new Shown(Render(text, area)), () => new Missing());

    /// <summary>
    /// Writes the area into the page as the <c>tessera-area</c> block, before
    /// <c>&lt;/head&gt;</c>, the tag that closes the page's head. A page with no such tag comes back
    /// unchanged. The block's shape is ADR 0006's:
    /// <c>{name, bounds: [west, south, east, north], home: {center: {lng, lat}, zoom}}</c>.
    /// </summary>
    public static string Render(string html, Area area)
    {
        var block = $"""<script id="tessera-area" type="application/json">{Json(area)}</script>""";
        var head = html.IndexOf("</head>", StringComparison.OrdinalIgnoreCase);
        return head < 0 ? html : html[..head] + block + html[head..];
    }

    /// <summary>
    /// The layer a page's name opens, such as <c>food.cafes</c> for <c>food.cafes.html</c>, or none
    /// when the name is not one of <see cref="LayerIds"/> followed by <c>.html</c>.
    /// </summary>
    public static Option<string> LayerOf(string file) =>
        file.EndsWith(".html", StringComparison.Ordinal) && LayerIds.Contains(file[..^".html".Length])
            ? Option.Some(file[..^".html".Length])
            : Option.None<string>();

    // The numbers must use a decimal point whatever the machine's language, since the map reads them
    // that way; JsonSerializer always writes them so.
    private static string Json(Area area) => JsonSerializer.Serialize(new
    {
        name = area.Name,
        bounds = new[] { area.Box.West, area.Box.South, area.Box.East, area.Box.North },
        home = new { center = new { lng = area.Home.Longitude, lat = area.Home.Latitude }, zoom = area.Home.Zoom },
    });
}
