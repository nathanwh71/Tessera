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
/// The health check at <c>/health</c>: an address a watching program asks to learn whether the
/// service can do its work (N-25). This class sits in the Service project, beside the program;
/// TesseraService calls <see cref="AddTesseraHealth"/> and <see cref="MapTesseraHealth"/> at start-up.
/// </summary>
/// <remarks>
/// <para>
/// The answer is one of three, each a status code, the number that says how a request went, and a
/// line of JSON, a plain text format for data:
/// 200 <c>{"status":"UP"}</c> when the service can serve tiles;
/// 503 <c>{"status":"OUT_OF_SERVICE"}</c> while the first list of layers loads;
/// 503 <c>{"status":"DOWN"}</c> when the database does not answer.
/// pg_tileserv, the tile server whose settings and addresses Tessera follows, answers at the same
/// address, and the Java build, this service's twin written in Java, gives the same three answers
/// there (ADR 0006 rule 6).
/// </para>
/// <para>
/// Two checks must both pass: the list of layers has loaded at least once (N-23), and the database
/// answers a ping, a tiny query, within a second. The ping has a pool of its own, a set of open
/// connections kept ready, so it never waits behind tile queries (N-26 c3).
/// </para>
/// <para>
/// The answer names no check and no fault, so a stranger learns nothing of the service's insides;
/// the log, the running record of events the service writes, holds the detail. The class lives
/// here, in the program, because it reads both the catalogue and the database, and no module may
/// reference both.
/// </para>
/// </remarks>
public static class Health
{
    /// <summary>The address of the health check.</summary>
    public const string Path = "/health";

    /// <summary>Registers the two checks, under the names 'catalogue' and 'database'.</summary>
    public static IServiceCollection AddTesseraHealth(this IServiceCollection services, Database database) =>
        services.AddHealthChecks()
            .AddCheck<CatalogueLoaded>("catalogue")
            .AddCheck("database", new DatabaseAnswers(database))
            .Services;

    /// <summary>
    /// Adds the route, the code that answers the address, for <c>/health</c>. Both kinds of failure
    /// answer 503; the word in the body tells them apart.
    /// </summary>
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
                // A watcher asks again each time, so no stored copy of an old answer may stand in for
                // a new one.
                context.Response.Headers.CacheControl = "no-store";
                context.Response.ContentType = "application/json";
                return context.Response.WriteAsync($$"""{"status":"{{Status(report)}}"}""");
            },
        });
        return routes;
    }

    /// <summary>
    /// The word the Java build's health check gives for the same state, so both builds agree.
    /// DOWN outranks OUT_OF_SERVICE, which outranks UP, so a database that does not answer gives
    /// DOWN whatever the state of the list.
    /// </summary>
    public static string Status(HealthReport report) =>
        report.Entries.TryGetValue("database", out var database) && database.Status != HealthStatus.Healthy ? "DOWN"
            : report.Status != HealthStatus.Healthy ? "OUT_OF_SERVICE"
            : "UP";

    // .NET, the toolkit the service runs on, builds this check itself and hands it the live
    // catalogue, the one copy the whole service shares.
    private sealed class CatalogueLoaded(LiveCatalogue catalogue) : IHealthCheck
    {
        public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default) =>
            Task.FromResult(catalogue.Current.Match(
                _ => HealthCheckResult.Healthy(),
                () => HealthCheckResult.Unhealthy("The list of layers is not ready yet.")));
    }

    // Pings the database; a ping waits a second at most, so /health answers within one.
    private sealed class DatabaseAnswers(Database database) : IHealthCheck
    {
        public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default) =>
            await database.Ping(cancellationToken).ConfigureAwait(false)
                ? HealthCheckResult.Healthy()
                : HealthCheckResult.Unhealthy("The database does not answer.");
    }
}
