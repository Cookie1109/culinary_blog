// Culinary Blog. Authorship is recorded in the repository history.

using MediatR;

namespace CulinaryBlog.Application.Abstractions.Caching;

/// <summary>Defines the cacheable request contract.</summary>
/// <typeparam name="TResponse">The response type.</typeparam>
public interface ICacheableRequest<out TResponse> : IRequest<TResponse>
{
    /// <summary>Gets the cache key.</summary>
    string CacheKey { get; }

    /// <summary>Gets the cache duration.</summary>
    TimeSpan CacheDuration { get; }

    /// <summary>Gets the cache tags.</summary>
    IReadOnlyCollection<string> CacheTags { get; }
}
