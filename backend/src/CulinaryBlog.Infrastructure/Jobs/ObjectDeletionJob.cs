using CulinaryBlog.Application.Media;
using Hangfire;
using Microsoft.Extensions.Logging;

namespace CulinaryBlog.Infrastructure.Jobs;

public sealed class ObjectDeletionJob(IFileStorageService fileStorage, ILogger<ObjectDeletionJob> logger)
{
    private static readonly Action<ILogger, Guid, int, Exception?> DeletionCompleted =
        LoggerMessage.Define<Guid, int>(LogLevel.Information, new EventId(4201, nameof(DeletionCompleted)),
            "Object deletion job {OutboxId} completed for {ObjectCount} objects");

    [AutomaticRetry(Attempts = 3, DelaysInSeconds = [60, 300, 1800])]
    public async Task DeleteAsync(Guid outboxId, string[] objectKeys, CancellationToken cancellationToken)
    {
        foreach (var objectKey in objectKeys.Where(key => !string.IsNullOrWhiteSpace(key)).Distinct(StringComparer.Ordinal))
        {
            await fileStorage.DeleteAsync(objectKey, cancellationToken).ConfigureAwait(false);
        }

        MediaJobMetrics.Completed.Add(1, new KeyValuePair<string, object?>("job.type", "objects.delete"));
        DeletionCompleted(logger, outboxId, objectKeys.Length, null);
    }
}
