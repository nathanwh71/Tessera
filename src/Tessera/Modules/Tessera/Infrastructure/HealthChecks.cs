using Microsoft.Extensions.Diagnostics.HealthChecks;
using Modules.Tessera.Infrastructure.Sql;

namespace Modules.Tessera.Infrastructure;

/// <summary>
/// The <b><i>health check</i></b> on the database: healthy when it answers a ping within a second,
/// through the same pool as the tiles, so <c>/health</c> answers within one (N-25, N-26 c3).
/// </summary>
public sealed class DatabaseAnswers(Database database) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context, CancellationToken cancellationToken = default) =>
        await database.Ping(cancellationToken).ConfigureAwait(false)
            ? HealthCheckResult.Healthy()
            : HealthCheckResult.Unhealthy();
}

/// <summary>
/// The <b><i>health check</i></b> on the list of layers: degraded until the first list loads, since
/// until then every tile answers 503 though the service itself is well (N-23 c3, N-25).
/// </summary>
public sealed class CatalogueReady(LiveCatalogue catalogue) : IHealthCheck
{
    public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default) =>
        Task.FromResult(catalogue.Current.HasNoValue ? HealthCheckResult.Degraded() : HealthCheckResult.Healthy());
}
