using CulinaryBlog.Application.Media;
using CulinaryBlog.Domain.Recipes;
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
            using var decoded = SKBitmap.Decode(original)
                ?? throw new InvalidDataException("The original image cannot be decoded.");
            var prefix = $"recipes/{imageRecord.RecipeId:N}/{imageRecord.Id:N}";
            var mediumKey = $"{prefix}/medium.webp";
            var thumbnailKey = $"{prefix}/thumbnail.webp";

            await UploadVariantAsync(decoded, mediumKey, 800, 600, cancellationToken).ConfigureAwait(false);
            await UploadVariantAsync(decoded, thumbnailKey, 300, 300, cancellationToken).ConfigureAwait(false);

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
        var scale = Math.Min(width / (double)source.Width, height / (double)source.Height);
        scale = Math.Min(scale, 1d);
        var targetWidth = Math.Max(1, (int)Math.Round(source.Width * scale));
        var targetHeight = Math.Max(1, (int)Math.Round(source.Height * scale));
        using var variant = source.Resize(
            new SKImageInfo(targetWidth, targetHeight),
            new SKSamplingOptions(SKCubicResampler.Mitchell))
            ?? throw new InvalidDataException("The image variant cannot be resized.");
        using var variantImage = SKImage.FromBitmap(variant);
        using var encoded = variantImage.Encode(SKEncodedImageFormat.Webp, 82);
        await using var output = new MemoryStream();
        encoded.SaveTo(output);
        output.Position = 0;
        await fileStorage.UploadAsync(objectKey, output, output.Length, "image/webp", cancellationToken).ConfigureAwait(false);
    }
}
