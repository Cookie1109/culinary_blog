// Culinary Blog. Authorship is recorded in the repository history.

using System.Net.Mail;
using System.Text;
using CulinaryBlog.Application.Auth;
using CulinaryBlog.Infrastructure.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CulinaryBlog.Infrastructure.Email;

/// <summary>Represents the password reset email sender.</summary>
/// <param name="emailOptions">The email options supplied to this operation.</param>
/// <param name="siteOptions">The site options supplied to this operation.</param>
/// <param name="logger">The logger supplied to this operation.</param>
internal sealed partial class PasswordResetEmailSender(
    IOptions<EmailOptions> emailOptions,
    IOptions<SitemapOptions> siteOptions,
    ILogger<PasswordResetEmailSender> logger) : IPasswordResetEmailSender
{
    /// <summary>Gets a value indicating whether is enabled.</summary>
    public bool IsEnabled => emailOptions.Value.Enabled;

    /// <summary>Performs the send operation.</summary>
    /// <param name="email">The email supplied to this operation.</param>
    /// <param name="token">The token supplied to this operation.</param>
    /// <param name="cancellationToken">Cancels the operation when requested by the caller.</param>
    /// <returns>A task representing the operation and its result.</returns>
    public async Task SendAsync(string email, string token, CancellationToken cancellationToken)
    {
        var options = emailOptions.Value;

        // The fragment keeps the token out of HTTP access logs and referrer headers.
        var link = siteOptions.Value.PublicBaseUrl.TrimEnd('/') + "/reset-password#email=" +
            Uri.EscapeDataString(email) + "&token=" + Uri.EscapeDataString(token);
        using var message = new MailMessage
        {
            From = new MailAddress(options.FromAddress, options.FromName),
            Subject = "Đặt lại mật khẩu Culinary Blog",
            SubjectEncoding = Encoding.UTF8,
            BodyEncoding = Encoding.UTF8,
            Body = $"Bạn đã yêu cầu đặt lại mật khẩu Culinary Blog.\n\nMở liên kết sau trong vòng 30 phút:\n{link}\n\nNếu bạn không yêu cầu, hãy bỏ qua email này.",
        };
        message.To.Add(email);
        using var client = SmtpClientFactory.Create(options);
        try
        {
            await client.SendMailAsync(message, cancellationToken).ConfigureAwait(false);
            DeliveryAccepted(logger);
        }
        catch (SmtpException exception)
        {
            // Do not expose whether an address exists or log reset tokens.
            DeliveryFailed(logger, options.Host, options.Port, exception.StatusCode);
        }
    }

    [LoggerMessage(EventId = 3003, Level = LogLevel.Error, Message = "Password reset email delivery failed on {Host}:{Port} with SMTP status {StatusCode}; check SMTP configuration and availability")]
    private static partial void DeliveryFailed(ILogger logger, string host, int port, SmtpStatusCode statusCode);

    [LoggerMessage(EventId = 3004, Level = LogLevel.Information, Message = "Password reset email accepted by SMTP server")]
    private static partial void DeliveryAccepted(ILogger logger);
}
