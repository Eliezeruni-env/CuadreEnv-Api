using Microsoft.Extensions.Diagnostics.HealthChecks;
using Onion.DataAccess;

namespace crud_onion.Health;

public sealed class DatabaseHealthCheck : IHealthCheck
{
    private readonly OnionDbContext _db;

    public DatabaseHealthCheck(OnionDbContext db)
    {
        _db = db;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var canConnect = await _db.Database.CanConnectAsync(cancellationToken);
            return canConnect
                ? HealthCheckResult.Healthy("Database is reachable.")
                : HealthCheckResult.Unhealthy("Database is not reachable.");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("Database health check failed.", ex);
        }
    }
}
