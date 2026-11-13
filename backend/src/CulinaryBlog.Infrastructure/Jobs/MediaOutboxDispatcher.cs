// Culinary Blog. Authorship is recorded in the repository history.

using System.Text.Json;
using Hangfire;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace CulinaryBlog.Infrastructure.Jobs;

/// <summary>Represents the media outbox dispatcher.</summary>
/// <param name="scopeFactory">The scope factory supplied to this operation.</param>
/// <param name="timeProvider">The time provider supplied to this operation.</param>
/// <param name="logger">The logger supplied to this operation.</param>
internal sealed class MediaOutboxDispatcher(
    IServiceScopeFactory scopeFactory,
    TimeProvider timeProvider,
    ILogger<MediaOutboxDispatcher> logger) : BackgroundService
{
    private static readonly Action<ILogger, Guid, Exception?> DispatchFailed =
        LoggerMessage.Define<Guid>(
            LogLevel.Error,
            new EventId(4301, nameof(DispatchFailed)),
            "Failed to dispatch media outbox message {OutboxId}");

    private static readonly Action<ILogger, Exception?> BatchFailed =
        LoggerMessage.Define(
            LogLevel.Error,
            new EventId(4302, nameof(BatchFailed)),
            "Media outbox batch dispatch failed");

    /// <summary>Executes the media outbox dispatcher.</summary>
    /// <param name="stoppingToken">The stopping token supplied to this operation.</param>
    /// <returns>A task representing the operation and its result.</returns>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(5));
        do
        {
            try
            {
                await this.DispatchBatchAsync(stoppingToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                return;
            }
            catch (Exception exception)
            {
                BatchFailed(logger, exception);
            }
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
            using var logScope = MediaJobLogContext.Begin(logger, message, message.Id);
            using var trace = JobTraceContext.Start("outbox.media", System.Diagnostics.ActivityKind.Producer, message.TraceParent, message.CorrelationId);
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
