// Culinary Blog. Authorship is recorded in the repository history.

using CulinaryBlog.Application.Content;
using CulinaryBlog.Infrastructure.Media;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CulinaryBlog.Infrastructure.Content;

/// <content>Contains category image storage.</content>
internal sealed partial class ContentService
{
    private const string CategoryImageUrlPrefix = "/api/v1/media/category/";
    private const string CategoryImageKeyPrefix = "category-images/";

    private static readonly Action<ILogger, string, Exception?> CategoryImageCleanupFailed = LoggerMessage.Define<string>(
        LogLevel.Warning,
        new EventId(4005, nameof(CategoryImageCleanupFailed)),
        "Could not remove unused category image {ObjectKey}");

    /// <summary>Validates and stores a category image.</summary>
    /// <param name="content">The uploaded image stream.</param>
    /// <param name="length">The declared image length in bytes.</param>
    /// <param name="contentType">The declared image MIME type.</param>
    /// <param name="cancellationToken">Cancels the operation.</param>
    /// <returns>The stored image location.</returns>
    public async Task<CategoryImageDto> UploadCategoryImageAsync(
        Stream content,
        long length,
        string contentType,
        CancellationToken cancellationToken)
    {
        if (length is <= 0 or > ImageUpload.MaximumBytes)
        {
            throw new ContentProblemException("FILE_SIZE_EXCEEDED", "Image size must be between 1 byte and 5 MB.", ContentProblemKind.BadRequest);
        }

        var temporaryDirectory = Directory.CreateTempSubdirectory("culinary-category-");
        var temporaryPath = Path.Combine(temporaryDirectory.FullName, "content");
        try
        {
            await ImageUpload.CopyToLimitedFileAsync(content, temporaryPath, cancellationToken).ConfigureAwait(false);
            var detectedType = await ImageUpload.DetectImageContentTypeAsync(temporaryPath, cancellationToken).ConfigureAwait(false);
            if (!string.Equals(contentType, detectedType, StringComparison.OrdinalIgnoreCase))
            {
                throw new ContentProblemException("FILE_SIGNATURE_INVALID", "Image signature does not match its MIME type.", ContentProblemKind.BadRequest);
            }

            var fileName = $"{Guid.NewGuid():N}{ImageUpload.Extensions[detectedType]}";
            try
            {
                await using var upload = File.OpenRead(temporaryPath);
                await fileStorage.UploadAsync($"{CategoryImageKeyPrefix}{fileName}", upload, upload.Length, detectedType, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception exception) when (exception is not OperationCanceledException)
            {
                throw new ContentProblemException("FILE_UPLOAD_FAILED", "The image storage service is unavailable.", ContentProblemKind.ServiceUnavailable);
            }

            return new CategoryImageDto($"{CategoryImageUrlPrefix}{fileName}");
        }
        finally
        {
            File.Delete(temporaryPath);
            temporaryDirectory.Delete();
        }
    }

    /// <summary>Deletes a replaced category image once no category references it.</summary>
    /// <param name="previousUrl">The image URL the category used before the update.</param>
    /// <param name="cancellationToken">Cancels the operation.</param>
    /// <returns>A task that completes when cleanup has been attempted.</returns>
    private async Task RemoveUnusedCategoryImageAsync(string? previousUrl, CancellationToken cancellationToken)
    {
        if (previousUrl is null || !previousUrl.StartsWith(CategoryImageUrlPrefix, StringComparison.Ordinal))
        {
            return;
        }

        var fileName = previousUrl[CategoryImageUrlPrefix.Length..];
        if (!IsCategoryImageFileName(fileName))
        {
            return;
        }

        var objectKey = $"{CategoryImageKeyPrefix}{fileName}";
        try
        {
            // Soft-deleted categories keep their image so they can be restored.
            if (await dbContext.Categories.IgnoreQueryFilters()
                .AnyAsync(category => category.ImageUrl == previousUrl, cancellationToken)
                .ConfigureAwait(false))
            {
                return;
            }

            await fileStorage.DeleteAsync(objectKey, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            CategoryImageCleanupFailed(logger, objectKey, exception);
        }
    }

    private static bool IsCategoryImageFileName(string fileName) =>
        ImageUpload.Extensions.Values.Contains(Path.GetExtension(fileName), StringComparer.Ordinal) &&
        Guid.TryParseExact(Path.GetFileNameWithoutExtension(fileName), "N", out _) &&
        fileName.Length == 32 + Path.GetExtension(fileName).Length;

    /// <summary>Reads a stored category image.</summary>
    /// <param name="fileName">The stored file name including its extension.</param>
    /// <param name="cancellationToken">Cancels the operation.</param>
    /// <returns>The image stream and content type.</returns>
    public async Task<MediaFileDto> OpenCategoryImageAsync(string fileName, CancellationToken cancellationToken)
    {
        var extension = Path.GetExtension(fileName);
        var contentType = ImageUpload.Extensions.FirstOrDefault(item => item.Value == extension).Key;
        if (contentType is null || !IsCategoryImageFileName(fileName))
        {
            throw NotFound("IMAGE_NOT_FOUND", "The category image was not found.");
        }

        var objectKey = $"{CategoryImageKeyPrefix}{fileName}";
        try
        {
            if (!await fileStorage.ExistsAsync(objectKey, cancellationToken).ConfigureAwait(false))
            {
                throw NotFound("IMAGE_NOT_FOUND", "The category image was not found.");
            }

            return new MediaFileDto(await fileStorage.OpenReadAsync(objectKey, cancellationToken).ConfigureAwait(false), contentType);
        }
        catch (Exception exception) when (exception is not (OperationCanceledException or ContentProblemException))
        {
            throw new ContentProblemException("FILE_UPLOAD_FAILED", "The image storage service is unavailable.", ContentProblemKind.ServiceUnavailable);
        }
    }
}
