using System.Net;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using Tessera.Catalogue;
using Tessera.Map;
using Tessera.Settings;
using Tessera.Sql;
using Tessera.Tiles;

namespace Tessera;

/// <summary>Puts the modules together into one web service. Program.cs builds it once, then runs it.</summary>
public static class TesseraService
{
    /// <summary>
    /// Builds the service from checked settings. It starts on an empty host, so it reads no
    /// appsettings.json: tessera.json and the environment hold every setting (ADR 0003).
    /// </summary>
    public static WebApplication Build(ServiceSettings settings, Database database, string mapFolder)
    {
        var builder = WebApplication.CreateEmptyBuilder(new WebApplicationOptions());
        // Kestrel is the web server built into .NET.
        builder.WebHost.UseKestrelCore().ConfigureKestrel(kestrel => kestrel.Listen(IPAddress.Parse(settings.Http.Host), settings.Http.Port));
        builder.Services.AddRoutingCore();
        builder.Services.AddProblemDetails(options => options.CustomizeProblemDetails = Problems.Shape);
        // With no logging settings .NET writes four lines for every request, about 140 MB a minute under
        // load. Its own code keeps warnings and its start-up lines; Tessera's lines stay at Information.
        builder.Logging.AddConsole();
        builder.Logging.AddFilter("Microsoft", LogLevel.Warning);
        builder.Logging.AddFilter("Microsoft.Hosting.Lifetime", LogLevel.Information);

        // One list of layers, which every request shares, loads in the background, so the service
        // answers from its first moment (N-23 c4).
        builder.Services.AddSingleton(services => new LiveCatalogue(
            cancel => LayerCatalogue.Load(database, cancel),
            TimeSpan.FromSeconds(settings.Catalogue.RefreshSeconds),
            TimeSpan.FromSeconds(Problems.RetryAfterSeconds),
            services.GetRequiredService<ILoggerFactory>().CreateLogger("Tessera.Catalogue")));
        builder.Services.AddHostedService(services => services.GetRequiredService<LiveCatalogue>());

        // N-25: the database must answer a ping, on its own pool, within a second, and the first list
        // of layers must have loaded.
        builder.Services.AddHealthChecks()
            .AddAsyncCheck("database", async cancel => await database.Ping(cancel).ConfigureAwait(false)
                ? HealthCheckResult.Healthy()
                : HealthCheckResult.Unhealthy());

        var app = builder.Build();
        var catalogue = app.Services.GetRequiredService<LiveCatalogue>();
        // The first catches a crash and the second an empty error, such as a 404 for an address no
        // route knows; both answer with a problem document.
        app.UseExceptionHandler();
        app.UseStatusCodePages();
        app.UseMapHeaders();
        app.UseRouting();
        app.MapHealthChecks("/health", new HealthCheckOptions
        {
            ResponseWriter = (context, report) =>
            {
                // The words the Java build uses. DOWN outranks OUT_OF_SERVICE, the state while the first
                // list of layers loads. The answer names no fault, so a stranger learns nothing more.
                var status = report.Entries["database"].Status != HealthStatus.Healthy ? "DOWN"
                    : catalogue.Current is null ? "OUT_OF_SERVICE"
                    : "UP";
                context.Response.StatusCode = status == "UP" ? StatusCodes.Status200OK : StatusCodes.Status503ServiceUnavailable;
                context.Response.Headers.CacheControl = "no-store";
                context.Response.ContentType = "application/json";
                return context.Response.WriteAsync($$"""{"status":"{{status}}"}""");
            },
        });
        app.MapTiles(catalogue, database, settings.Tiles);
        app.UseMap(settings.Area, settings.Map, mapFolder);
        return app;
    }
}
