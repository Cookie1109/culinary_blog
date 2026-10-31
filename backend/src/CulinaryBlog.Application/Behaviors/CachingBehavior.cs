// Culinary Blog. Authorship is recorded in the repository history.

using CulinaryBlog.Application.Abstractions.Caching;
using MediatR;

namespace CulinaryBlog.Application.Behaviors;

/// <summary>Represents the caching behavior.</summary>
/// <param name="cache">The cache supplied to this operation.</param>
/// <typeparam name="TRequest">The request type.</typeparam>
/// <typeparam name="TResponse">The response type.</typeparam>
internal sealed class CachingBehavior<TRequest, TResponse>(IApplicationCache cache)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    /// <summary>Handles the caching behavior.</summary>
    /// <param name="request">The request supplied to this operation.</param>
    /// <param name="next">The next supplied to this operation.</param>
    /// <param name="cancellationToken">Cancels the operation when requested by the caller.</param>
    /// <returns>A task representing the operation and its result.</returns>
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        if (request is not ICacheableRequest<TResponse> cacheable)
        {
            return await next(cancellationToken).ConfigureAwait(false);
        }

        var cached = await cache.GetAsync<TResponse>(cacheable.CacheKey, cancellationToken).ConfigureAwait(false);
        if (cached is not null)
        {
            return cached;
        }

        var response = await next(cancellationToken).ConfigureAwait(false);
        await cache.SetAsync(
                cacheable.CacheKey,
                response,
                cacheable.CacheDuration,
                cacheable.CacheTags,
                cancellationToken)
            .ConfigureAwait(false);

        return response;
    }
}
