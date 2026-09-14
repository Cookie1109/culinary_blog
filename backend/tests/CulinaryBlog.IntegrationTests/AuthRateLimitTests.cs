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

    private sealed class LowGlobalRateLimitApiFactory : WebApplicationFactory<Program>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Testing");
            builder.UseSetting("RateLimiting:GlobalPermitLimit", "1");
        }
    }
}
