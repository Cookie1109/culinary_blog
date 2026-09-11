using MediatR;

namespace CulinaryBlog.Application.Abstractions.Caching;

public interface ICacheableRequest<out TResponse> : IRequest<TResponse>
{
    string CacheKey { get; }

    TimeSpan CacheDuration { get; }
}
