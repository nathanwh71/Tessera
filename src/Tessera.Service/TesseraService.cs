using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Net;
using Tessera.Catalogue;
using Tessera.Http;
using Tessera.Map;
using Tessera.Settings;
using Tessera.Sql;
using Tessera.Tiles;

namespace Tessera.Service;

/// <summary>
/// Puts the modules together into one web service. This project is the only one that references
/// every module; the modules never reference it (ADR 0001).
/// </summary>
public static class TesseraService
{
    /// <summary>
    /// Builds the service from settings that have passed their checks and a catalogue already
    /// loaded. The order of the middleware matters: each request meets them top to bottom.
    /// </summary>
    /// <remarks>
    /// Middleware is a chain of steps every request passes through on its way in. The error handler
    /// comes first so it can catch a failure in any later step; the target screen comes before any
    /// route, so a disguised address never reaches one (S-06).
    /// The service builds on an empty host, so it reads no appsettings.json: tessera.json and the
    /// environment hold every setting (ADR 0003).
    /// </remarks>
    public static WebApplication Build(ServiceSettings settings, Database database, string mapFolder)
    {
        var cache = new TileCache(settings.Tiles, TimeProvider.System);
        var builder = WebApplication.CreateEmptyBuilder(new WebApplicationOptions());
        // HttpMaxConnections caps the connections open at once; HttpAcceptCount is how many more may
        // wait in the operating system's line before it refuses a new one.
        builder.WebHost.UseKestrelCore()
            .UseSockets(sockets => sockets.Backlog = settings.Http.AcceptCount)
            .ConfigureKestrel(kestrel =>
            {
                kestrel.Limits.MaxConcurrentConnections = settings.Http.MaxConnections;
                kestrel.Listen(settings.Http.Host, settings.Http.Port);
            });
        builder.Services.AddRoutingCore();
        // The map's own files travel gzip-packed when the browser accepts it (N-20); a tile packs
        // itself, once, in Tiles. The usual text types are on the list already; .NET names a script
        // text/javascript, and a problem document has a type of its own.
        builder.Services.AddResponseCompression(compression =>
        {
            compression.Providers.Add<GzipCompressionProvider>();
            compression.MimeTypes = [.. ResponseCompressionDefaults.MimeTypes, "text/javascript", "application/problem+json"];
        });
        builder.Logging.AddConsole();
        // The list of layers loads in the background, so the service answers from its first moment,
        // with 503 until the database answers (N-23 c4). A layer that leaves the list takes its kept
        // tiles with it (N-23 c3).
        builder.Services.AddSingleton(services =>
        {
            var log = services.GetRequiredService<ILoggerFactory>().CreateLogger("Tessera.Catalogue");
            return LiveCatalogue.From(
                database,
                (old, fresh) =>
                {
                    var departed = LiveCatalogue.Departed(old, fresh);
                    cache.DropLayers(departed);
                    if (old.Layers.Count != fresh.Layers.Count || departed.Length > 0)
                        log.LogInformation("The service publishes {Count} layers.", fresh.Layers.Count);
                },
                error => log.LogWarning("Could not read the list of layers ({SqlState}): {Message}", error.SqlState, error.Message));
        });
        builder.Services.AddHostedService(services => new CatalogueRefresh(services.GetRequiredService<LiveCatalogue>(), settings.Catalogue.Refresh));
        builder.Services.AddTesseraHealth(database);

        var app = builder.Build();
        app.UseProblemDocuments();
        app.UseTargetScreen();
        app.UseResponseCompression();
        app.UseMapHeaders();
        app.UseRouting();
        app.MapTesseraHealth();
        app.MapTiles(app.Services.GetRequiredService<LiveCatalogue>(), database, settings.Tiles, cache);
        app.UseMap(settings.Area, settings.Map, mapFolder);
        return app;
    }
}

/// <summary>
/// Runs the live catalogue's refresh loop for as long as the service runs (N-23). Until the first
/// load succeeds it tries every five seconds, the same wait a 503 asks of a browser.
/// </summary>
internal sealed class CatalogueRefresh(LiveCatalogue catalogue, TimeSpan every) : BackgroundService
{
    protected override Task ExecuteAsync(CancellationToken stoppingToken) =>
        catalogue.Run(every, TimeSpan.FromSeconds(Problems.RetryAfterSeconds), TimeProvider.System, stoppingToken);
}
