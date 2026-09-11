using System.Net;
using CulinaryBlog.Api.Middleware;

namespace CulinaryBlog.IntegrationTests;

public sealed class LivenessTests : IClassFixture<ApiFactory>
{
    private readonly HttpClient _client;

    public LivenessTests(ApiFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task LivenessReturnsAggregateStatusAndCorrelationId()
    {
        using var response = await _client.GetAsync(new Uri("/health/live", UriKind.Relative));
        var payload = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("Healthy", payload, StringComparison.Ordinal);
        Assert.True(response.Headers.Contains(CorrelationIdMiddleware.HeaderName));
        Assert.DoesNotContain("entries", payload, StringComparison.OrdinalIgnoreCase);
    }
}
