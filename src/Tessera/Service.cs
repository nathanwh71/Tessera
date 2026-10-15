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

/// <summary>Puts the modules together into one web service. Program.cs builds it once, then runs it.</summary>
public static class TesseraService
{
    /// <summary>
    /// Builds the service on ASP.NET Core's standard start-up, which reads tessera.json, its
    /// <c>Urls</c> and <c>Logging</c> sections included, and the environment (ADR 0003). Every setting
    /// is checked here, before anything runs, and a failed check throws with every problem.
    /// </summary>
    /// <param name="overrides">Settings a test lays over the file and the environment.</param>
    public static WebApplication Build(IDictionary<string, string?>? overrides = null)
    {
        var builder = WebApplication.CreateBuilder();
        TesseraSettings.AddFile(builder.Configuration);
        builder.Configuration.AddInMemoryCollection(overrides ?? new Dictionary<string, string?>());
        TesseraSettings.Register(builder.Services, builder.Configuration, TesseraSettings.ReaderDatabase);
        builder.Services.AddProblemDetails(options => options.CustomizeProblemDetails = Problems.Shape);

        builder.Services.AddSingleton(services => Database.Open(
            services.GetRequiredService<IOptions<DatabaseConnection>>().Value.Text,
            services.GetRequiredService<IOptions<DatabaseSettings>>().Value));
        // One list of layers, which every request shares, loads in the background, so the service
        // answers from its first moment (N-23 c4).
        builder.Services.AddSingleton(services => new LiveCatalogue(
            cancel => LayerCatalogue.Load(services.GetRequiredService<Database>(), cancel),
            TimeSpan.FromSeconds(services.GetRequiredService<IOptions<CatalogueSettings>>().Value.RefreshSeconds),
            TimeSpan.FromSeconds(Problems.RetryAfterSeconds),
            services.GetRequiredService<ILoggerFactory>().CreateLogger("Tessera.Catalogue")));
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
        app.UseMapHeaders();
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
                context.Response.Headers.CacheControl = "no-store";
                context.Response.ContentType = "application/json";
                return context.Response.WriteAsync($$"""{"status":"{{status}}"}""");
            },
        });
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
