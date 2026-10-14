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
/// Puts the modules together into one web service, a program that answers requests from browsers.
/// This class sits in the Service project, the only one that references every module; the modules
/// never reference it (ADR 0001). Program.cs calls <see cref="Build"/> once, then runs the result.
/// </summary>
public static class TesseraService
{
    /// <summary>
    /// Builds the service from settings that have passed their checks. The list of layers loads
    /// later, in the background, once the service runs.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The order of the middleware matters. Middleware is a chain of steps every request passes
    /// through on its way in, top to bottom. The error handler comes first so it can catch a failure
    /// in any later step; the target screen comes before any route, the code that answers one kind
    /// of address, so a disguised address never reaches one (S-06).
    /// </para>
    /// <para>
    /// The service builds on an empty host, the frame .NET (the toolkit the service runs on) wraps
    /// around a web service, so it reads no appsettings.json, .NET's usual settings file: tessera.json
    /// and the environment variables, settings handed to the program by the machine, hold every
    /// setting (ADR 0003).
    /// </para>
    /// </remarks>
    public static WebApplication Build(ServiceSettings settings, Database database, string mapFolder)
    {
        var cache = new TileCache(settings.Tiles, TimeProvider.System);
        var builder = WebApplication.CreateEmptyBuilder(new WebApplicationOptions());
        // Kestrel is the web server built into .NET, the part that holds the network connections.
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
        // The map's own files travel packed smaller with gzip when the browser can unpack it (N-20);
        // a tile packs itself, once, in Tiles. The list names the types of file worth packing. The
        // usual text types are on it already; the two added are the type .NET gives a script and the
        // type of a problem document, the service's error report.
        builder.Services.AddResponseCompression(compression =>
        {
            compression.Providers.Add<GzipCompressionProvider>();
            compression.MimeTypes = [.. ResponseCompressionDefaults.MimeTypes, "text/javascript", "application/problem+json"];
        });
        builder.Logging.AddConsole();
        // The log is the running record of events the service writes. The empty builder reads no
        // logging settings, so .NET would write four lines for every request: about 140 MB a minute
        // under load, enough to fill a disk in a day. From .NET's own code the log keeps warnings and
        // worse, with its start-up lines, as the Java build, this service's twin written in Java,
        // does; Tessera's own lines stay at Information, the everyday level.
        builder.Logging.AddFilter("Microsoft", LogLevel.Warning);
        builder.Logging.AddFilter("Microsoft.Hosting.Lifetime", LogLevel.Information);
        // The list of layers loads in the background, so the service answers from its first moment,
        // with 503, the status code for 'try again later', until the database answers (N-23 c4). A
        // layer that leaves the list takes its cached tiles, the copies kept in memory, with it
        // (N-23 c3). The
        // service keeps one live catalogue, which every request shares.
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
        // A hosted service is a loop .NET starts with the service and stops with it. The database
        // watch pings, sends the database a tiny query, each second from start-up, so while the
        // database is down, or hangs, tile requests get 503 at once and /health reports DOWN (N-28).
        builder.Services.AddHostedService(services => new DatabaseWatching(database, services.GetRequiredService<ILoggerFactory>().CreateLogger("Tessera.Database")));
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

/// <summary>Runs the database watch's loop (N-28) and logs each change of its verdict.</summary>
internal sealed class DatabaseWatching(Database database, ILogger log) : BackgroundService
{
    private static readonly TimeSpan Every = TimeSpan.FromSeconds(1);

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        database.Watch.Changed += up =>
        {
            if (up)
                log.LogInformation("The database answers again.");
            else
                log.LogWarning("The database is down; tile requests answer 503 until it answers again.");
        };
        return database.Watch.Run(Every, TimeProvider.System, stoppingToken);
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
