using System.Net;
using System.Net.Mail;

namespace CulinaryBlog.Infrastructure.Email;

internal static class SmtpClientFactory
{
    public static SmtpClient Create(EmailOptions options)
    {
        if (!options.EnableSsl
            && !string.Equals(options.Host, "localhost", StringComparison.OrdinalIgnoreCase)
            && !string.Equals(options.Host, "mailhog", StringComparison.OrdinalIgnoreCase)
            && !(IPAddress.TryParse(options.Host, out var address) && IPAddress.IsLoopback(address)))
        {
            throw new InvalidOperationException("SMTP requires TLS except for loopback or MailHog capture.");
        }

        // The guarded plaintext branch is limited to local email capture.
#pragma warning disable S5332
        return new SmtpClient(options.Host, options.Port)
        {
            EnableSsl = options.EnableSsl,
            UseDefaultCredentials = false,
            Credentials = string.IsNullOrWhiteSpace(options.Username)
                ? null : new NetworkCredential(options.Username, options.Password),
        };
#pragma warning restore S5332
    }
}
