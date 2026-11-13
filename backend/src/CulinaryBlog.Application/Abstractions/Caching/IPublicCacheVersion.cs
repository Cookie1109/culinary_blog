// Culinary Blog. Authorship is recorded in the repository history.

namespace CulinaryBlog.Application.Abstractions.Caching;

/// <summary>Defines the public cache version contract.</summary>
public interface IPublicCacheVersion
{
    /// <summary>Gets the i public cache version.</summary>
    /// <param name="cancellationToken">Cancels the operation when requested by the caller.</param>
    /// <returns>A task representing the operation and its result.</returns>
    Task<long> GetAsync(CancellationToken cancellationToken = default);
}
