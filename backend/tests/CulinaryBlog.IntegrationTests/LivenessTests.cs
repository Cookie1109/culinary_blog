using System.Net;
using System.Text.Json;
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

    [Theory]
    [InlineData("/health/live")]
    [InlineData("/health/ready")]
    [InlineData("/health")]
    public async Task PublicHealthEndpointsNeverExposeDependencyDetails(string path)
    {
        using var response = await _client.GetAsync(new Uri(path, UriKind.Relative));
        var payload = await response.Content.ReadAsStringAsync();
        using var document = JsonDocument.Parse(payload);

        var property = Assert.Single(document.RootElement.EnumerateObject());
        Assert.Equal("status", property.Name);
        Assert.DoesNotContain("postgresql", payload, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("redis", payload, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("minio", payload, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("password", payload, StringComparison.OrdinalIgnoreCase);
    }
}
