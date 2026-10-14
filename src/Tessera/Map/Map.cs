using System.Collections.Immutable;
using System.Text.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.FileProviders;
using Tessera.Settings;

namespace Tessera.Map;

/// <summary>
/// Serves the map people view in a browser: the page at <c>/</c>, the same page opened on one layer at
/// <c>/{layer}.html</c>, and its scripts and styles under <c>/assets/</c> (US-23). The built map ships
/// inside the release, in a folder beside the program (US-23 c4).
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

    /// <summary>The nine layers the map names in its own code (US-23 c7); this list must match the map's.</summary>
    public static readonly ImmutableHashSet<string> LayerIds =
    [
        "food.cafes", "food.restaurants", "food.markets",
        "outdoors.parks", "outdoors.playgrounds",
        "culture.museums", "culture.libraries",
        "transport.trains", "transport.buses",
    ];

    /// <summary>Reads the page once, at start-up, and adds the routes for the page and its files.</summary>
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

        string[] read = [HttpMethods.Get, HttpMethods.Head];
        app.MapMethods("/", read, () => Answer(page, settings));
        // Any other single name, such as /index.html, is not found.
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
        return head < 0 ? html : html[..head] + script + html[head..];
    }

    /// <summary>The layer a page's name opens, such as <c>food.cafes</c> for <c>food.cafes.html</c>, or null.</summary>
    public static string? LayerOf(string file) =>
        file.EndsWith(".html", StringComparison.Ordinal) && LayerIds.Contains(file[..^".html".Length])
            ? file[..^".html".Length]
            : null;

    /// <summary>
    /// Adds the page's headers to every HTML answer, at the last moment, once its type is known:
    /// check for a newer page each time, the browser's rules, no guessing a file's type, and no
    /// sending the page's address onwards.
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
