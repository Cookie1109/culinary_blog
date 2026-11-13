// Culinary Blog. Authorship is recorded in the repository history.

namespace CulinaryBlog.Application.Abstractions.Caching;

/// <summary>Defines the application cache contract.</summary>
public interface IApplicationCache
{
    /// <summary>Gets the i application cache.</summary>
    /// <param name="key">The key supplied to this operation.</param>
    /// <param name="cancellationToken">Cancels the operation when requested by the caller.</param>
    /// <typeparam name="T">The  type.</typeparam>
    /// <returns>A task representing the operation and its result.</returns>
    Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default);

    /// <summary>Performs the set operation.</summary>
    /// <param name="key">The key supplied to this operation.</param>
    /// <param name="value">The value supplied to this operation.</param>
    /// <param name="absoluteExpiration">The absolute expiration supplied to this operation.</param>
    /// <param name="tags">The tags supplied to this operation.</param>
    /// <param name="cancellationToken">Cancels the operation when requested by the caller.</param>
    /// <typeparam name="T">The  type.</typeparam>
    /// <returns>A task representing the operation and its result.</returns>
    Task SetAsync<T>(
        string key,
        T value,
        TimeSpan absoluteExpiration,
        IReadOnlyCollection<string> tags,
        CancellationToken cancellationToken = default);

    /// <summary>Removes the i application cache.</summary>
    /// <param name="key">The key supplied to this operation.</param>
    /// <param name="cancellationToken">Cancels the operation when requested by the caller.</param>
    /// <returns>A task representing the operation and its result.</returns>
    Task RemoveAsync(string key, CancellationToken cancellationToken = default);

    /// <summary>Removes by tag.</summary>
    /// <param name="tag">The tag supplied to this operation.</param>
    /// <param name="cancellationToken">Cancels the operation when requested by the caller.</param>
    /// <returns>A task representing the operation and its result.</returns>
    Task RemoveByTagAsync(string tag, CancellationToken cancellationToken = default);
}
