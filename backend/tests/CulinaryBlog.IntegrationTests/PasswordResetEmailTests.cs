using System.Text;
using System.Text.Json;
using CulinaryBlog.Application.Auth;
using CulinaryBlog.Infrastructure;
using DotNet.Testcontainers.Builders;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace CulinaryBlog.IntegrationTests;

public sealed class PasswordResetEmailTests
{
    [Fact]
    public async Task SmtpDeliveryContainsAnEncodedResetLinkOnTheConfiguredSite()
    {
        await using var mail = new ContainerBuilder("mailhog/mailhog:v1.0.1@sha256:8d76a3d4ffa32a3661311944007a415332c4bb855657f4f6c57996405c009bea")
            .WithPortBinding(1025, true)
            .WithPortBinding(8025, true)
            .WithWaitStrategy(Wait.ForUnixContainer().UntilHttpRequestIsSucceeded(request => request.ForPort(8025).ForPath("/api/v2/messages")))
            .Build();
        await mail.StartAsync();
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["ConnectionStrings:Database"] = "Host=localhost;Database=unused",
            ["ConnectionStrings:Redis"] = "localhost:6379",
            ["ObjectStorage:Endpoint"] = "http://localhost:9000",
            ["Jwt:SigningKey"] = "email-test-only-signing-key-not-used-for-tokens",
            ["BackgroundJobs:Enabled"] = "false",
            ["Email:Enabled"] = "true",
            ["Email:Host"] = mail.Hostname,
            ["Email:Port"] = mail.GetMappedPublicPort(1025).ToString(System.Globalization.CultureInfo.InvariantCulture),
            ["Sitemap:PublicBaseUrl"] = "https://recipes.example.test",
        }).Build();
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddInfrastructure(configuration);
        await using var provider = services.BuildServiceProvider();
        await using var scope = provider.CreateAsyncScope();
        var sender = scope.ServiceProvider.GetRequiredService<IPasswordResetEmailSender>();
        Assert.True(sender.IsEnabled);
        await sender.SendAsync("author+test@example.test", "token+/=", CancellationToken.None);

        using var client = new HttpClient();
        using var messages = JsonDocument.Parse(await client.GetStringAsync(
            new Uri($"http://{mail.Hostname}:{mail.GetMappedPublicPort(8025)}/api/v2/messages")));
        var content = Assert.Single(messages.RootElement.GetProperty("items").EnumerateArray()).GetProperty("Content");
        var body = Encoding.UTF8.GetString(Convert.FromBase64String(content.GetProperty("Body").GetString()!));
        Assert.Contains("https://recipes.example.test/reset-password#email=author%2Btest%40example.test&token=token%2B%2F%3D", body, StringComparison.Ordinal);
        Assert.Contains("30 phút", body, StringComparison.Ordinal);
    }
}
