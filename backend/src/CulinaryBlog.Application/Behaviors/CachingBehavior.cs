using CulinaryBlog.Application.Abstractions.Caching;
using MediatR;

namespace CulinaryBlog.Application.Behaviors;

internal sealed class CachingBehavior<TRequest, TResponse>(IApplicationCache cache)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
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
                cancellationToken)
            .ConfigureAwait(false);

        return response;
    }
}
