using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using CulinaryBlog.Application.Auth;
using CulinaryBlog.Application.Media;
using CulinaryBlog.Infrastructure.Configuration;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using NSubstitute;

namespace CulinaryBlog.IntegrationTests;

public sealed class EndpointMatrixTests(AuthApiFactory factory) : IClassFixture<AuthApiFactory>, IDisposable
{
    private readonly WebApplicationFactory<Program> _app = factory.WithWebHostBuilder(builder =>
        builder.UseSetting("RateLimiting:GlobalPermitLimit", "10000").UseSetting("RateLimiting:AuthPermitLimit", "1000"));

    public void Dispose() => _app.Dispose();
    [Fact]
    public async Task ContractInventoryMatchesCurrentApi()
    {
        using var client = _app.CreateClient();
        var json = await client.GetStringAsync("/openapi/v1.json");
        using var contract = JsonDocument.Parse(json);
        using var matrix = JsonDocument.Parse(await File.ReadAllTextAsync(Path.Combine(AppContext.BaseDirectory, "endpoint-matrix.json")));
        foreach (var entry in matrix.RootElement.GetProperty("operations").EnumerateArray())
        {
            var operation = entry.GetProperty("operation").GetString()!.Split(' ', 2);
            Assert.True(contract.RootElement.GetProperty("paths").TryGetProperty(operation[1], out var path), operation[1]);
            Assert.True(path.TryGetProperty(operation[0].ToLowerInvariant(), out _), entry.ToString());
        }
        var output = Environment.GetEnvironmentVariable("CULINARY_CONTRACT_OUTPUT");
        if (!string.IsNullOrEmpty(output))
        {
            Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(output))!);
            await File.WriteAllTextAsync(output, json);
        }
    }

    [Theory]
    [InlineData("/api/v1")]
    [InlineData("/api/v1/release")]
    [InlineData("/api/v1/operations/maintenance")]
    public async Task SupportingReadsReturnDataAndRejectWrites(string path)
    {
        using var client = _app.CreateClient();
        using var happy = await client.GetAsync(path);
        Assert.Equal(HttpStatusCode.OK, happy.StatusCode);
        Assert.Contains("\"data\"", await happy.Content.ReadAsStringAsync(), StringComparison.Ordinal);
        using var invalid = await client.PostAsJsonAsync(path, new { });
        Assert.Equal(HttpStatusCode.MethodNotAllowed, invalid.StatusCode);
    }

    [Fact]
    public async Task SitemapReturnsStoredXmlOrNotFound()
    {
        using var client = _app.CreateClient();
        await using var scope = _app.Services.CreateAsyncScope();
        var storage = scope.ServiceProvider.GetRequiredService<IFileStorageService>();
        var key = scope.ServiceProvider.GetRequiredService<IOptions<SitemapOptions>>().Value.ObjectKey;
        const string xml = "<?xml version=\"1.0\"?><urlset xmlns=\"http://www.sitemaps.org/schemas/sitemap/0.9\"/>";
        using var missing = await client.GetAsync("/api/v1/sitemap.xml");
        Assert.Equal(HttpStatusCode.NotFound, missing.StatusCode);
        using var input = new MemoryStream(Encoding.UTF8.GetBytes(xml));
        await storage.UploadAsync(key, input, input.Length, "application/xml", default);
        using var happy = await client.GetAsync("/api/v1/sitemap.xml");
        Assert.Equal(HttpStatusCode.OK, happy.StatusCode);
        Assert.Equal("application/xml", happy.Content.Headers.ContentType?.MediaType);
        Assert.Equal(xml, await happy.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task GoogleAdapterValidatesInputAndReturnsTheServiceSession()
    {
        var auth = Substitute.For<IAuthService>();
        var session = new AuthSessionDto(
            "synthetic-access", "synthetic-refresh", DateTimeOffset.UtcNow.AddMinutes(1),
            new UserDto("synthetic-user", "adapter@example.test", "Adapter", null, null, ["Author"], true, true, DateTimeOffset.UtcNow));
        auth.LoginWithGoogleAsync("synthetic-provider-token", Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(session);
        await using var app = factory.WithWebHostBuilder(builder => builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<IAuthService>();
            services.AddSingleton(auth);
        }));
        using var client = app.CreateClient();
        using var invalid = await client.PostAsJsonAsync("/api/v1/auth/google", new { idToken = string.Empty });
        Assert.Equal(HttpStatusCode.UnprocessableEntity, invalid.StatusCode);
        Assert.Empty(auth.ReceivedCalls());
        using var happy = await client.PostAsJsonAsync("/api/v1/auth/google", new { idToken = "synthetic-provider-token" });
        Assert.Equal(HttpStatusCode.OK, happy.StatusCode);
        var payload = await happy.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(session.AccessToken, payload.GetProperty("data").GetProperty("accessToken").GetString());
    }

    [Fact]
    public async Task AccountEndpointsRejectDuplicateInvalidAndUnauthenticatedRequests()
    {
        using var client = _app.CreateClient();
        var account = new { email = $"matrix-{Guid.NewGuid():N}@example.test", displayName = "Matrix Author", password = "Synthetic#Matrix123" };
        using var created = await client.PostAsJsonAsync("/api/v1/auth/register", account);
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        using var duplicate = await client.PostAsJsonAsync("/api/v1/auth/register", account);
        Assert.Equal(HttpStatusCode.Conflict, duplicate.StatusCode);
        using var invalid = await client.PostAsJsonAsync("/api/v1/auth/logout", new { refreshToken = string.Empty });
        Assert.Equal(HttpStatusCode.UnprocessableEntity, invalid.StatusCode);
        using var privateProfile = await client.PatchAsJsonAsync("/api/v1/auth/me", new { displayName = "Unauthorized" });
        Assert.Equal(HttpStatusCode.Unauthorized, privateProfile.StatusCode);
    }

    [Theory]
    [InlineData("POST", "/api/v1/categories")]
    [InlineData("PUT", "/api/v1/categories/11111111-1111-1111-1111-111111111111")]
    [InlineData("DELETE", "/api/v1/categories/11111111-1111-1111-1111-111111111111")]
    public async Task CategoryWritesRequireAdmin(string method, string path)
    {
        using var client = _app.CreateClient();
        using var registered = await client.PostAsJsonAsync("/api/v1/auth/register", new
        {
            email = $"matrix-{Guid.NewGuid():N}@example.test",
            displayName = "Matrix Author",
            password = "Synthetic#Matrix123",
        });
        Assert.Equal(HttpStatusCode.Created, registered.StatusCode);
        var payload = await registered.Content.ReadFromJsonAsync<JsonElement>();
        var author = payload.GetProperty("data").GetProperty("accessToken").GetString();
        foreach (var (token, expected) in new[] { ((string?)null, HttpStatusCode.Unauthorized), (author, HttpStatusCode.Forbidden) })
        {
            using var request = new HttpRequestMessage(new HttpMethod(method), path);
            request.Headers.Authorization = token is null ? null : new AuthenticationHeaderValue("Bearer", token);
            request.Content = JsonContent.Create(new { name = "Test category", orderIndex = 0 });
            using var response = await client.SendAsync(request);
            Assert.Equal(expected, response.StatusCode);
        }
    }

    [Theory]
    [InlineData("PUT", "")]
    [InlineData("DELETE", "")]
    [InlineData("PATCH", "/publish")]
    [InlineData("PATCH", "/unpublish")]
    [InlineData("PATCH", "/archive")]
    [InlineData("PATCH", "/unarchive")]
    [InlineData("POST", "/ingredients")]
    [InlineData("PUT", "/ingredients/{child}")]
    [InlineData("DELETE", "/ingredients/{child}")]
    [InlineData("POST", "/steps")]
    [InlineData("PUT", "/steps/{child}")]
    [InlineData("DELETE", "/steps/{child}")]
    [InlineData("POST", "/images")]
    [InlineData("PATCH", "/images/{child}")]
    [InlineData("DELETE", "/images/{child}")]
    public async Task ProtectedRecipeMutationsRejectGuestsAndOtherAuthors(string method, string suffix)
    {
        using var client = _app.CreateClient();
        async Task<string> Register()
        {
            using var response = await client.PostAsJsonAsync("/api/v1/auth/register", new { email = $"matrix-{Guid.NewGuid():N}@example.test", displayName = "Matrix Author", password = "Synthetic#Matrix123" });
            response.EnsureSuccessStatusCode();
            return (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("data").GetProperty("accessToken").GetString()!;
        }
        var owner = await Register();
        var other = await Register();
        var categories = await client.GetFromJsonAsync<JsonElement>("/api/v1/categories");
        var body = new { title = "Matrix recipe", description = "Matrix description", categoryId = categories.GetProperty("data")[0].GetProperty("id").GetGuid(), prepTime = 1, cookTime = 0, servings = 1, difficulty = "easy", name = "Rice", quantity = 1, orderIndex = 0 };
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", owner);
        using var created = await client.PostAsJsonAsync("/api/v1/recipes", body);
        created.EnsureSuccessStatusCode();
        var id = (await created.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("data").GetProperty("id").GetGuid();
        foreach (var (token, expected) in new[] { ((string?)null, HttpStatusCode.Unauthorized), (other, HttpStatusCode.Forbidden) })
        {
            client.DefaultRequestHeaders.Authorization = token is null ? null : new AuthenticationHeaderValue("Bearer", token);
            using var request = new HttpRequestMessage(new HttpMethod(method), $"/api/v1/recipes/{id}" + suffix.Replace("{child}", Guid.NewGuid().ToString(), StringComparison.Ordinal));
            request.Headers.TryAddWithoutValidation("If-Match", "\"1\"");
            request.Content = suffix == "/images" ? new MultipartFormDataContent() : JsonContent.Create(body);
            using var response = await client.SendAsync(request);
            Assert.Equal(expected, response.StatusCode);
        }
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", owner);
        var stored = await client.GetFromJsonAsync<JsonElement>($"/api/v1/me/recipes/{id}");
        Assert.Equal(1, stored.GetProperty("data").GetProperty("version").GetInt64());
    }
}
