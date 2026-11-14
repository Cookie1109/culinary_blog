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

    // HTTP and data cache keys share one metadata read per request.
    // PostgreSQL updates this version in the same transaction as content changes.

    /// <summary>Gets the database public cache version.</summary>
    /// <param name="cancellationToken">Cancels the operation when requested by the caller.</param>
    /// <returns>A task representing the operation and its result.</returns>
    public Task<long> GetAsync(CancellationToken cancellationToken = default) => this.version ??= this.RefreshAsync(cancellationToken);

    /// <summary>Reads the committed generation again after a content mutation.</summary>
    /// <param name="cancellationToken">Cancels the metadata read.</param>
    /// <returns>The current committed generation.</returns>
    public Task<long> RefreshAsync(CancellationToken cancellationToken = default)
    {
        this.version = db.Database.SqlQuery<long>($"SELECT \"Generation\" AS \"Value\" FROM \"PublicCacheState\" WHERE \"Id\" = 1")
            .SingleAsync(cancellationToken);

        return this.version;
    }
}
