using CulinaryBlog.Infrastructure.Configuration;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using Minio;
using Minio.DataModel.Args;

namespace CulinaryBlog.Infrastructure.Health;

internal sealed class MinioHealthCheck(IMinioClient client, IOptions<ObjectStorageOptions> options) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var exists = await client.BucketExistsAsync(
                    new BucketExistsArgs().WithBucket(options.Value.BucketName),
                    cancellationToken)
                .ConfigureAwait(false);

            return exists
                ? HealthCheckResult.Healthy()
                : HealthCheckResult.Unhealthy("The configured private bucket does not exist.");
        }
        catch (Exception exception)
        {
            return HealthCheckResult.Unhealthy("MinIO is unavailable.", exception);
        }
    }
}
