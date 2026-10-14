using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.FileProviders;
using Tessera.Kernel;
using Tessera.Settings;

namespace Tessera.Map;

/// <summary>
/// Serves the map that people view in a browser: the page at <c>/</c>, the same page opened on one
/// layer at <c>/{layer}.html</c>, and its scripts, styles and fonts under <c>/assets/</c> (US-23).
/// This class sits in the Map module; TesseraService calls <see cref="UseMapHeaders"/> and
/// <see cref="UseMap"/> once at start-up.
/// </summary>
/// <remarks>
/// The built map ships inside the service's own release (US-23 c4), in a folder beside the program.
/// Only the two page routes, the code that answers those addresses, serve the page, with the area
/// written in; the files under <c>assets</c> go out as they are. With the preview switched off,
/// every map address answers 404 'not found' (US-23 c5).
/// </remarks>
public static class MapRoutes
{
    /// <summary>The page's file in the map's folder.</summary>
    public const string PageFile = "index.html";

    /// <summary>
    /// The browser's rules for the page, sent as its Content Security Policy, so that a script
    /// slipped into the page from elsewhere cannot run. The page may load everything from its own
    /// site and nothing from any other, run no plug-ins, and appear inside no other site's page.
    /// MapLibre, the code that draws the map, makes its images and its worker, a script running
    /// beside the page, from <c>blob:</c> addresses held in the browser's memory, and React, the code
    /// that builds the page's controls, writes styles into the page itself, so the rules let those in.
    /// </summary>
    public const string Policy =
        "default-src 'self'; script-src 'self'; style-src 'self' 'unsafe-inline'; img-src 'self' data: blob:; " +
        "font-src 'self'; connect-src 'self'; worker-src 'self' blob:; child-src 'self' blob:; " +
        "object-src 'none'; base-uri 'none'; form-action 'none'; frame-ancestors 'none'";

    /// <summary>
    /// Reads the page once, at start-up, and adds the routes for the page and its files.
    /// </summary>
    /// <param name="app">The web application, to which the routes are added.</param>
    /// <param name="area">The area written into the page.</param>
    /// <param name="settings">Holds the preview switch.</param>
    /// <param name="folder">The folder that holds the built map.</param>
    public static WebApplication UseMap(this WebApplication app, AreaSettings area, MapSettings settings, string folder)
    {
        var page = Page.Of(ReadPage(folder), area, settings);
        var assets = Path.Combine(folder, "assets");
        if (settings.ShowPreview && Directory.Exists(assets))
        {
            // Each file's name carries a hash, a short fingerprint of its content, so a changed file
            // gets a new name; a browser may therefore keep each one for a year (31,536,000 seconds)
            // without asking again. Vite, the tool that builds the map, names MapLibre's worker .js,
            // so the usual list of file types suffices and a file of unknown type is not served.
            app.UseStaticFiles(new StaticFileOptions
            {
                FileProvider = new PhysicalFileProvider(assets),
                RequestPath = "/assets",
                ServeUnknownFileTypes = false,
                OnPrepareResponse = context => context.Context.Response.Headers.CacheControl = "public, max-age=31536000, immutable",
            });
        }

        // GET asks for the page; HEAD asks for its headers alone, the lines of extra
        // information sent ahead of the page. The Java build, this service's twin written in Java,
        // answers both (ADR 0006 rule 7).
        string[] read = [HttpMethods.Get, HttpMethods.Head];
        app.MapMethods("/", read, () => Answer(page));
        // Any other single name, such as /index.html, is not found: the page goes out through these
        // two routes alone. The 404 holds no detail, as in the Java build: the status says all there is.
        app.MapMethods("/{file}", read, (string file) => Page.LayerOf(file).Match(_ => Answer(page), () => Problems.NotFound()));
        return app;
    }

    /// <summary>
    /// The page, or a 404 that says why there is none. <see cref="UseMapHeaders"/> adds the page's
    /// headers on the way out.
    /// </summary>
    private static IResult Answer(Page page) => page.Match(
        html => Results.Text(html, "text/html; charset=utf-8"),
        () => Problems.NotFound("The map preview is switched off."),
        () => Problems.NotFound("This release carries no map."));

    /// <summary>
    /// Adds the page's headers to every HTML answer. <c>no-cache</c> makes the browser check for a
    /// newer page each time, since each release changes it. The other three carry the browser's
    /// rules (<see cref="Policy"/>), forbid the browser to guess a file's type from its content, and
    /// stop the browser from sending the page's address with the requests it makes.
    /// </summary>
    /// <remarks>
    /// This is a step of middleware, the chain every request passes through. It sets the headers
    /// at the last moment before the answer starts, once the answer's type is known.
    /// </remarks>
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

    // The page's text, or none when the release carries no built map.
    private static Option<string> ReadPage(string folder)
    {
        var path = Path.Combine(folder, PageFile);
        return File.Exists(path) ? Option.Some(File.ReadAllText(path)) : Option.None<string>();
    }
}
