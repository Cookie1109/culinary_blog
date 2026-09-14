using CulinaryBlog.Application.Media;
using CulinaryBlog.Infrastructure.Configuration;
using Microsoft.Extensions.Options;
using Minio;
using Minio.DataModel.Args;

namespace CulinaryBlog.Infrastructure.Media;

internal sealed class MinioFileStorageService(
    IMinioClient minioClient,
    IOptions<ObjectStorageOptions> options) : IFileStorageService
{
    private readonly string _bucketName = options.Value.BucketName;

    public Task UploadAsync(
        string objectKey,
        Stream content,
        long length,
        string contentType,
        CancellationToken cancellationToken) =>
        minioClient.PutObjectAsync(
            new PutObjectArgs()
                .WithBucket(_bucketName)
                .WithObject(objectKey)
                .WithStreamData(content)
                .WithObjectSize(length)
                .WithContentType(contentType),
            cancellationToken);

    public async Task<Stream> OpenReadAsync(string objectKey, CancellationToken cancellationToken)
    {
        var result = new MemoryStream();
        await minioClient.GetObjectAsync(
            new GetObjectArgs()
                .WithBucket(_bucketName)
                .WithObject(objectKey)
                .WithCallbackStream(stream => stream.CopyTo(result)),
            cancellationToken).ConfigureAwait(false);
        result.Position = 0;
        return result;
    }

    public Task DeleteAsync(string objectKey, CancellationToken cancellationToken) =>
        minioClient.RemoveObjectAsync(
            new RemoveObjectArgs().WithBucket(_bucketName).WithObject(objectKey),
            cancellationToken);

    public async Task<bool> ExistsAsync(string objectKey, CancellationToken cancellationToken)
    {
        try
        {
            await minioClient.StatObjectAsync(
                new StatObjectArgs().WithBucket(_bucketName).WithObject(objectKey),
                cancellationToken).ConfigureAwait(false);
            return true;
        }
        catch (Minio.Exceptions.ObjectNotFoundException)
        {
            return false;
        }
    }

    public async IAsyncEnumerable<string> ListKeysAsync(
        string prefix,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken)
    {
        await foreach (var item in minioClient.ListObjectsEnumAsync(
            new ListObjectsArgs().WithBucket(_bucketName).WithPrefix(prefix).WithRecursive(true),
            cancellationToken).ConfigureAwait(false))
        {
            yield return item.Key;
        }
    }
}
