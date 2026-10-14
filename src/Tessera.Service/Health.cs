using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Tessera.Catalogue;
using Tessera.Sql;

namespace Tessera.Service;

/// <summary>
/// <c>/health</c>: 200 <c>{"status":"UP"}</c> when the service can serve tiles; 503
/// <c>{"status":"OUT_OF_SERVICE"}</c> while the first list of layers loads; 503
/// <c>{"status":"DOWN"}</c> when the database does not answer (N-25). pg_tileserv answers at the
/// same address, and the Java build gives the same three answers there (ADR 0006 rule 6).
/// </summary>
/// <remarks>
/// Two checks must both pass: the list of layers has loaded at least once (N-23), and the database
/// watch says it is up. The watch pings each second on a pool of its own, so it never waits behind
/// tiles, and /health answers at once whatever the database's state (N-26 c3, N-28). The answer names no check and no fault, so a stranger learns
/// nothing of the service's insides; the log holds the detail. It lives here, in the program,
/// because it reads both the catalogue and the database, and no module may reference both.
/// </remarks>
public static class Health
{
    public const string Path = "/health";

    public static IServiceCollection AddTesseraHealth(this IServiceCollection services, Database database) =>
        services.AddHealthChecks()
            .AddCheck<CatalogueLoaded>("catalogue")
            .AddCheck("database", new DatabaseAnswers(database))
            .Services;

    public static IEndpointRouteBuilder MapTesseraHealth(this IEndpointRouteBuilder routes)
    {
        routes.MapHealthChecks(Path, new HealthCheckOptions
        {
            ResultStatusCodes =
            {
                [HealthStatus.Healthy] = StatusCodes.Status200OK,
                [HealthStatus.Degraded] = StatusCodes.Status503ServiceUnavailable,
                [HealthStatus.Unhealthy] = StatusCodes.Status503ServiceUnavailable,
            },
            ResponseWriter = (context, report) =>
            {
                // A watcher asks again each time, so no copy of an old answer may stand in for a new one.
                context.Response.Headers.CacheControl = "no-store";
                context.Response.ContentType = "application/json";
                return context.Response.WriteAsync($$"""{"status":"{{Status(report)}}"}""");
            },
        });
        return routes;
    }

    /// <summary>
    /// The word Spring's health check, in the Java build, gives for the same state: DOWN outranks
    /// OUT_OF_SERVICE, which outranks UP, so a database that does not answer is DOWN whatever the list.
    /// </summary>
    public static string Status(HealthReport report) =>
        report.Entries.TryGetValue("database", out var database) && database.Status != HealthStatus.Healthy ? "DOWN"
            : report.Status != HealthStatus.Healthy ? "OUT_OF_SERVICE"
            : "UP";

    // .NET builds this check itself and hands it the live catalogue, a singleton of the service.
    private sealed class CatalogueLoaded(LiveCatalogue catalogue) : IHealthCheck
    {
        public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default) =>
            Task.FromResult(catalogue.Current.Match(
                _ => HealthCheckResult.Healthy(),
                () => HealthCheckResult.Unhealthy("The list of layers is not ready yet.")));
    }

    // The verdict of the database watch, read at no cost, so /health answers at once and never
    // waits on a database that hangs (N-28). The watch pings on a pool of its own (N-26 c3).
    private sealed class DatabaseAnswers(Database database) : IHealthCheck
    {
        public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default) =>
            Task.FromResult(database.Watch.Up
                ? HealthCheckResult.Healthy()
                : HealthCheckResult.Unhealthy("The database watch says the database is down."));
    }
}
