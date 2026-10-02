using CulinaryBlog.Application.Abstractions.Caching;
using CulinaryBlog.Infrastructure.Jobs;

namespace CulinaryBlog.Api.Presentation;

internal sealed class PublicCacheInvalidationFilter(
    IApplicationCache cache,
    PublicContentRefreshScheduler refreshScheduler) : IEndpointFilter
{
    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        var result = await next(context).ConfigureAwait(false);
        if (result is IStatusCodeHttpResult { StatusCode: >= StatusCodes.Status400BadRequest })
        {
            return result;
        }

        await cache.RemoveByTagAsync(PublicCacheKeys.RecipesTag, context.HttpContext.RequestAborted)
            .ConfigureAwait(false);
        await cache.RemoveByTagAsync(PublicCacheKeys.CategoriesTag, context.HttpContext.RequestAborted)
            .ConfigureAwait(false);
        await refreshScheduler.ScheduleAsync(CancellationToken.None).ConfigureAwait(false);
        return result;
    }
}
