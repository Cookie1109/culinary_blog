// Culinary Blog. Authorship is recorded in the repository history.

using CulinaryBlog.Infrastructure.Email;

namespace CulinaryBlog.IntegrationTests;

/// <summary>Checks SMTP transport policy without opening network connections or sending mail.</summary>
public sealed class EmailTransportSecurityTests
{
    /// <summary>Verifies that plaintext SMTP is limited to the disposable local capture services.</summary>
    /// <param name="host">The SMTP host under test.</param>
    /// <param name="tls">Whether TLS is enabled.</param>
    /// <param name="allowed">Whether the connection policy permits constructing a client.</param>
    [Theory]
    [InlineData("smtp.example.test", false, false)]
    [InlineData("192.0.2.10", false, false)]
    [InlineData("smtp.example.test", true, true)]
    [InlineData("localhost", false, true)]
    [InlineData("127.0.0.1", false, true)]
    [InlineData("::1", false, true)]
    [InlineData("mailhog", false, true)]
    public void SmtpTransportRejectsExternalPlaintext(string host, bool tls, bool allowed)
    {
        var options = new EmailOptions { Enabled = true, Host = host, EnableSsl = tls };
        if (!allowed)
        {
            Assert.Throws<InvalidOperationException>(() => SmtpClientFactory.Create(options));
            return;
        }

        using var client = SmtpClientFactory.Create(options);
        Assert.Equal(tls, client.EnableSsl);
        Assert.Equal(host, client.Host);
        Assert.False(client.UseDefaultCredentials);
    }
}
