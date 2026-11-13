// Culinary Blog. Authorship is recorded in the repository history.

using CulinaryBlog.Application.Media;
using CulinaryBlog.Infrastructure.Configuration;
using Microsoft.Extensions.Options;
using Minio;
using Minio.DataModel.Args;

namespace CulinaryBlog.Infrastructure.Media;

/// <summary>Represents the minio file storage service.</summary>
/// <param name="minioClient">The minio client supplied to this operation.</param>
/// <param name="options">The options supplied to this operation.</param>
internal sealed class MinioFileStorageService(
    IMinioClient minioClient,
    IOptions<ObjectStorageOptions> options) : IFileStorageService
{
    private readonly string bucketName = options.Value.BucketName;

    /// <summary>Uploads the minio file storage service.</summary>
    /// <param name="objectKey">The object key supplied to this operation.</param>
    /// <param name="content">The content supplied to this operation.</param>
    /// <param name="length">The length supplied to this operation.</param>
    /// <param name="contentType">The content type supplied to this operation.</param>
    /// <param name="cancellationToken">Cancels the operation when requested by the caller.</param>
    /// <returns>A task representing the operation and its result.</returns>
    public Task UploadAsync(
        string objectKey,
        Stream content,
        long length,
        string contentType,
        CancellationToken cancellationToken) =>
        minioClient.PutObjectAsync(
            new PutObjectArgs()
                .WithBucket(this.bucketName)
                .WithObject(objectKey)
                .WithStreamData(content)
                .WithObjectSize(length)
                .WithContentType(contentType),
            cancellationToken);

    /// <summary>Performs the open read operation.</summary>
    /// <param name="objectKey">The object key supplied to this operation.</param>
    /// <param name="cancellationToken">Cancels the operation when requested by the caller.</param>
    /// <returns>A task representing the operation and its result.</returns>
    public async Task<Stream> OpenReadAsync(string objectKey, CancellationToken cancellationToken)
    {
        var result = new MemoryStream();
        await minioClient.GetObjectAsync(
            new GetObjectArgs()
                .WithBucket(this.bucketName)
                .WithObject(objectKey)
                .WithCallbackStream(stream => stream.CopyTo(result)),
            cancellationToken).ConfigureAwait(false);
        result.Position = 0;
        return result;
    }

    /// <summary>Deletes the minio file storage service.</summary>
    /// <param name="objectKey">The object key supplied to this operation.</param>
    /// <param name="cancellationToken">Cancels the operation when requested by the caller.</param>
    /// <returns>A task representing the operation and its result.</returns>
    public Task DeleteAsync(string objectKey, CancellationToken cancellationToken) =>
        minioClient.RemoveObjectAsync(
            new RemoveObjectArgs().WithBucket(this.bucketName).WithObject(objectKey),
            cancellationToken);

    /// <summary>Performs the exists operation.</summary>
    /// <param name="objectKey">The object key supplied to this operation.</param>
    /// <param name="cancellationToken">Cancels the operation when requested by the caller.</param>
    /// <returns>A task representing the operation and its result.</returns>
    public async Task<bool> ExistsAsync(string objectKey, CancellationToken cancellationToken)
    {
        try
        {
            await minioClient.StatObjectAsync(
                new StatObjectArgs().WithBucket(this.bucketName).WithObject(objectKey),
                cancellationToken).ConfigureAwait(false);
            return true;
        }
        catch (Minio.Exceptions.ObjectNotFoundException)
        {
            return false;
        }
    }

    /// <summary>Lists keys.</summary>
    /// <param name="prefix">The prefix supplied to this operation.</param>
    /// <param name="cancellationToken">Cancels the operation when requested by the caller.</param>
    /// <returns>The list keys result.</returns>
    public async IAsyncEnumerable<string> ListKeysAsync(
        string prefix,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken)
    {
        await foreach (var item in minioClient.ListObjectsEnumAsync(
            new ListObjectsArgs().WithBucket(this.bucketName).WithPrefix(prefix).WithRecursive(true),
            cancellationToken).ConfigureAwait(false))
        {
            yield return item.Key;
        }
    }
}
