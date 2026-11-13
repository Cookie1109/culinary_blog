// Culinary Blog. Authorship is recorded in the repository history.

using System.Net;
using System.Net.Mail;
using System.Text;
using CulinaryBlog.Infrastructure.Configuration;
using CulinaryBlog.Infrastructure.Persistence;
using Hangfire;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CulinaryBlog.Infrastructure.Email;

/// <summary>Represents the welcome email job.</summary>
/// <param name="dbContext">The db context supplied to this operation.</param>
/// <param name="options">The options supplied to this operation.</param>
/// <param name="siteOptions">The site options supplied to this operation.</param>
/// <param name="timeProvider">The time provider supplied to this operation.</param>
/// <param name="logger">The logger supplied to this operation.</param>
public sealed partial class WelcomeEmailJob(
    AppDbContext dbContext,
    IOptions<EmailOptions> options,
    IOptions<SitemapOptions> siteOptions,
    TimeProvider timeProvider,
    ILogger<WelcomeEmailJob> logger)
{
    /// <summary>Performs the send operation.</summary>
    /// <param name="outboxId">The outbox id supplied to this operation.</param>
    /// <param name="cancellationToken">Cancels the operation when requested by the caller.</param>
    /// <returns>A task representing the operation and its result.</returns>
    [AutomaticRetry(Attempts = 3, DelaysInSeconds = [60, 300, 1800])]
    public async Task SendAsync(Guid outboxId, CancellationToken cancellationToken)
    {
        if (!options.Value.Enabled)
        {
            return;
        }

        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken).ConfigureAwait(false);
        var message = await dbContext.WelcomeEmailOutbox.FromSqlInterpolated(
                $"SELECT * FROM \"WelcomeEmailOutbox\" WHERE \"Id\" = {outboxId} FOR UPDATE")
            .SingleOrDefaultAsync(cancellationToken).ConfigureAwait(false);
        if (message is null || message.SentAt is not null || message.Attempts >= 4)
        {
            return;
        }

        var email = options.Value;
        var name = WebUtility.HtmlEncode(message.DisplayName);
        var url = WebUtility.HtmlEncode(siteOptions.Value.PublicBaseUrl.TrimEnd('/') + "/");
        using var mail = new MailMessage
        {
            From = new MailAddress(email.FromAddress, email.FromName),
            Subject = "Chào mừng bạn đến với Culinary Blog",
            SubjectEncoding = Encoding.UTF8,
            BodyEncoding = Encoding.UTF8,
            Body = $"<html lang=\"vi\"><body><h1>Chào {name}!</h1><p>Chào mừng bạn đến với Culinary Blog.</p><p>Khám phá và chia sẻ công thức nấu ăn của bạn.</p><p><a href=\"{url}\">Truy cập Culinary Blog</a></p></body></html>",
            IsBodyHtml = true,
        };
        mail.To.Add(message.Recipient);
        using var smtp = SmtpClientFactory.Create(email);
        try
        {
            await smtp.SendMailAsync(mail, cancellationToken).ConfigureAwait(false);
            message.SentAt = timeProvider.GetUtcNow();
            message.LastError = null;
            await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
            DeliveryAccepted(logger, message.Id);
        }
        catch (Exception exception) when (exception is SmtpException or InvalidOperationException)
        {
            message.Attempts++;
            message.LastError = exception.Message[..Math.Min(exception.Message.Length, 1000)];
            var delay = message.Attempts switch { 1 => 1, 2 => 5, _ => 30 };
            message.NextAttemptAt = timeProvider.GetUtcNow().AddMinutes(delay);
            await dbContext.SaveChangesAsync(CancellationToken.None).ConfigureAwait(false);
            await transaction.CommitAsync(CancellationToken.None).ConfigureAwait(false);
            DeliveryFailed(logger, message.Id, message.Attempts);
            throw;
        }
    }

    [LoggerMessage(EventId = 3002, Level = LogLevel.Warning, Message = "Welcome email delivery failed for message {MessageId} on attempt {Attempt}")]
    private static partial void DeliveryFailed(ILogger logger, Guid messageId, int attempt);

    [LoggerMessage(EventId = 3005, Level = LogLevel.Information, Message = "Welcome email accepted by SMTP server for message {MessageId}")]
    private static partial void DeliveryAccepted(ILogger logger, Guid messageId);
}
