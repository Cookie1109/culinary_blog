using System.Net;
using System.Net.Http.Json;

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
}
