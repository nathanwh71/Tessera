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
