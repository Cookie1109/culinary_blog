// Culinary Blog. Authorship is recorded in the repository history.

using CulinaryBlog.Application.Content;
using SkiaSharp;

namespace CulinaryBlog.Infrastructure.Media;

/// <summary>Validates uploaded recipe and profile images.</summary>
internal static class ImageUpload
{
    /// <summary>The maximum permitted image length in bytes.</summary>
    internal const long MaximumBytes = 5 * 1024 * 1024;

    /// <summary>Maps accepted MIME types to their canonical file extensions.</summary>
    internal static readonly IReadOnlyDictionary<string, string> Extensions =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["image/jpeg"] = ".jpg",
            ["image/png"] = ".png",
            ["image/webp"] = ".webp",
            ["image/avif"] = ".avif",
        };

    /// <summary>Copies an image while enforcing the upload limit.</summary>
    /// <param name="source">The uploaded image stream.</param>
    /// <param name="path">The temporary destination file.</param>
    /// <param name="cancellationToken">Cancels the copy operation.</param>
    /// <returns>A task that completes when the bounded copy finishes.</returns>
    internal static async Task CopyToLimitedFileAsync(Stream source, string path, CancellationToken cancellationToken)
    {
        await using var destination = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.None, 64 * 1024, true);
        byte[] buffer = new byte[64 * 1024];
        long total = 0;
        int read;
        while ((read = await source.ReadAsync(buffer, cancellationToken).ConfigureAwait(false)) > 0)
        {
            total += read;
            if (total > MaximumBytes)
            {
                throw new ContentProblemException("FILE_SIZE_EXCEEDED", "Image size cannot exceed 5 MB.", ContentProblemKind.BadRequest);
            }

            await destination.WriteAsync(buffer.AsMemory(0, read), cancellationToken).ConfigureAwait(false);
        }
    }

    /// <summary>Checks image signatures, dimensions and decoding.</summary>
    /// <param name="path">The temporary image file to inspect.</param>
    /// <param name="cancellationToken">Cancels image validation.</param>
    /// <returns>The MIME type detected from the validated image.</returns>
    internal static Task<string> DetectImageContentTypeAsync(string path, CancellationToken cancellationToken)
    {
        try
        {
            using var input = File.OpenRead(path);
            cancellationToken.ThrowIfCancellationRequested();
            if (AvifImage.HasSignature(input))
            {
                using var avif = AvifImage.Decode(input);
                return Task.FromResult("image/avif");
            }

            using var codec = SKCodec.Create(input);
            if (codec is null || codec.Info.Width <= 0 || codec.Info.Height <= 0 ||
                (long)codec.Info.Width * codec.Info.Height > 40_000_000)
            {
                throw new ContentProblemException(
                    "FILE_SIGNATURE_INVALID", "The uploaded image dimensions are invalid or too large.", ContentProblemKind.BadRequest);
            }

            string? contentType = codec?.EncodedFormat switch
            {
                SKEncodedImageFormat.Jpeg => "image/jpeg",
                SKEncodedImageFormat.Png => "image/png",
                SKEncodedImageFormat.Webp => "image/webp",
                _ => null,
            };
            input.Position = 0;
            using var decoded = SKBitmap.Decode(input) ?? throw new ContentProblemException(
                    "FILE_SIGNATURE_INVALID", "The uploaded file cannot be decoded as an image.", ContentProblemKind.BadRequest);
            return Task.FromResult(contentType ?? throw new ContentProblemException(
                "FILE_SIGNATURE_INVALID", "The uploaded file is not a supported image.", ContentProblemKind.BadRequest));
        }
        catch (ContentProblemException)
        {
            throw;
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            throw new ContentProblemException("FILE_SIGNATURE_INVALID", "The uploaded file is not a valid image.", ContentProblemKind.BadRequest);
        }
    }
}
