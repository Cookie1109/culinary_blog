// Culinary Blog. Authorship is recorded in the repository history.

using CulinaryBlog.Application.Auth;
using CulinaryBlog.Application.Content;
using CulinaryBlog.Infrastructure.Media;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CulinaryBlog.Infrastructure.Identity;

internal sealed partial class AuthService
{
    private static readonly Action<ILogger, string, Exception?> AvatarCleanupFailed = LoggerMessage.Define<string>(
        LogLevel.Warning,
        new EventId(4402, nameof(AvatarCleanupFailed)),
        "Could not remove obsolete avatar {ObjectKey}");

    /// <summary>Stores an avatar and commits the associated profile changes.</summary>
    public async Task<UserDto> UpdateProfileAvatarAsync(
        string userId,
        UpdateProfileRequest request,
        Stream content,
        long length,
        string contentType,
        CancellationToken cancellationToken)
    {
        var user = await this.FindActiveUserAsync(userId, cancellationToken).ConfigureAwait(false);
        if (length is <= 0 or > ImageUpload.MaximumBytes)
        {
            throw new ContentProblemException("FILE_SIZE_EXCEEDED", "Image size must be between 1 byte and 5 MB.", ContentProblemKind.BadRequest);
        }

        var temporaryDirectory = Directory.CreateTempSubdirectory("culinary-avatar-");
        var temporaryPath = Path.Combine(temporaryDirectory.FullName, "content");
        string? uploadedAvatar = null;
        var committed = false;
        try
        {
            await ImageUpload.CopyToLimitedFileAsync(content, temporaryPath, cancellationToken).ConfigureAwait(false);
            var detectedType = await ImageUpload.DetectImageContentTypeAsync(temporaryPath, cancellationToken).ConfigureAwait(false);
            if (!string.Equals(contentType, detectedType, StringComparison.OrdinalIgnoreCase))
            {
                throw new ContentProblemException("FILE_SIGNATURE_INVALID", "Image signature does not match its MIME type.", ContentProblemKind.BadRequest);
            }

            var fileName = $"{Guid.NewGuid():N}{ImageUpload.Extensions[detectedType]}";
            var owner = Uri.EscapeDataString(user.Id.ToString());
            var objectKey = $"avatars/{owner}/{fileName}";
            var publicBaseUrl = siteOptions.Value.PublicBaseUrl;
            uploadedAvatar = $"{publicBaseUrl.TrimEnd('/')}/api/v1/avatars/{owner}/{fileName}";
            await using (var upload = File.OpenRead(temporaryPath))
            {
                await fileStorage.UploadAsync(objectKey, upload, upload.Length, detectedType, cancellationToken).ConfigureAwait(false);
            }

            var result = await this.UpdateCurrentUserAsync(userId, request with { AvatarUrl = uploadedAvatar, UpdateAvatarUrl = true }, cancellationToken).ConfigureAwait(false);
            committed = true;
            return result;
        }
        finally
        {
            if (!committed && uploadedAvatar is not null)
            {
                await this.TryDeleteAvatarAsync(user.Id.ToString(), uploadedAvatar).ConfigureAwait(false);
            }

            File.Delete(temporaryPath);
            temporaryDirectory.Delete();
        }
    }

    /// <summary>Reads only the current avatar of an active account.</summary>
    public async Task<MediaFileDto> OpenProfileAvatarAsync(string userId, string fileName, CancellationToken cancellationToken)
    {
        var contentType = Path.GetExtension(fileName) switch
        {
            ".jpg" => "image/jpeg",
            ".png" => "image/png",
            ".webp" => "image/webp",
            ".avif" => "image/avif",
            _ => null,
        };
        if (contentType is null || !Guid.TryParseExact(Path.GetFileNameWithoutExtension(fileName), "N", out _))
        {
            throw new ContentProblemException("IMAGE_NOT_FOUND", "The profile avatar was not found.", ContentProblemKind.NotFound);
        }

        if (!Guid.TryParse(userId, out var id))
        {
            throw new ContentProblemException("IMAGE_NOT_FOUND", "The profile avatar was not found.", ContentProblemKind.NotFound);
        }

        var avatarUrl = await dbContext.Users.AsNoTracking()
            .Where(user => user.Id == id && user.IsActive)
            .Select(user => user.AvatarUrl)
            .SingleOrDefaultAsync(cancellationToken).ConfigureAwait(false);
        if (!Uri.TryCreate(avatarUrl, UriKind.Absolute, out var avatarUri) ||
            avatarUri.AbsolutePath != $"/api/v1/avatars/{Uri.EscapeDataString(userId)}/{fileName}")
        {
            throw new ContentProblemException("IMAGE_NOT_FOUND", "The profile avatar was not found.", ContentProblemKind.NotFound);
        }

        var objectKey = $"avatars/{Uri.EscapeDataString(userId)}/{fileName}";
        if (!await fileStorage.ExistsAsync(objectKey, cancellationToken).ConfigureAwait(false))
        {
            throw new ContentProblemException("IMAGE_NOT_FOUND", "The profile avatar was not found.", ContentProblemKind.NotFound);
        }

        return new MediaFileDto(await fileStorage.OpenReadAsync(objectKey, cancellationToken).ConfigureAwait(false), contentType);
    }

    private async Task TryDeleteAvatarAsync(string userId, string? avatarUrl)
    {
        var owner = Uri.EscapeDataString(userId);
        var prefix = $"/api/v1/avatars/{owner}/";
        if (!Uri.TryCreate(avatarUrl, UriKind.Absolute, out var uri) || !uri.AbsolutePath.StartsWith(prefix, StringComparison.Ordinal))
        {
            return;
        }

        var fileName = uri.AbsolutePath[prefix.Length..];
        var extension = Path.GetExtension(fileName);
        if (!Guid.TryParseExact(Path.GetFileNameWithoutExtension(fileName), "N", out _) || !ImageUpload.Extensions.Values.Contains(extension, StringComparer.Ordinal))
        {
            return;
        }

        var objectKey = $"avatars/{owner}/{fileName}";
        try
        {
            await fileStorage.DeleteAsync(objectKey, CancellationToken.None).ConfigureAwait(false);
        }
        catch (Exception exception)
        {
            AvatarCleanupFailed(logger, objectKey, exception);
        }
    }
}
