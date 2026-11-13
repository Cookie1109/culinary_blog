// Culinary Blog. Authorship is recorded in the repository history.

using Microsoft.Extensions.Diagnostics.HealthChecks;
using StackExchange.Redis;

namespace CulinaryBlog.Infrastructure.Health;

/// <summary>Represents the redis health check.</summary>
/// <param name="connection">The connection supplied to this operation.</param>
internal sealed class RedisHealthCheck(IConnectionMultiplexer connection) : IHealthCheck
{
    /// <summary>Performs the check health operation.</summary>
    /// <param name="context">The context supplied to this operation.</param>
    /// <param name="cancellationToken">Cancels the operation when requested by the caller.</param>
    /// <returns>A task representing the operation and its result.</returns>
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await connection.GetDatabase().PingAsync().WaitAsync(cancellationToken).ConfigureAwait(false);
            return HealthCheckResult.Healthy();
        }
        catch (Exception exception)
        {
            return HealthCheckResult.Unhealthy("Redis is unavailable.", exception);
        }
    }
}
