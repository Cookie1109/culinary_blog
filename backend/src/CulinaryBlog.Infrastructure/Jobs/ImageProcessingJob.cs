using CulinaryBlog.Application.Media;
using CulinaryBlog.Domain.Recipes;
using CulinaryBlog.Infrastructure.Media;
using CulinaryBlog.Infrastructure.Persistence;
using Hangfire;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SkiaSharp;

namespace CulinaryBlog.Infrastructure.Jobs;

public sealed class ImageProcessingJob(
    AppDbContext dbContext,
    IFileStorageService fileStorage,
    ILogger<ImageProcessingJob> logger)
{
    private static readonly Action<ILogger, Guid, Guid, Exception?> ProcessingCompleted =
        LoggerMessage.Define<Guid, Guid>(LogLevel.Information, new EventId(4101, nameof(ProcessingCompleted)),
            "Image resize job {OutboxId} completed for image {ImageId}");

    private static readonly Action<ILogger, Guid, Guid, Exception?> ProcessingFailed =
        LoggerMessage.Define<Guid, Guid>(LogLevel.Error, new EventId(4102, nameof(ProcessingFailed)),
            "Image resize job {OutboxId} failed for image {ImageId}");

    [AutomaticRetry(Attempts = 3, DelaysInSeconds = [60, 300, 1800])]
    public async Task ProcessAsync(Guid outboxId, Guid imageId, CancellationToken cancellationToken)
    {
        var imageRecord = await dbContext.RecipeImages.IgnoreQueryFilters()
            .SingleOrDefaultAsync(image => image.Id == imageId, cancellationToken)
            .ConfigureAwait(false);
        if (imageRecord is null || imageRecord.IsDeleted || imageRecord.ProcessingStatus == ImageProcessingStatus.Ready)
        {
            return;
        }

        try
        {
            await using var original = await fileStorage.OpenReadAsync(imageRecord.ObjectKey, cancellationToken).ConfigureAwait(false);
            using var decoded = imageRecord.ContentType == "image/avif" ? AvifImage.Decode(original) : SKBitmap.Decode(original)
                ?? throw new InvalidDataException("The original image cannot be decoded.");
            var prefix = $"recipes/{imageRecord.RecipeId:N}/{imageRecord.Id:N}";
            var mediumKey = $"{prefix}/medium.webp";
            var thumbnailKey = $"{prefix}/thumbnail.webp";

            await UploadVariantAsync(decoded, mediumKey, 800, 600, cancellationToken).ConfigureAwait(false);
            await UploadVariantAsync(decoded, thumbnailKey, 300, 300, cancellationToken).ConfigureAwait(false);

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
    }

    private async Task UploadVariantAsync(
        SKBitmap source,
        string objectKey,
        int width,
        int height,
        CancellationToken cancellationToken)
    {
        using var variant = new SKBitmap(width, height);
        using (var canvas = new SKCanvas(variant))
        {
            canvas.Clear(SKColors.White);
            var scale = Math.Min(width / (float)source.Width, height / (float)source.Height);
            var targetWidth = source.Width * scale;
            var targetHeight = source.Height * scale;
            canvas.DrawBitmap(source, SKRect.Create((width - targetWidth) / 2, (height - targetHeight) / 2, targetWidth, targetHeight),
                new SKSamplingOptions(SKCubicResampler.Mitchell));
        }
        using var variantImage = SKImage.FromBitmap(variant);
        using var encoded = variantImage.Encode(SKEncodedImageFormat.Webp, 90);
        await using var output = new MemoryStream();
        encoded.SaveTo(output);
        output.Position = 0;
        await fileStorage.UploadAsync(objectKey, output, output.Length, "image/webp", cancellationToken).ConfigureAwait(false);
    }
}
