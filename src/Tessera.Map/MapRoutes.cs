using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.FileProviders;
using Tessera.Settings;

namespace Tessera.Map;

/// <summary>
/// Serves the map's built files: the page at <c>/</c>, the same page opened on one layer at
/// <c>/{layer}.html</c>, and its scripts and styles (US-23).
/// </summary>
/// <remarks>
/// The page ships inside the service's own release (US-23 c4), in a folder beside the program.
/// With the preview switched off in tessera.json, none of these routes exists, so <c>/</c> answers
/// 404 (US-23 c5).
/// </remarks>
public static class MapRoutes
{
    public const string PageFile = "index.html";

    public static WebApplication UseMap(this WebApplication app, MapSettings settings, string folder)
    {
        if (!settings.Preview || !Directory.Exists(folder))
            return app;

        var files = new PhysicalFileProvider(folder);
        app.UseStaticFiles(new StaticFileOptions { FileProvider = files, ServeUnknownFileTypes = false });
        app.MapGet("/", () => Page(folder));
        // The page reads the layer's id from its own address; the service sends the same page.
        app.MapGet("/{layer}.html", (string layer) => Page(folder));
        return app;
    }

    private static IResult Page(string folder) =>
        Results.File(Path.Combine(folder, PageFile), "text/html; charset=utf-8");
}
