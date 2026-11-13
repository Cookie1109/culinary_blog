// Culinary Blog. Authorship is recorded in the repository history.

using CulinaryBlog.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace CulinaryBlog.Infrastructure.RateLimiting;

/// <summary>Represents the postgres rate limit counter.</summary>
/// <param name="scopes">The scopes supplied to this operation.</param>
public sealed class PostgresRateLimitCounter(IServiceScopeFactory scopes)
{
    /// <summary>Performs the reserve operation.</summary>
    /// <param name="ip">The ip supplied to this operation.</param>
    /// <param name="policy">The policy supplied to this operation.</param>
    /// <param name="globalLimit">The global limit supplied to this operation.</param>
    /// <param name="endpointLimit">The endpoint limit supplied to this operation.</param>
    /// <param name="cancellationToken">Cancels the operation when requested by the caller.</param>
    /// <returns>A task representing the operation and its result.</returns>
    public async Task<RateLimitReservation> ReserveAsync(string ip, string policy, int globalLimit, int endpointLimit, CancellationToken cancellationToken)
    {
        await using var scope = scopes.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        return await db.Database.SqlQuery<RateLimitReservation>(
            $"SELECT * FROM \"ReserveRateLimit\"({ip}, {policy}, {globalLimit}, {endpointLimit})")
            .SingleAsync(cancellationToken).ConfigureAwait(false);
    }
}
