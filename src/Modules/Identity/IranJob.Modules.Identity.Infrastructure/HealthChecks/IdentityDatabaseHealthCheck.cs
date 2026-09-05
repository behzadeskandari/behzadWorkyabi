using IranJob.Modules.Identity.Infrastructure.Persistence;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace IranJob.Modules.Identity.Infrastructure.HealthChecks;

public sealed class IdentityDatabaseHealthCheck(IdentityDbContext dbContext) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        var canConnect = await dbContext.Database.CanConnectAsync(cancellationToken);
        return canConnect
            ? HealthCheckResult.Healthy("Identity database is reachable.")
            : HealthCheckResult.Unhealthy("Identity database is unavailable.");
    }
}
