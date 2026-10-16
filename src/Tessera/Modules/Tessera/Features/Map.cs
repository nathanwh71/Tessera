using System.Collections.Immutable;
using System.Text.Json;
using Common.Api;
using Microsoft.Extensions.FileProviders;
using Modules.Tessera.Domain;

namespace Modules.Tessera.Features;

/// <summary>
/// Serves the map people view in a browser: the page at <c>/</c>, the same page opened on one
/// <b><i>layer</i></b> at <c>/{layer}.html</c>, and its scripts and styles under <c>/assets/</c>
/// (US-23). The built map ships inside the release, in a folder beside the program (US-23 c4).
/// </summary>
public static class MapRoutes
{
    /// <summary>
    /// The browser's rules for the page, its Content Security Policy, so a script slipped in from
    /// elsewhere cannot run. MapLibre makes its images and worker from <c>blob:</c> addresses, and React
    /// writes styles into the page, so the rules let those in.
    /// </summary>
    public const string Policy =
        "default-src 'self'; script-src 'self'; style-src 'self' 'unsafe-inline'; img-src 'self' data: blob:; " +
        "font-src 'self'; connect-src 'self'; worker-src 'self' blob:; child-src 'self' blob:; " +
        "object-src 'none'; base-uri 'none'; form-action 'none'; frame-ancestors 'none'";

    /// <summary>
    /// The nine layers the map names in its own code (US-23 c7); this list must match the map's. The
    /// server keeps it so a page for any other name, such as <c>/x.html</c>, answers 404, not an
    /// empty map.
    /// </summary>
    public static readonly ImmutableHashSet<string> LayerIds =
    [
        "food.cafes", "food.restaurants", "food.markets",
        "outdoors.parks", "outdoors.playgrounds",
        "culture.museums", "culture.libraries",
        "transport.trains", "transport.buses",
    ];

    /// <summary>
    /// Reads the page once, at start-up, since neither the page nor the area changes while the program
    /// runs, and adds the routes for the page and its files. With the preview off, the files go
    /// unserved too, so no part of the map stays reachable (US-23 c5).
    /// </summary>
    public static WebApplication UseMap(this WebApplication app, AreaSettings area, MapSettings settings, string folder)
    {
        var file = Path.Combine(folder, "index.html");
        var page = File.Exists(file) ? Render(File.ReadAllText(file), area) : null;
        var assets = Path.Combine(folder, "assets");
        if (settings.ShowPreview && Directory.Exists(assets))
        {
            // Each file's name carries a hash of its content, so a changed file gets a new name, and a
            // browser may keep each one for a year without asking again.
            app.UseStaticFiles(new StaticFileOptions
            {
                FileProvider = new PhysicalFileProvider(assets),
                RequestPath = "/assets",
                OnPrepareResponse = context =>
                    context.Context.Response.Headers.CacheControl = "public, max-age=31536000, immutable",
            });
        }

        // GET fetches the page; HEAD asks for its headers alone, as a watcher or a checking cache may.
        string[] read = [HttpMethods.Get, HttpMethods.Head];
        app.MapMethods("/", read, () => Answer(page, settings));
        // Any other single name, such as /index.html, is not found, so each view of the map has one
        // address.
        app.MapMethods("/{file}", read, (string file) =>
            LayerOf(file) is null ? Problems.NotFound() : Answer(page, settings));
        return app;
    }

    /// <summary>The page, or a 404 that says why there is none (US-23 c5).</summary>
    internal static IResult Answer(string? page, MapSettings settings) =>
        !settings.ShowPreview ? Problems.NotFound("The map preview is switched off.")
        : page is null ? Problems.NotFound("This release carries no map.")
        : TypedResults.Text(page, "text/html; charset=utf-8");

    /// <summary>
    /// Writes the area into the page as the <c>tessera-area</c> block, before <c>&lt;/head&gt;</c>, so the
    /// map needs no extra request for it. The block's shape is ADR 0006's contract with the map:
    /// <c>{name, bounds: [west, south, east, north], home: {center: {lng, lat}, zoom}}</c>.
    /// </summary>
    public static string Render(string html, AreaSettings area)
    {
        var json = JsonSerializer.Serialize(new
        {
            name = area.Name,
            bounds = new[] { area.MinLongitude, area.MinLatitude, area.MaxLongitude, area.MaxLatitude },
            home = new { center = new { lng = area.HomeLongitude, lat = area.HomeLatitude }, zoom = area.HomeLevel },
        });
        var head = html.IndexOf("</head>", StringComparison.OrdinalIgnoreCase);
        var script = $"""<script id="tessera-area" type="application/json">{json}</script>""";
        // A page without </head> is not the built map's page, so it goes out untouched rather than
        // broken.
        return head < 0 ? html : html[..head] + script + html[head..];
    }

    /// <summary>
    /// The layer a page's name opens, such as <c>food.cafes</c> for <c>food.cafes.html</c>, or null
    /// when the name opens none of the nine. The match is ordinal, letter for letter, as layer ids are
    /// (US-03).
    /// </summary>
    public static string? LayerOf(string file) =>
        file.EndsWith(".html", StringComparison.Ordinal) && LayerIds.Contains(file[..^".html".Length])
            ? file[..^".html".Length]
            : null;

    /// <summary>
    /// Adds the page's headers to every HTML answer, at the last moment, once its type is known.
    /// <c>no-cache</c> makes a browser check for a newer page each time, so a new release shows at once,
    /// while the files the page names, each named by its hash, stay in the browser's <b><i>cache</i></b>.
    /// The Content Security Policy blocks scripts from elsewhere; <c>nosniff</c> stops a browser
    /// guessing a file's type and running text as a script; <c>no-referrer</c> keeps the page's address
    /// from the sites it links to.
    /// </summary>
    public static WebApplication UseMapHeaders(this WebApplication app)
    {
        app.Use((context, next) =>
        {
            context.Response.OnStarting(() =>
            {
                if (context.Response.ContentType?.StartsWith("text/html", StringComparison.OrdinalIgnoreCase) == true)
                {
                    context.Response.Headers.CacheControl = "no-cache";
                    context.Response.Headers.ContentSecurityPolicy = Policy;
                    context.Response.Headers.XContentTypeOptions = "nosniff";
                    context.Response.Headers["Referrer-Policy"] = "no-referrer";
                }

                return Task.CompletedTask;
            });
            return next(context);
        });
        return app;
    }
}
