using System.Net;
using System.Net.Mail;
using CulinaryBlog.Application.Auth;
using CulinaryBlog.Infrastructure.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CulinaryBlog.Infrastructure.Email;

internal sealed partial class PasswordResetEmailSender(
    IOptions<EmailOptions> emailOptions,
    IOptions<SitemapOptions> siteOptions,
    ILogger<PasswordResetEmailSender> logger) : IPasswordResetEmailSender
{
    public bool IsEnabled => emailOptions.Value.Enabled;

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
            Body = $"Bạn đã yêu cầu đặt lại mật khẩu Culinary Blog.\n\nMở liên kết sau trong vòng 30 phút:\n{link}\n\nNếu bạn không yêu cầu, hãy bỏ qua email này.",
        };
        message.To.Add(email);
        using var client = new SmtpClient(options.Host, options.Port)
        {
            EnableSsl = options.EnableSsl,
            UseDefaultCredentials = false,
            Credentials = string.IsNullOrWhiteSpace(options.Username)
                ? null : new NetworkCredential(options.Username, options.Password),
        };
        try
        {
            await client.SendMailAsync(message, cancellationToken).ConfigureAwait(false);
        }
        catch (SmtpException)
        {
            // Do not expose whether an address exists or log reset tokens.
            DeliveryFailed(logger);
        }
    }

    [LoggerMessage(EventId = 3003, Level = LogLevel.Error, Message = "Password reset email delivery failed; check SMTP configuration and availability")]
    private static partial void DeliveryFailed(ILogger logger);
}
