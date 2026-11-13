// Culinary Blog. Authorship is recorded in the repository history.

using CulinaryBlog.Application.Media;
using CulinaryBlog.Infrastructure.Persistence;
using Hangfire;
using Microsoft.Extensions.Logging;

namespace CulinaryBlog.Infrastructure.Jobs;

/// <summary>Represents the object deletion job.</summary>
/// <param name="fileStorage">The file storage supplied to this operation.</param>
/// <param name="logger">The logger supplied to this operation.</param>
/// <param name="dbContext">The db context supplied to this operation.</param>
public sealed class ObjectDeletionJob(IFileStorageService fileStorage, ILogger<ObjectDeletionJob> logger, AppDbContext dbContext)
{
    private static readonly Action<ILogger, Guid, int, Exception?> DeletionCompleted =
        LoggerMessage.Define<Guid, int>(
            LogLevel.Information,
            new EventId(4201, nameof(DeletionCompleted)),
            "Object deletion job {OutboxId} completed for {ObjectCount} objects");

    private static readonly Action<ILogger, Guid, Exception?> DeletionFailed =
        LoggerMessage.Define<Guid>(
            LogLevel.Error,
            new EventId(4202, nameof(DeletionFailed)),
            "Object deletion job {OutboxId} failed");

    /// <summary>Deletes the object deletion job.</summary>
    /// <param name="outboxId">The outbox id supplied to this operation.</param>
    /// <param name="objectKeys">The object keys supplied to this operation.</param>
    /// <param name="cancellationToken">Cancels the operation when requested by the caller.</param>
    /// <returns>A task representing the operation and its result.</returns>
    [AutomaticRetry(Attempts = 3, DelaysInSeconds = [60, 300, 1800])]
    public async Task DeleteAsync(Guid outboxId, string[] objectKeys, CancellationToken cancellationToken)
    {
        var message = await MediaJobLogContext.ReadAsync(dbContext, outboxId, cancellationToken).ConfigureAwait(false);
        using var logScope = MediaJobLogContext.Begin(logger, message, outboxId);
        string[] keys = [.. objectKeys.Where(key => !string.IsNullOrWhiteSpace(key)).SelectMany(IncludeVariants).Distinct(StringComparer.Ordinal)];
        try
        {
            foreach (string? objectKey in keys)
            {
                await fileStorage.DeleteAsync(objectKey, cancellationToken).ConfigureAwait(false);
            }
        }
        catch (Exception exception)
        {
            DeletionFailed(logger, outboxId, exception);
            throw;
        }

        MediaJobMetrics.Completed.Add(1, new KeyValuePair<string, object?>("job.type", "objects.delete"));
        DeletionCompleted(logger, outboxId, keys.Length, null);
    }

    private static IEnumerable<string> IncludeVariants(string objectKey)
    {
        yield return objectKey;
        int separator = objectKey.LastIndexOf('/');
        if (separator >= 0 && objectKey[(separator + 1)..].StartsWith("original.", StringComparison.Ordinal))
        {
            string prefix = objectKey[..(separator + 1)];
            yield return prefix + "medium.webp";
            yield return prefix + "thumbnail.webp";
        }
    }
}
