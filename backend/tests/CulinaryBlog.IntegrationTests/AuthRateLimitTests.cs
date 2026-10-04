using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace CulinaryBlog.IntegrationTests;

public sealed class AuthRateLimitTests : IClassFixture<ApiFactory>
{
    private readonly HttpClient _client;

    public AuthRateLimitTests(ApiFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task EleventhAuthRequestWithinAMinuteReturnsRetryAfter()
    {
        HttpResponseMessage? lastResponse = null;
        for (var requestNumber = 1; requestNumber <= 11; requestNumber++)
        {
            lastResponse?.Dispose();
            lastResponse = await _client.PostAsJsonAsync("/api/v1/auth/login", new { });
        }

        using (lastResponse)
        {
            Assert.NotNull(lastResponse);
            Assert.Equal(HttpStatusCode.TooManyRequests, lastResponse.StatusCode);
            Assert.Equal("60", Assert.Single(lastResponse.Headers.GetValues("Retry-After")));
        }
    }

    [Fact]
    public async Task HealthChecksDoNotConsumeTheGlobalRequestQuota()
    {
        using var factory = new LowGlobalRateLimitApiFactory();
        using var client = factory.CreateClient();

        for (var requestNumber = 1; requestNumber <= 3; requestNumber++)
        {
            using var response = await client.GetAsync("/health/live");
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }
    }

    [Fact]
    public async Task GeneralQuotaIsAppliedPerForwardedClientIp()
    {
        using var factory = new LowGlobalRateLimitApiFactory(trustForwardedHeaders: true);
        using var firstClient = factory.CreateClient();
        using var secondClient = factory.CreateClient();
        firstClient.DefaultRequestHeaders.Add("X-Forwarded-For", "203.0.113.10");
        secondClient.DefaultRequestHeaders.Add("X-Forwarded-For", "203.0.113.11");

        using var firstResponse = await firstClient.GetAsync("/api/v1");
        using var secondResponse = await secondClient.GetAsync("/api/v1");
        using var rateLimited = await firstClient.GetAsync("/api/v1");

        Assert.Equal(HttpStatusCode.OK, firstResponse.StatusCode);
        Assert.Equal(HttpStatusCode.OK, secondResponse.StatusCode);
        Assert.Equal(HttpStatusCode.TooManyRequests, rateLimited.StatusCode);
        Assert.Equal("60", Assert.Single(rateLimited.Headers.GetValues("Retry-After")));
    }

    private sealed class LowGlobalRateLimitApiFactory : WebApplicationFactory<Program>
    {
        private readonly bool _trustForwardedHeaders;

        public LowGlobalRateLimitApiFactory(bool trustForwardedHeaders = false)
        {
            _trustForwardedHeaders = trustForwardedHeaders;
        }

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Testing");
            builder.UseSetting("BackgroundJobs:Enabled", "false");
            builder.UseSetting("RateLimiting:GlobalPermitLimit", "1");
            builder.UseSetting("ReverseProxy:TrustForwardedHeaders", _trustForwardedHeaders.ToString());
        }
    }
}
