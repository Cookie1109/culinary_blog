// Culinary Blog. Authorship is recorded in the repository history.

using CulinaryBlog.Application.Abstractions.Caching;
using CulinaryBlog.Application.Content;
using CulinaryBlog.Application.Media;
using CulinaryBlog.Domain.Recipes;
using CulinaryBlog.Infrastructure.Media;
using CulinaryBlog.Infrastructure.Persistence;
using Hangfire;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SkiaSharp;

namespace CulinaryBlog.Infrastructure.Jobs;

/// <summary>Represents the image processing job.</summary>
/// <param name="dbContext">The db context supplied to this operation.</param>
/// <param name="fileStorage">The file storage supplied to this operation.</param>
/// <param name="cache">The cache supplied to this operation.</param>
/// <param name="publicContentRefresh">The public content refresh supplied to this operation.</param>
/// <param name="logger">The logger supplied to this operation.</param>
public sealed class ImageProcessingJob(
    AppDbContext dbContext,
    IFileStorageService fileStorage,
    IApplicationCache cache,
    IPublicContentRefresh publicContentRefresh,
    ILogger<ImageProcessingJob> logger)
{
    private static readonly Action<ILogger, Guid, Guid, Exception?> ProcessingCompleted =
        LoggerMessage.Define<Guid, Guid>(
            LogLevel.Information,
            new EventId(4101, nameof(ProcessingCompleted)),
            "Image resize job {OutboxId} completed for image {ImageId}");

    private static readonly Action<ILogger, Guid, Guid, Exception?> ProcessingFailed =
        LoggerMessage.Define<Guid, Guid>(
            LogLevel.Error,
            new EventId(4102, nameof(ProcessingFailed)),
            "Image resize job {OutboxId} failed for image {ImageId}");

    /// <summary>Performs the process operation.</summary>
    /// <param name="outboxId">The outbox id supplied to this operation.</param>
    /// <param name="imageId">The image id supplied to this operation.</param>
    /// <param name="cancellationToken">Cancels the operation when requested by the caller.</param>
    /// <returns>A task representing the operation and its result.</returns>
    [AutomaticRetry(Attempts = 3, DelaysInSeconds = [60, 300, 1800])]
    public async Task ProcessAsync(Guid outboxId, Guid imageId, CancellationToken cancellationToken)
    {
        var message = await MediaJobLogContext.ReadAsync(dbContext, outboxId, cancellationToken).ConfigureAwait(false);
        using var logScope = MediaJobLogContext.Begin(logger, message, outboxId);
        var imageRecord = await dbContext.RecipeImages.IgnoreQueryFilters()
            .SingleOrDefaultAsync(image => image.Id == imageId, cancellationToken)
            .ConfigureAwait(false);
        if (imageRecord is null || imageRecord.IsDeleted)
        {
            return;
        }

        if (imageRecord.ProcessingStatus == ImageProcessingStatus.Ready)
        {
            await this.RefreshPublicContentAsync().ConfigureAwait(false);
            return;
        }

        try
        {
            await using var original = await fileStorage.OpenReadAsync(imageRecord.ObjectKey, cancellationToken).ConfigureAwait(false);
            using var decoded = imageRecord.ContentType == "image/avif" ? AvifImage.Decode(original) : SKBitmap.Decode(original) ?? throw new InvalidDataException("The original image cannot be decoded.");
            string prefix = $"recipes/{imageRecord.RecipeId:N}/{imageRecord.Id:N}";
            string mediumKey = $"{prefix}/medium.webp";
            string thumbnailKey = $"{prefix}/thumbnail.webp";

            await this.UploadVariantAsync(decoded, mediumKey, 800, 600, cancellationToken).ConfigureAwait(false);
            await this.UploadVariantAsync(decoded, thumbnailKey, 300, 300, cancellationToken).ConfigureAwait(false);

            // Deletion may finish while binary uploads are in flight.
            await dbContext.Entry(imageRecord).ReloadAsync(cancellationToken).ConfigureAwait(false);
            if (imageRecord.IsDeleted || await dbContext.Recipes.IgnoreQueryFilters().AnyAsync(
                    recipe => recipe.Id == imageRecord.RecipeId && recipe.IsDeleted, cancellationToken).ConfigureAwait(false))
            {
                await fileStorage.DeleteAsync(mediumKey, cancellationToken).ConfigureAwait(false);
                await fileStorage.DeleteAsync(thumbnailKey, cancellationToken).ConfigureAwait(false);
                return;
            }

            imageRecord.MarkReady(mediumKey, thumbnailKey);
            await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            MediaJobMetrics.Completed.Add(1, new KeyValuePair<string, object?>("job.type", "image.resize"));
            ProcessingCompleted(logger, outboxId, imageId, null);
        }
        catch (Exception exception)
        {
            imageRecord.MarkFailed();
            await dbContext.SaveChangesAsync(CancellationToken.None).ConfigureAwait(false);
            MediaJobMetrics.Failed.Add(1, new KeyValuePair<string, object?>("job.type", "image.resize"));
            ProcessingFailed(logger, outboxId, imageId, exception);
            throw;
        }

        await this.RefreshPublicContentAsync().ConfigureAwait(false);
    }

    private async Task RefreshPublicContentAsync()
    {
        await cache.RemoveByTagAsync(PublicCacheKeys.RecipesTag, CancellationToken.None).ConfigureAwait(false);
        await cache.RemoveByTagAsync(PublicCacheKeys.CategoriesTag, CancellationToken.None).ConfigureAwait(false);
        await publicContentRefresh.ScheduleAsync(CancellationToken.None).ConfigureAwait(false);
    }

    private async Task UploadVariantAsync(
        SKBitmap source,
        string objectKey,
        int width,
        int height,
        CancellationToken cancellationToken)
    {
        // A fixed-size canvas preserves the source aspect ratio without stretching or cropping it.
        using var variant = new SKBitmap(width, height);
        using (var canvas = new SKCanvas(variant))
        {
            canvas.Clear(SKColors.White);
            float scale = Math.Min(width / (float)source.Width, height / (float)source.Height);
            float targetWidth = source.Width * scale;
            float targetHeight = source.Height * scale;
            var destination = SKRect.Create((width - targetWidth) / 2, (height - targetHeight) / 2, targetWidth, targetHeight);
            canvas.DrawBitmap(source, destination, new SKSamplingOptions(SKCubicResampler.Mitchell));
        }

        using var variantImage = SKImage.FromBitmap(variant);
        using var encoded = variantImage.Encode(SKEncodedImageFormat.Webp, 90);
        await using var output = new MemoryStream();
        encoded.SaveTo(output);
        output.Position = 0;
        await fileStorage.UploadAsync(objectKey, output, output.Length, "image/webp", cancellationToken).ConfigureAwait(false);
    }
}
