// Culinary Blog. Authorship is recorded in the repository history.

namespace CulinaryBlog.Application.Media;

/// <summary>Defines the file storage service contract.</summary>
public interface IFileStorageService
{
    /// <summary>Uploads the i file storage service.</summary>
    /// <param name="objectKey">The object key supplied to this operation.</param>
    /// <param name="content">The content supplied to this operation.</param>
    /// <param name="length">The length supplied to this operation.</param>
    /// <param name="contentType">The content type supplied to this operation.</param>
    /// <param name="cancellationToken">Cancels the operation when requested by the caller.</param>
    /// <returns>A task representing the operation and its result.</returns>
    Task UploadAsync(string objectKey, Stream content, long length, string contentType, CancellationToken cancellationToken);

    /// <summary>Performs the open read operation.</summary>
    /// <param name="objectKey">The object key supplied to this operation.</param>
    /// <param name="cancellationToken">Cancels the operation when requested by the caller.</param>
    /// <returns>A task representing the operation and its result.</returns>
    Task<Stream> OpenReadAsync(string objectKey, CancellationToken cancellationToken);

    /// <summary>Deletes the i file storage service.</summary>
    /// <param name="objectKey">The object key supplied to this operation.</param>
    /// <param name="cancellationToken">Cancels the operation when requested by the caller.</param>
    /// <returns>A task representing the operation and its result.</returns>
    Task DeleteAsync(string objectKey, CancellationToken cancellationToken);

    /// <summary>Performs the exists operation.</summary>
    /// <param name="objectKey">The object key supplied to this operation.</param>
    /// <param name="cancellationToken">Cancels the operation when requested by the caller.</param>
    /// <returns>A task representing the operation and its result.</returns>
    Task<bool> ExistsAsync(string objectKey, CancellationToken cancellationToken);

    /// <summary>Lists keys.</summary>
    /// <param name="prefix">The prefix supplied to this operation.</param>
    /// <param name="cancellationToken">Cancels the operation when requested by the caller.</param>
    /// <returns>The list keys result.</returns>
    IAsyncEnumerable<string> ListKeysAsync(string prefix, CancellationToken cancellationToken);
}
