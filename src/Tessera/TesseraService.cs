using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
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
    /// in any later step. Nginx, in front, keeps tiles, packs answers with gzip and tidies malformed
    /// addresses (ADR 0008), so none of that happens here.
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
        builder.Logging.AddConsole();
        // The log is the running record of events the service writes. The empty builder reads no
        // logging settings, so .NET would write four lines for every request: about 140 MB a minute
        // under load, enough to fill a disk in a day. From .NET's own code the log keeps warnings and
        // worse, with its start-up lines, as the Java build, this service's twin written in Java,
        // does; Tessera's own lines stay at Information, the everyday level.
        builder.Logging.AddFilter("Microsoft", LogLevel.Warning);
        builder.Logging.AddFilter("Microsoft.Hosting.Lifetime", LogLevel.Information);
        // The list of layers loads in the background, so the service answers from its first moment,
        // with 503, the status code for 'try again later', until the database answers (N-23 c4). The
        // service keeps one live catalogue, which every request shares.
        builder.Services.AddSingleton(services =>
        {
            var log = services.GetRequiredService<ILoggerFactory>().CreateLogger("Tessera.Catalogue");
            return LiveCatalogue.From(
                database,
                (old, fresh) =>
                {
                    if (!old.Layers.Keys.ToHashSet().SetEquals(fresh.Layers.Keys))
                        log.LogInformation("The service publishes {Count} layers.", fresh.Layers.Count);
                },
                error => log.LogWarning("Could not read the list of layers ({SqlState}): {Message}", error.SqlState, error.Message));
        });
        builder.Services.AddHostedService(services => new CatalogueRefresh(services.GetRequiredService<LiveCatalogue>(), settings.Catalogue.Refresh));
        builder.Services.AddTesseraHealth(database);

        var app = builder.Build();
        app.UseProblemDocuments();
        app.UseMapHeaders();
        app.UseRouting();
        app.MapTesseraHealth();
        app.MapTiles(app.Services.GetRequiredService<LiveCatalogue>(), database, settings.Tiles);
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
