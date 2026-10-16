using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Modules.Tessera.Infrastructure;

namespace Modules.Tessera.Features;

/// <summary>
/// <c>/health</c>, the address a watcher, such as the start-up script, asks whether the service is
/// well (N-25). It answers <c>{"status":"UP"}</c> with 200, or <c>OUT_OF_SERVICE</c> or <c>DOWN</c>
/// with 503.
/// </summary>
public static class HealthRoutes
{
    /// <summary>Adds <c>/health</c>, which runs every health check and reads the list of layers.</summary>
    public static WebApplication MapHealth(this WebApplication app, LiveCatalogue catalogue)
    {
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
        return app;
    }
}
