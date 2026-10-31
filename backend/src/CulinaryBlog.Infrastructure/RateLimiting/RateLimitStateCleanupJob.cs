// Culinary Blog. Authorship is recorded in the repository history.

using CulinaryBlog.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace CulinaryBlog.Infrastructure.RateLimiting;

/// <summary>Represents the rate limit state cleanup job.</summary>
/// <param name="db">The db supplied to this operation.</param>
public sealed class RateLimitStateCleanupJob(AppDbContext db)
{
    /// <summary>Performs the run operation.</summary>
    /// <param name="cancellationToken">Cancels the operation when requested by the caller.</param>
    /// <returns>A task representing the operation and its result.</returns>
    public async Task RunAsync(CancellationToken cancellationToken)
    {
        await db.Database.ExecuteSqlAsync(
            $"""
            WITH expired AS (
                SELECT "Peer" FROM "SharedRateLimitState"
                WHERE "ExpiresAtMilliseconds" < floor(extract(epoch FROM clock_timestamp()) * 1000)::bigint
                ORDER BY "ExpiresAtMilliseconds" LIMIT 1000 FOR UPDATE SKIP LOCKED)
            DELETE FROM "SharedRateLimitState" s USING expired e WHERE s."Peer" = e."Peer"
            """,
            cancellationToken).ConfigureAwait(false);
    }
}
