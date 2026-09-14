namespace CulinaryBlog.Application.Media;

public interface IFileStorageService
{
    Task UploadAsync(string objectKey, Stream content, long length, string contentType, CancellationToken cancellationToken);

    Task<Stream> OpenReadAsync(string objectKey, CancellationToken cancellationToken);

    Task DeleteAsync(string objectKey, CancellationToken cancellationToken);

    Task<bool> ExistsAsync(string objectKey, CancellationToken cancellationToken);

    IAsyncEnumerable<string> ListKeysAsync(string prefix, CancellationToken cancellationToken);
}
