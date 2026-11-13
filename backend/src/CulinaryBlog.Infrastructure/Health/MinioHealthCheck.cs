// Culinary Blog. Authorship is recorded in the repository history.

using CulinaryBlog.Infrastructure.Configuration;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using Minio;
using Minio.DataModel.Args;

namespace CulinaryBlog.Infrastructure.Health;

/// <summary>Represents the minio health check.</summary>
/// <param name="client">The client supplied to this operation.</param>
/// <param name="options">The options supplied to this operation.</param>
internal sealed class MinioHealthCheck(IMinioClient client, IOptions<ObjectStorageOptions> options) : IHealthCheck
{
    /// <summary>Performs the check health operation.</summary>
    /// <param name="context">The context supplied to this operation.</param>
    /// <param name="cancellationToken">Cancels the operation when requested by the caller.</param>
    /// <returns>A task representing the operation and its result.</returns>
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
