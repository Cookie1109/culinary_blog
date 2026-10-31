// Culinary Blog. Authorship is recorded in the repository history.

using System.Net;
using System.Net.Mail;

namespace CulinaryBlog.Infrastructure.Email;

/// <summary>Applies the shared TLS policy before creating either email sender's SMTP client.</summary>
internal static class SmtpClientFactory
{
    /// <summary>Creates an SMTP client after rejecting plaintext transport to external hosts.</summary>
    /// <param name="options">The validated SMTP connection and credential settings.</param>
    /// <returns>The client owned and disposed by the email sender.</returns>
    public static SmtpClient Create(EmailOptions options)
    {
        if (!IsTransportAllowed(options))
        {
            throw new InvalidOperationException("SMTP requires TLS except for loopback or MailHog capture.");
        }

        // Reviewed local-capture branch; production and external SMTP require TLS.
#pragma warning disable S5332
        return new SmtpClient(options.Host, options.Port)
        {
            EnableSsl = options.EnableSsl,
            UseDefaultCredentials = false,
            Credentials = string.IsNullOrWhiteSpace(options.Username) ? null : new NetworkCredential(options.Username, options.Password),
        };
#pragma warning restore S5332
    }

    /// <summary>Determines whether SMTP uses TLS or a local capture transport.</summary>
    /// <param name="options">The SMTP settings to check before a connection can be created.</param>
    /// <returns>True for TLS, loopback addresses, localhost and the local MailHog service.</returns>
    public static bool IsTransportAllowed(EmailOptions options) =>
        options.EnableSsl
        || string.Equals(options.Host, "localhost", StringComparison.OrdinalIgnoreCase)
        || string.Equals(options.Host, "mailhog", StringComparison.OrdinalIgnoreCase)
        || (IPAddress.TryParse(options.Host, out var address) && IPAddress.IsLoopback(address));
}
