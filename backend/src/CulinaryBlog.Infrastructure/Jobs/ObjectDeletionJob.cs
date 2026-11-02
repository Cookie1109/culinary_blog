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
        var keys = objectKeys.Where(key => !string.IsNullOrWhiteSpace(key)).SelectMany(IncludeVariants).Distinct(StringComparer.Ordinal).ToArray();
        foreach (var objectKey in keys)
        {
            await fileStorage.DeleteAsync(objectKey, cancellationToken).ConfigureAwait(false);
        }

        MediaJobMetrics.Completed.Add(1, new KeyValuePair<string, object?>("job.type", "objects.delete"));
        DeletionCompleted(logger, outboxId, keys.Length, null);
    }

    private static IEnumerable<string> IncludeVariants(string objectKey)
    {
        yield return objectKey;
        var separator = objectKey.LastIndexOf('/');
        if (separator >= 0 && objectKey[(separator + 1)..].StartsWith("original.", StringComparison.Ordinal))
        {
            var prefix = objectKey[..(separator + 1)];
            yield return prefix + "medium.webp";
            yield return prefix + "thumbnail.webp";
        }
    }
}
