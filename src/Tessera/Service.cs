using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Tessera.Catalogue;
using Tessera.Map;
using Tessera.Settings;
using Tessera.Sql;
using Tessera.Tiles;

namespace Tessera;

/// <summary>
/// Puts the <b><i>modules</i></b> together into one web service. The service is a
/// <b><i>modular monolith</i></b>, one program built from modules with guarded borders, so one
/// release serves everything while each module stays small (ADR 0001). Program.cs builds it once,
/// then runs it.
/// </summary>
public static class TesseraService
{
    /// <summary>
    /// Builds the service on ASP.NET Core's standard start-up, which reads tessera.json, the
    /// <b><i>settings file</i></b>, its <c>Urls</c> and <c>Logging</c> sections included, and the
    /// environment (ADR 0003). Every setting is checked here, before anything runs, and a failed
    /// check throws with every problem, so one failed start names all there is to fix (N-21 c4).
    /// </summary>
    /// <param name="overrides">Settings a test lays over the file and the environment.</param>
    public static WebApplication Build(IDictionary<string, string?>? overrides = null)
    {
        var builder = WebApplication.CreateBuilder();
        TesseraSettings.AddFile(builder.Configuration);
        builder.Configuration.AddInMemoryCollection(overrides ?? new Dictionary<string, string?>());
        TesseraSettings.Register(builder.Services);
        DatabaseConnections.Register(builder.Services, builder.Configuration, TesseraSettings.ReaderDatabase);
        // Every error answers as a problem document of one shape, so no route writes its own (US-07 c5).
        builder.Services.AddProblemDetails(options => options.CustomizeProblemDetails = Problems.Shape);

        // One Database for the whole program, since it holds the one pool of connections that every
        // request shares within the limits of N-22.
        builder.Services.AddSingleton(services => Database.Open(
            services.GetRequiredService<IOptions<DatabaseConnection>>().Value.Text,
            services.GetRequiredService<IOptions<DatabaseSettings>>().Value));
        // One list of layers, which every request shares, loads in the background, so the service
        // answers from its first moment (N-23 c3). Before the first load it tries every five seconds,
        // as N-23 c3 sets: the same wait Retry-After asks of each browser.
        builder.Services.AddSingleton(services => new LiveCatalogue(
            cancel => LayerCatalogue.Load(services.GetRequiredService<Database>(), cancel),
            TimeSpan.FromSeconds(services.GetRequiredService<IOptions<CatalogueSettings>>().Value.RefreshSeconds),
            TimeSpan.FromSeconds(Problems.RetryAfterSeconds),
            services.GetRequiredService<ILoggerFactory>().CreateLogger("Tessera.Catalogue")));
        // The background loop is the same object the tiles read, so a refresh reaches every request.
        builder.Services.AddHostedService(services => services.GetRequiredService<LiveCatalogue>());
        // N-25: the database must answer a ping within a second, through the same pool as the tiles.
        builder.Services.AddHealthChecks().AddCheck<DatabaseAnswers>("database");

        var app = builder.Build();
        TesseraSettings.Check(app.Services);
        var catalogue = app.Services.GetRequiredService<LiveCatalogue>();
        // The first catches a crash and the second an empty error, such as a 404 for an address no
        // route knows; both answer with a problem document.
        app.UseExceptionHandler();
        app.UseStatusCodePages();
        // Before the routes, so its hook joins every answer, whichever route or step writes it.
        app.UseMapHeaders();
        // Routing picks each request's route here, so the steps above run for every request first.
        app.UseRouting();
        app.MapHealthChecks("/health", new HealthCheckOptions
        {
            ResponseWriter = (context, report) =>
            {
                // The usual health-check words. DOWN outranks OUT_OF_SERVICE, the state while the first
                // list of layers loads. The answer names no fault, so a stranger learns nothing more.
                var status = report.Status != HealthStatus.Healthy ? "DOWN"
                    : catalogue.Current is null ? "OUT_OF_SERVICE"
                    : "UP";
                context.Response.StatusCode =
                    status == "UP" ? StatusCodes.Status200OK : StatusCodes.Status503ServiceUnavailable;
                // No copy is kept, since a kept 'UP' would hide a failure from the watcher (N-25 c3).
                context.Response.Headers.CacheControl = "no-store";
                context.Response.ContentType = "application/json";
                return context.Response.WriteAsync($$"""{"status":"{{status}}"}""");
            },
        });
        // The tiles, then the map, whose files the release keeps in wwwroot beside the program
        // (Tessera.csproj, US-23 c4). Each route's pattern, not this order, decides which one answers.
        app.MapTiles(
            catalogue,
            app.Services.GetRequiredService<Database>(),
            app.Services.GetRequiredService<IOptions<TileSettings>>().Value);
        app.UseMap(
            app.Services.GetRequiredService<IOptions<AreaSettings>>().Value,
            app.Services.GetRequiredService<IOptions<MapSettings>>().Value,
            Path.Combine(AppContext.BaseDirectory, "wwwroot"));
        return app;
    }

    // Pings the database; the ping waits a second at most, so /health answers within one.
    private sealed class DatabaseAnswers(Database database) : IHealthCheck
    {
        public async Task<HealthCheckResult> CheckHealthAsync(
            HealthCheckContext context, CancellationToken cancellationToken = default) =>
            await database.Ping(cancellationToken).ConfigureAwait(false)
                ? HealthCheckResult.Healthy()
                : HealthCheckResult.Unhealthy();
    }
}
