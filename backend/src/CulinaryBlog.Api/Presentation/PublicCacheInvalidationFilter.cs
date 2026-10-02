using CulinaryBlog.Application.Abstractions.Caching;

namespace CulinaryBlog.Api.Presentation;

internal sealed class PublicCacheInvalidationFilter(IApplicationCache cache) : IEndpointFilter
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
        return result;
    }
}
