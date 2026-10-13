using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.FileProviders;
using Tessera.Http;
using Tessera.Kernel;
using Tessera.Settings;

namespace Tessera.Map;

/// <summary>
/// Serves the map: the page at <c>/</c>, the same page opened on one layer at <c>/{layer}.html</c>,
/// and its scripts, styles and fonts under <c>/assets/</c> (US-23).
/// </summary>
/// <remarks>
/// The built map ships inside the service's own release (US-23 c4), in a folder beside the program.
/// Only these routes serve the page, with the area written in; the folder's other files go out as
/// they are. With the preview switched off, every map address answers 404 (US-23 c5).
/// </remarks>
public static class MapRoutes
{
    public const string PageFile = "index.html";

    /// <summary>
    /// The browser's rules for the page: everything from the page's own origin, nothing from any
    /// other site, no plug-ins, and no framing by another site. MapLibre draws its images and runs
    /// its worker from <c>blob:</c> addresses, and React writes inline styles, so those are let in.
    /// </summary>
    public const string Policy =
        "default-src 'self'; script-src 'self'; style-src 'self' 'unsafe-inline'; img-src 'self' data: blob:; " +
        "font-src 'self'; connect-src 'self'; worker-src 'self' blob:; child-src 'self' blob:; " +
        "object-src 'none'; base-uri 'none'; form-action 'none'; frame-ancestors 'none'";

    public static WebApplication UseMap(this WebApplication app, Area area, MapSettings settings, string folder)
    {
        var page = Page.Of(ReadPage(folder), area, settings);
        var assets = Path.Combine(folder, "assets");
        if (settings.Preview && Directory.Exists(assets))
        {
            // Vite names MapLibre's worker .js, so the usual types serve; every file goes out as it is,
            // with a year's cache, since its name carries a hash that changes with its content.
            app.UseStaticFiles(new StaticFileOptions
            {
                FileProvider = new PhysicalFileProvider(assets),
                RequestPath = "/assets",
                ServeUnknownFileTypes = false,
                OnPrepareResponse = context => context.Context.Response.Headers.CacheControl = "public, max-age=31536000, immutable",
            });
        }

        app.MapGet("/", () => Answer(page));
        // Any other single name, such as /index.html, is not found: the page goes out through these two routes alone.
        app.MapGet("/{file}", (string file) => Page.LayerOf(file).Match(_ => Answer(page), () => Problems.NotFound("No page has that name.")));
        return app;
    }

    /// <summary>
    /// The page asks the browser to check for a newer copy each time, since each release changes it,
    /// and carries the browser's rules for it.
    /// </summary>
    private static IResult Answer(Page page) => page.Match(
        html => Results.Text(html, "text/html; charset=utf-8"),
        () => Problems.NotFound("The map preview is switched off."),
        () => Problems.NotFound("This release carries no map."));

    /// <summary>Adds the page's headers to every answer the map's own routes give.</summary>
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

    private static Option<string> ReadPage(string folder)
    {
        var path = Path.Combine(folder, PageFile);
        return File.Exists(path) ? Option.Some(File.ReadAllText(path)) : Option.None<string>();
    }
}
