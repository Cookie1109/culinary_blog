using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using CulinaryBlog.Api.Security;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace CulinaryBlog.IntegrationTests;

public sealed class SecurityHardeningTests
{
    [Fact]
    public void ProductionRejectsDevelopmentSecretsAndInsecurePublicUrls()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:Database"] = "Host=localhost;Password=local-development-only",
                ["Jwt:SigningKey"] = "local-development-only-change-this-key",
                ["ObjectStorage:AccessKey"] = "culinary-local",
                ["ObjectStorage:SecretKey"] = "local-development-only",
                ["AllowedHosts"] = "*",
                ["Sitemap:PublicBaseUrl"] = "http://localhost:8080",
            })
            .Build();

        var exception = Assert.Throws<InvalidOperationException>(
            () => ProductionConfigurationGuard.Validate(isProduction: true, configuration));

        Assert.Contains("Jwt:SigningKey", exception.Message, StringComparison.Ordinal);
        Assert.Contains("AllowedHosts", exception.Message, StringComparison.Ordinal);
        Assert.Contains("Sitemap:PublicBaseUrl", exception.Message, StringComparison.Ordinal);
        Assert.Contains("Release:CommitSha", exception.Message, StringComparison.Ordinal);
        Assert.Contains("Release:ApiImageDigest", exception.Message, StringComparison.Ordinal);
        Assert.Contains("Release:WebImageDigest", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void ProductionAcceptsExplicitSecretsHostsAndHttpsOrigins()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:Database"] = "Host=database;Database=culinary;Username=app;Password=a-deployment-secret",
                ["Jwt:SigningKey"] = "a-deployment-managed-signing-key-at-least-32-characters",
                ["ObjectStorage:AccessKey"] = "deployment-access-key",
                ["ObjectStorage:SecretKey"] = "deployment-secret-key",
                ["AllowedHosts"] = "culinary.example.test",
                ["Sitemap:PublicBaseUrl"] = "https://culinary.example.test",
                ["Cors:AllowedOrigins:0"] = "https://culinary.example.test",
                ["Release:CommitSha"] = new string('a', 40),
                ["Release:ApiImageDigest"] = $"sha256:{new string('b', 64)}",
                ["Release:WebImageDigest"] = $"sha256:{new string('c', 64)}",
            })
            .Build();

        ProductionConfigurationGuard.Validate(isProduction: true, configuration);
    }

    [Fact]
    public async Task ReleaseEndpointExposesImmutableDeploymentIdentity()
    {
        using var factory = new SecurityApiFactory();
        using var client = factory.CreateClient();

        var response = await client.GetFromJsonAsync<ReleaseEnvelope>("/api/v1/release");

        Assert.NotNull(response);
        Assert.Equal(new string('a', 40), response.Data.CommitSha);
        Assert.Equal($"sha256:{new string('b', 64)}", response.Data.ApiImageDigest);
        Assert.Equal($"sha256:{new string('c', 64)}", response.Data.WebImageDigest);
    }

    [Fact]
    public async Task CorsPreflightAllowsConfiguredOriginOnly()
    {
        using var factory = new SecurityApiFactory();
        using var client = factory.CreateClient();

        using var allowedRequest = CreatePreflight("https://app.example.test");
        using var allowedResponse = await client.SendAsync(allowedRequest);
        Assert.Equal(HttpStatusCode.NoContent, allowedResponse.StatusCode);
        Assert.Equal(
            "https://app.example.test",
            Assert.Single(allowedResponse.Headers.GetValues("Access-Control-Allow-Origin")));
        Assert.False(allowedResponse.Headers.Contains("Access-Control-Allow-Credentials"));

        using var blockedRequest = CreatePreflight("https://attacker.example");
        using var blockedResponse = await client.SendAsync(blockedRequest);
        Assert.False(blockedResponse.Headers.Contains("Access-Control-Allow-Origin"));
    }

    [Fact]
    public async Task AuthResponsesAreNeverCacheable()
    {
        using var factory = new SecurityApiFactory();
        using var client = factory.CreateClient();

        using var response = await client.PostAsync("/api/v1/auth/login", JsonContent.Create(new { }));

        Assert.True(response.Headers.CacheControl?.NoStore);
        Assert.True(response.Headers.CacheControl?.NoCache);
        Assert.Equal("no-cache", Assert.Single(response.Headers.Pragma).Name);
    }

    private static HttpRequestMessage CreatePreflight(string origin)
    {
        var request = new HttpRequestMessage(HttpMethod.Options, "/api/v1/auth/login");
        request.Headers.Add("Origin", origin);
        request.Headers.Add("Access-Control-Request-Method", "POST");
        request.Headers.Add("Access-Control-Request-Headers", "content-type");
        return request;
    }

    private sealed class SecurityApiFactory : WebApplicationFactory<Program>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Testing");
            builder.UseSetting("BackgroundJobs:Enabled", "false");
            builder.UseSetting("Cors:AllowedOrigins:0", "https://app.example.test");
            builder.UseSetting("Release:CommitSha", new string('a', 40));
            builder.UseSetting("Release:ApiImageDigest", $"sha256:{new string('b', 64)}");
            builder.UseSetting("Release:WebImageDigest", $"sha256:{new string('c', 64)}");
        }
    }

    private sealed record ReleaseEnvelope(ReleaseData Data);

    private sealed record ReleaseData(string CommitSha, string ApiImageDigest, string WebImageDigest);
}
