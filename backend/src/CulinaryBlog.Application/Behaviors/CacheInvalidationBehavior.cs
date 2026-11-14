// Culinary Blog. Authorship is recorded in the repository history.

using CulinaryBlog.Application.Abstractions.Caching;
using CulinaryBlog.Application.Content;
using MediatR;
using Microsoft.Extensions.Logging;

namespace CulinaryBlog.Application.Behaviors;

/// <summary>Represents the cache invalidation behavior.</summary>
/// <param name="cache">The cache supplied to this operation.</param>
/// <param name="version">Tracks changes to publicly visible content.</param>
/// <param name="publicContentRefresh">The public content refresh supplied to this operation.</param>
/// <param name="logger">The logger supplied to this operation.</param>
/// <typeparam name="TRequest">The request type.</typeparam>
/// <typeparam name="TResponse">The response type.</typeparam>
internal sealed class CacheInvalidationBehavior<TRequest, TResponse>(
    IApplicationCache cache,
    IPublicCacheVersion version,
    IPublicContentRefresh publicContentRefresh,
    ILogger<CacheInvalidationBehavior<TRequest, TResponse>> logger) : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private static readonly Action<ILogger, string, Exception?> InvalidationFailed = LoggerMessage.Define<string>(
        LogLevel.Error,
        new EventId(1005, nameof(InvalidationFailed)),
        "Post-commit refresh {Operation} failed; cache TTL and scheduled refresh remain the fallback");

    /// <summary>Handles the cache invalidation behavior.</summary>
    /// <param name="request">The request supplied to this operation.</param>
    /// <param name="next">The next supplied to this operation.</param>
    /// <param name="cancellationToken">Cancels the operation when requested by the caller.</param>
    /// <returns>A task representing the operation and its result.</returns>
    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        var generationBefore = request is IContentMutation
            ? await version.GetAsync(cancellationToken).ConfigureAwait(false)
            : (long?)null;
        var response = await next(cancellationToken).ConfigureAwait(false);
        if (request is IContentMutation)
        {
            var publicContentChanged = true;
            try
            {
                publicContentChanged = await version.RefreshAsync(CancellationToken.None).ConfigureAwait(false) != generationBefore;
            }
            catch (Exception exception)
            {
                // A committed write must still evict conservatively if the metadata read fails.
                InvalidationFailed(logger, "generation", exception);
            }

            // The write has committed: finish invalidation even if the browser disconnects.
            if (publicContentChanged)
            {
                foreach (var tag in new[] { PublicCacheKeys.RecipesTag, PublicCacheKeys.CategoriesTag })
                {
                    try
                    {
                        await cache.RemoveByTagAsync(tag, CancellationToken.None).ConfigureAwait(false);
                    }
                    catch (Exception exception)
                    {
                        InvalidationFailed(logger, tag, exception);
                    }
                }
            }

            try
            {
                await publicContentRefresh.ScheduleAsync(CancellationToken.None).ConfigureAwait(false);
            }
            catch (Exception exception)
            {
                InvalidationFailed(logger, "schedule", exception);
            }
        }

        return response;
    }
}
