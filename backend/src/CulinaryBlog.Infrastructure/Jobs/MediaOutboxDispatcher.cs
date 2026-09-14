using System.Text.Json;
using Hangfire;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace CulinaryBlog.Infrastructure.Jobs;

internal sealed class MediaOutboxDispatcher(
    IServiceScopeFactory scopeFactory,
    TimeProvider timeProvider,
    ILogger<MediaOutboxDispatcher> logger) : BackgroundService
{
    private static readonly Action<ILogger, Guid, Exception?> DispatchFailed =
        LoggerMessage.Define<Guid>(LogLevel.Error, new EventId(4301, nameof(DispatchFailed)),
            "Failed to dispatch media outbox message {OutboxId}");

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(5));
        do
        {
            await DispatchBatchAsync(stoppingToken).ConfigureAwait(false);
        }
        while (await timer.WaitForNextTickAsync(stoppingToken).ConfigureAwait(false));
    }

    private async Task DispatchBatchAsync(CancellationToken cancellationToken)
    {
        using var scope = scopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<Persistence.AppDbContext>();
        var jobs = scope.ServiceProvider.GetRequiredService<IBackgroundJobClient>();
        var now = timeProvider.GetUtcNow();
        var messages = await dbContext.MediaOutbox
            .Where(message => message.ProcessedAt == null && message.NextAttemptAt <= now)
            .OrderBy(message => message.CreatedAt)
            .Take(20)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        foreach (var message in messages)
        {
            try
            {
                if (message.Type == MediaOutboxTypes.ResizeImage)
                {
                    var payload = JsonSerializer.Deserialize<ResizeImagePayload>(message.Payload)
                        ?? throw new InvalidOperationException("Resize image outbox payload is invalid.");
                    jobs.Enqueue<ImageProcessingJob>(job => job.ProcessAsync(message.Id, payload.ImageId, CancellationToken.None));
                }
                else if (message.Type == MediaOutboxTypes.DeleteObjects)
                {
                    var payload = JsonSerializer.Deserialize<DeleteObjectsPayload>(message.Payload)
                        ?? throw new InvalidOperationException("Delete objects outbox payload is invalid.");
                    jobs.Enqueue<ObjectDeletionJob>(job => job.DeleteAsync(message.Id, payload.ObjectKeys, CancellationToken.None));
                }
                else
                {
                    throw new InvalidOperationException($"Unsupported media outbox type '{message.Type}'.");
                }

                message.ProcessedAt = now;
                message.LastError = null;
                MediaJobMetrics.Completed.Add(1, new KeyValuePair<string, object?>("job.type", "outbox.dispatch"));
            }
            catch (Exception exception)
            {
                message.Attempts++;
                message.LastError = exception.Message[..Math.Min(exception.Message.Length, 2000)];
                message.NextAttemptAt = now.AddSeconds(Math.Min(300, 5 * Math.Pow(2, message.Attempts)));
                MediaJobMetrics.Failed.Add(1, new KeyValuePair<string, object?>("job.type", "outbox.dispatch"));
                DispatchFailed(logger, message.Id, exception);
            }
        }

        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }
}

internal static class MediaOutboxTypes
{
    public const string ResizeImage = "image.resize";
    public const string DeleteObjects = "objects.delete";
}

internal sealed record ResizeImagePayload(Guid ImageId);

internal sealed record DeleteObjectsPayload(string[] ObjectKeys);
