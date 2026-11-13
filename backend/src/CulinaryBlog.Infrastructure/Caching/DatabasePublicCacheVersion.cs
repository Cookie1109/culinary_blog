// Culinary Blog. Authorship is recorded in the repository history.

using CulinaryBlog.Application.Abstractions.Caching;
using CulinaryBlog.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CulinaryBlog.Infrastructure.Caching;

/// <summary>Represents the database public cache version.</summary>
/// <param name="db">The db supplied to this operation.</param>
internal sealed class DatabasePublicCacheVersion(AppDbContext db) : IPublicCacheVersion
{
    private Task<long>? version;

    // One cheap metadata read per request, shared by HTTP and data cache keys.
    // PostgreSQL updates this version in the same transaction as content changes.

    /// <summary>Gets the database public cache version.</summary>
    /// <param name="cancellationToken">Cancels the operation when requested by the caller.</param>
    /// <returns>A task representing the operation and its result.</returns>
    public Task<long> GetAsync(CancellationToken cancellationToken = default) => this.version ??=
        db.Database.SqlQuery<long>($"SELECT \"Generation\" AS \"Value\" FROM \"PublicCacheState\" WHERE \"Id\" = 1")
            .SingleAsync(cancellationToken);
}
