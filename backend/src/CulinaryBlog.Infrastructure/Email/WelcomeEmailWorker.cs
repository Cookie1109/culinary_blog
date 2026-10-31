// Culinary Blog. Authorship is recorded in the repository history.

using CulinaryBlog.Infrastructure.Persistence;
using Hangfire;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CulinaryBlog.Infrastructure.Email;

/// <summary>Represents the welcome email worker.</summary>
/// <param name="scopeFactory">The scope factory supplied to this operation.</param>
/// <param name="options">The options supplied to this operation.</param>
/// <param name="timeProvider">The time provider supplied to this operation.</param>
/// <param name="logger">The logger supplied to this operation.</param>
internal sealed partial class WelcomeEmailWorker(
    IServiceScopeFactory scopeFactory,
    IOptions<EmailOptions> options,
    TimeProvider timeProvider,
    ILogger<WelcomeEmailWorker> logger) : BackgroundService
{
    /// <summary>Performs the dispatch operation.</summary>
    /// <param name="cancellationToken">Cancels the operation when requested by the caller.</param>
    /// <returns>A task representing the operation and its result.</returns>
    internal async Task DispatchAsync(CancellationToken cancellationToken)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var jobs = scope.ServiceProvider.GetRequiredService<IBackgroundJobClient>();
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken).ConfigureAwait(false);
        var now = timeProvider.GetUtcNow();
        var messages = await db.WelcomeEmailOutbox.FromSqlInterpolated(
            $"""
            SELECT * FROM "WelcomeEmailOutbox"
            WHERE "SentAt" IS NULL AND "JobId" IS NULL AND "Attempts" < 4 AND "NextAttemptAt" <= {now}
            ORDER BY "CreatedAt" LIMIT 20 FOR UPDATE SKIP LOCKED
            """)
            .ToListAsync(cancellationToken).ConfigureAwait(false);
        foreach (var message in messages)
        {
            using var trace = Jobs.JobTraceContext.Start("outbox.welcome", System.Diagnostics.ActivityKind.Producer, message.TraceParent, message.CorrelationId);
            message.JobId = jobs.Enqueue<WelcomeEmailJob>(job => job.SendAsync(message.Id, CancellationToken.None));
        }

        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Executes the welcome email worker.</summary>
    /// <param name="stoppingToken">The stopping token supplied to this operation.</param>
    /// <returns>A task representing the operation and its result.</returns>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!options.Value.Enabled)
        {
            return;
        }

        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(15), timeProvider);
        do
        {
            try
            {
                await this.DispatchAsync(stoppingToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                return;
            }
            catch (Exception exception)
            {
                ProcessingFailed(logger, exception);
            }
        }
        while (await timer.WaitForNextTickAsync(stoppingToken).ConfigureAwait(false));
    }

    [LoggerMessage(EventId = 3001, Level = LogLevel.Error, Message = "Welcome email outbox dispatch failed")]
    private static partial void ProcessingFailed(ILogger logger, Exception exception);
}
