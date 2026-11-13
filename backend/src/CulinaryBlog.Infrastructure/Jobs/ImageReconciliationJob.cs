// Culinary Blog. Authorship is recorded in the repository history.

using CulinaryBlog.Application.Media;
using CulinaryBlog.Infrastructure.Persistence;
using Hangfire;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CulinaryBlog.Infrastructure.Jobs;

/// <summary>Represents the image reconciliation job.</summary>
/// <param name="dbContext">The db context supplied to this operation.</param>
/// <param name="fileStorage">The file storage supplied to this operation.</param>
/// <param name="jobs">The jobs supplied to this operation.</param>
/// <param name="timeProvider">The time provider supplied to this operation.</param>
/// <param name="logger">The logger supplied to this operation.</param>
public sealed class ImageReconciliationJob(
    AppDbContext dbContext,
    IFileStorageService fileStorage,
    IBackgroundJobClient jobs,
    TimeProvider timeProvider,
    ILogger<ImageReconciliationJob> logger)
{
    private static readonly Action<ILogger, Guid, string, Exception?> MissingObject =
        LoggerMessage.Define<Guid, string>(
            LogLevel.Warning,
            new EventId(4401, nameof(MissingObject)),
            "Image record {ImageId} has no original object {ObjectKey}");

    private static readonly Action<ILogger, string, Exception?> OrphanObject =
        LoggerMessage.Define<string>(
            LogLevel.Warning,
            new EventId(4402, nameof(OrphanObject)),
            "Orphan media object found at {ObjectKey}");

    /// <summary>Performs the run operation.</summary>
    /// <param name="cancellationToken">Cancels the operation when requested by the caller.</param>
    /// <returns>A task representing the operation and its result.</returns>
    [AutomaticRetry(Attempts = 1)]
    public async Task RunAsync(CancellationToken cancellationToken)
    {
        var images = await dbContext.RecipeImages.IgnoreQueryFilters().AsNoTracking().ToListAsync(cancellationToken).ConfigureAwait(false);
        var knownKeys = images.SelectMany(image => new[] { image.ObjectKey, image.MediumObjectKey, image.ThumbnailObjectKey })
            .Where(key => key is not null)
            .ToHashSet(StringComparer.Ordinal);

        foreach (var image in images.Where(image => !image.IsDeleted))
        {
            if (!await fileStorage.ExistsAsync(image.ObjectKey, cancellationToken).ConfigureAwait(false))
            {
                MediaJobMetrics.Orphans.Add(1, new KeyValuePair<string, object?>("orphan.type", "missing_object"));
                MissingObject(logger, image.Id, image.ObjectKey, null);
            }

            if (image.ProcessingStatus != Domain.Recipes.ImageProcessingStatus.Ready &&
                image.CreatedAt < timeProvider.GetUtcNow().AddMinutes(-5))
            {
                jobs.Enqueue<ImageProcessingJob>(job => job.ProcessAsync(Guid.NewGuid(), image.Id, CancellationToken.None));
            }
        }

        await foreach (var objectKey in fileStorage.ListKeysAsync("recipes/", cancellationToken).ConfigureAwait(false))
        {
            if (!knownKeys.Contains(objectKey))
            {
                MediaJobMetrics.Orphans.Add(1, new KeyValuePair<string, object?>("orphan.type", "missing_record"));
                OrphanObject(logger, objectKey, null);
            }
        }
    }
}
