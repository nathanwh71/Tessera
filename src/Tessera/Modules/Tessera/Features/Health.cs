using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Modules.Tessera.Features;

/// <summary>
/// <c>/health</c>, the address a watcher, such as the start-up script, asks whether the service is
/// well (N-25). It answers <c>{"status":"UP"}</c> with 200, or <c>OUT_OF_SERVICE</c> or <c>DOWN</c>
/// with 503. It answers GET and HEAD alone; any other method gets 405.
/// </summary>
public static class HealthRoutes
{
    /// <summary>
    /// Adds <c>/health</c>, which runs every <b><i>health check</i></b>. The worst result decides:
    /// Unhealthy, a database that does not answer, outranks Degraded, a list of layers not yet loaded.
    /// </summary>
    public static WebApplication MapHealth(this WebApplication app)
    {
        app.MapHealthChecks("/health", new HealthCheckOptions
        {
            ResultStatusCodes =
            {
                [HealthStatus.Healthy] = StatusCodes.Status200OK,
                [HealthStatus.Degraded] = StatusCodes.Status503ServiceUnavailable,
                [HealthStatus.Unhealthy] = StatusCodes.Status503ServiceUnavailable,
            },
            ResponseWriter = (context, report) =>
            {
                // The status words of Spring Boot Actuator, a common health-check library for Java,
                // which many watchers read. The answer names no fault, so a stranger learns nothing more.
                var status = report.Status switch
                {
                    HealthStatus.Healthy => "UP",
                    HealthStatus.Degraded => "OUT_OF_SERVICE",
                    _ => "DOWN",
                };
                // No copy is kept, since a kept 'UP' would hide a failure from the watcher (N-25 c3).
                context.Response.Headers.CacheControl = "no-store";
                context.Response.ContentType = "application/json";
                return context.Response.WriteAsync($$"""{"status":"{{status}}"}""");
            },
        })
        // /health is read, never written, as a tile is: GET and HEAD alone. Any other method finds
        // no route for itself, and routing answers 405 with 'Allow: GET, HEAD', as for a tile.
        .WithMetadata(new HttpMethodMetadata([HttpMethods.Get, HttpMethods.Head]));
        return app;
    }
}
