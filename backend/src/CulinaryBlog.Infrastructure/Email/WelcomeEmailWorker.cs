using System.Net;
using System.Net.Mail;
using CulinaryBlog.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CulinaryBlog.Infrastructure.Email;

internal sealed class WelcomeEmailWorker(
    IServiceScopeFactory scopeFactory,
    IOptions<EmailOptions> options,
    TimeProvider timeProvider,
    ILogger<WelcomeEmailWorker> logger) : BackgroundService
{
    private static readonly Action<ILogger, Exception?> ProcessingFailed =
        LoggerMessage.Define(LogLevel.Error, new EventId(3001, nameof(ProcessingFailed)), "Welcome email outbox processing failed");

    private static readonly Action<ILogger, Guid, int, Exception?> DeliveryFailed =
        LoggerMessage.Define<Guid, int>(
            LogLevel.Warning,
            new EventId(3002, nameof(DeliveryFailed)),
            "Welcome email delivery failed for outbox message {MessageId} on attempt {Attempt}");

    private static readonly TimeSpan[] RetryDelays =
    [
        TimeSpan.FromMinutes(1),
        TimeSpan.FromMinutes(5),
        TimeSpan.FromMinutes(30),
    ];

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!options.Value.Enabled)
        {
            return;
        }

        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(15), timeProvider);
        do
        {
            await ProcessBatchAsync(stoppingToken).ConfigureAwait(false);
        }
        while (await timer.WaitForNextTickAsync(stoppingToken).ConfigureAwait(false));
    }

    private async Task ProcessBatchAsync(CancellationToken cancellationToken)
    {
        try
        {
            await using var scope = scopeFactory.CreateAsyncScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var now = timeProvider.GetUtcNow();
            var messages = await dbContext.WelcomeEmailOutbox
                .Where(message => message.SentAt == null && message.Attempts < 4 && message.NextAttemptAt <= now)
                .OrderBy(message => message.CreatedAt)
                .Take(20)
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);

            foreach (var message in messages)
            {
                await SendAsync(message, cancellationToken).ConfigureAwait(false);
            }

            await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
        }
        catch (Exception exception)
        {
            ProcessingFailed(logger, exception);
        }
    }

    private async Task SendAsync(WelcomeEmailOutbox message, CancellationToken cancellationToken)
    {
        try
        {
            var emailOptions = options.Value;
            using var mailMessage = new MailMessage
            {
                From = new MailAddress(emailOptions.FromAddress, emailOptions.FromName),
                Subject = "Welcome to Culinary Blog",
                Body = $"Hello {message.DisplayName},\n\nWelcome to Culinary Blog!",
                IsBodyHtml = false,
            };
            mailMessage.To.Add(message.Recipient);

            using var smtpClient = new SmtpClient(emailOptions.Host, emailOptions.Port)
            {
                DeliveryMethod = SmtpDeliveryMethod.Network,
                UseDefaultCredentials = true,
            };
            await smtpClient.SendMailAsync(mailMessage, cancellationToken).ConfigureAwait(false);
            message.SentAt = timeProvider.GetUtcNow();
            message.LastError = null;
        }
        catch (Exception exception) when (exception is SmtpException or InvalidOperationException)
        {
            message.Attempts++;
            message.LastError = exception.Message[..Math.Min(exception.Message.Length, 1000)];
            var retryIndex = Math.Min(message.Attempts - 1, RetryDelays.Length - 1);
            message.NextAttemptAt = timeProvider.GetUtcNow().Add(RetryDelays[retryIndex]);
            DeliveryFailed(logger, message.Id, message.Attempts, exception);
        }
    }
}
