// Culinary Blog. Authorship is recorded in the repository history.

using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using CulinaryBlog.Application.Auth;
using CulinaryBlog.Application.Media;
using MediatR;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace CulinaryBlog.IntegrationTests;

/// <summary>Contains regression checks for endpoint matrix tests.</summary>
/// <param name="factory">The factory supplied to this operation.</param>
public sealed class EndpointMatrixTests(AuthApiFactory factory) : IClassFixture<AuthApiFactory>
{
    /// <summary>Verifies that other author cannot mutate any recipe component and no version changes.</summary>
    /// <returns>A task representing the operation and its result.</returns>
    [Fact]
    public async Task OtherAuthorCannotMutateAnyRecipeComponentAndNoVersionChanges()
    {
        using var client = factory.CreateClient();
        async Task<JsonElement> Register()
        {
            using var response = await client.PostAsJsonAsync("/api/v1/auth/register", new
            {
                displayName = "Matrix author",
                email = $"matrix-{Guid.NewGuid():N}@example.test",
                password = "Valid#Password1",
            });
            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
            return (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("data");
        }

        var owner = await Register();
        var other = await Register();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", owner.GetProperty("accessToken").GetString());
        var categories = await client.GetFromJsonAsync<JsonElement>("/api/v1/categories");
        using var created = await client.PostAsJsonAsync("/api/v1/recipes", new
        {
            title = "Owner matrix recipe",
            description = "Private recipe.",
            categoryId = categories.GetProperty("data")[0].GetProperty("id").GetGuid(),
            prepTime = 1,
            cookTime = 0,
            servings = 1,
            difficulty = "easy",
        });
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        var recipe = (await created.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("data");
        var recipePath = "/api/v1/recipes/" + recipe.GetProperty("id").GetGuid();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", other.GetProperty("accessToken").GetString());
        (string Method, string Suffix)[] operations = [
            ("PUT", string.Empty), ("DELETE", string.Empty), ("PATCH", "/publish"), ("PATCH", "/unpublish"), ("PATCH", "/archive"), ("PATCH", "/unarchive"),
            ("POST", "/ingredients"), ("PUT", "/ingredients/22222222-2222-2222-2222-222222222222"), ("DELETE", "/ingredients/22222222-2222-2222-2222-222222222222"),
            ("POST", "/steps"), ("PUT", "/steps/22222222-2222-2222-2222-222222222222"), ("DELETE", "/steps/22222222-2222-2222-2222-222222222222"),
            ("POST", "/images"), ("PATCH", "/images/22222222-2222-2222-2222-222222222222"), ("DELETE", "/images/22222222-2222-2222-2222-222222222222"),];
        foreach (var (method, suffix) in operations)
        {
            using var request = new HttpRequestMessage(new HttpMethod(method), recipePath + suffix);
            request.Headers.IfMatch.Add(new EntityTagHeaderValue("\"1\""));
            if (suffix == "/images")
            {
                var multipart = new MultipartFormDataContent();
                var file = new ByteArrayContent([1]);
                file.Headers.ContentType = new MediaTypeHeaderValue("image/png");
                multipart.Add(file, "file", "synthetic.png");
                request.Content = multipart;
            }
            else if (method != "DELETE")
            {
                request.Content = JsonContent.Create(new { name = "Salt", title = "Cook food", description = "Cook food.", altText = "Recipe", isPrimary = true });
            }

            using var response = await client.SendAsync(request);
            Assert.True(response.StatusCode == HttpStatusCode.Forbidden, $"{method} {suffix}: {response.StatusCode}");
        }

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", owner.GetProperty("accessToken").GetString());
        var current = await client.GetFromJsonAsync<JsonElement>("/api/v1/me/recipes/" + recipe.GetProperty("id").GetGuid());
        Assert.Equal(1, current.GetProperty("data").GetProperty("version").GetInt64());
        Assert.Empty(current.GetProperty("data").GetProperty("ingredients").EnumerateArray());
        Assert.Empty(current.GetProperty("data").GetProperty("steps").EnumerateArray());
        Assert.Empty(current.GetProperty("data").GetProperty("images").EnumerateArray());
    }

    /// <summary>Verifies that protected operations reject guests before reading or writing resources.</summary>
    /// <param name="method">The method supplied to this operation.</param>
    /// <param name="path">The path supplied to this operation.</param>
    /// <returns>A task representing the operation and its result.</returns>
    [Theory]
    [InlineData("GET", "/auth/me")]
    [InlineData("PATCH", "/auth/me")]
    [InlineData("POST", "/auth/change-password")]
    [InlineData("POST", "/categories")]
    [InlineData("PUT", "/categories/11111111-1111-1111-1111-111111111111")]
    [InlineData("DELETE", "/categories/11111111-1111-1111-1111-111111111111")]
    [InlineData("GET", "/admin/recipes")]
    [InlineData("GET", "/admin/recipes/11111111-1111-1111-1111-111111111111")]
    [InlineData("GET", "/admin/audit-logs")]
    [InlineData("POST", "/recipes")]
    [InlineData("PUT", "/recipes/11111111-1111-1111-1111-111111111111")]
    [InlineData("DELETE", "/recipes/11111111-1111-1111-1111-111111111111")]
    [InlineData("PATCH", "/recipes/11111111-1111-1111-1111-111111111111/publish")]
    [InlineData("PATCH", "/recipes/11111111-1111-1111-1111-111111111111/unpublish")]
    [InlineData("PATCH", "/recipes/11111111-1111-1111-1111-111111111111/archive")]
    [InlineData("PATCH", "/recipes/11111111-1111-1111-1111-111111111111/unarchive")]
    [InlineData("GET", "/me/recipes")]
    [InlineData("GET", "/me/recipes/11111111-1111-1111-1111-111111111111")]
    [InlineData("POST", "/recipes/11111111-1111-1111-1111-111111111111/ingredients")]
    [InlineData("PUT", "/recipes/11111111-1111-1111-1111-111111111111/ingredients/22222222-2222-2222-2222-222222222222")]
    [InlineData("DELETE", "/recipes/11111111-1111-1111-1111-111111111111/ingredients/22222222-2222-2222-2222-222222222222")]
    [InlineData("POST", "/recipes/11111111-1111-1111-1111-111111111111/steps")]
    [InlineData("PUT", "/recipes/11111111-1111-1111-1111-111111111111/steps/22222222-2222-2222-2222-222222222222")]
    [InlineData("DELETE", "/recipes/11111111-1111-1111-1111-111111111111/steps/22222222-2222-2222-2222-222222222222")]
    [InlineData("POST", "/recipes/11111111-1111-1111-1111-111111111111/images")]
    [InlineData("PATCH", "/recipes/11111111-1111-1111-1111-111111111111/images/22222222-2222-2222-2222-222222222222")]
    [InlineData("DELETE", "/recipes/11111111-1111-1111-1111-111111111111/images/22222222-2222-2222-2222-222222222222")]
    public async Task ProtectedOperationsRejectGuestsBeforeReadingOrWritingResources(string method, string path)
    {
        using var client = factory.CreateClient();
        using var request = new HttpRequestMessage(new HttpMethod(method), "/api/v1" + path);
        using var response = await client.SendAsync(request);
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    /// <summary>Verifies that supporting read endpoints return data and reject writes.</summary>
    /// <param name="path">The path supplied to this operation.</param>
    /// <returns>A task representing the operation and its result.</returns>
    [Theory]
    [InlineData("/api/v1")]
    [InlineData("/api/v1/release")]
    [InlineData("/api/v1/operations/maintenance")]
    public async Task SupportingReadEndpointsReturnDataAndRejectWrites(string path)
    {
        using var client = factory.CreateClient();
        using var happy = await client.GetAsync(path);
        Assert.Equal(HttpStatusCode.OK, happy.StatusCode);
        Assert.Contains("\"data\"", await happy.Content.ReadAsStringAsync(), StringComparison.Ordinal);
        using var error = await client.PostAsJsonAsync(path, new { });
        Assert.Equal(HttpStatusCode.MethodNotAllowed, error.StatusCode);
    }

    /// <summary>Verifies that sitemap endpoint returns stored xml or not found.</summary>
    /// <returns>A task representing the operation and its result.</returns>
    [Fact]
    public async Task SitemapEndpointReturnsStoredXmlOrNotFound()
    {
        using var client = factory.CreateClient();
        await using var scope = factory.Services.CreateAsyncScope();
        var storage = scope.ServiceProvider.GetRequiredService<IFileStorageService>();
        var key = scope.ServiceProvider.GetRequiredService<ISitemapLocation>().ObjectKey;
        const string xml = "<?xml version=\"1.0\"?><urlset xmlns=\"http://www.sitemaps.org/schemas/sitemap/0.9\"/>";
        using var input = new MemoryStream(Encoding.UTF8.GetBytes(xml));
        await storage.UploadAsync(key, input, input.Length, "application/xml", default);
        using var happy = await client.GetAsync("/api/v1/sitemap.xml");
        Assert.Equal(HttpStatusCode.OK, happy.StatusCode);
        Assert.Equal("application/xml", happy.Content.Headers.ContentType?.MediaType);
        Assert.Equal(xml, await happy.Content.ReadAsStringAsync());
        await storage.DeleteAsync(key, default);
        using var missing = await client.GetAsync("/api/v1/sitemap.xml");
        Assert.Equal(HttpStatusCode.NotFound, missing.StatusCode);
    }

    /// <summary>Verifies that google http adapter returns session and rejects invalid body without calling provider.</summary>
    /// <returns>A task representing the operation and its result.</returns>
    [Fact]
    public async Task GoogleHttpAdapterReturnsSessionAndRejectsInvalidBodyWithoutCallingProvider()
    {
        // Only the Application handler is substituted: this proves HTTP envelope/validation,
        // not Google signature/provider acceptance (owned by phase10).
        await using var custom = factory.WithWebHostBuilder(builder => builder.ConfigureTestServices(services =>
            services.Replace(ServiceDescriptor.Transient<IRequestHandler<GoogleLoginCommand, AuthSessionDto>, GoogleAdapterHandler>())));
        using var client = custom.CreateClient();
        using var happy = await client.PostAsJsonAsync("/api/v1/auth/google", new { idToken = "synthetic-provider-token" });
        Assert.Equal(HttpStatusCode.OK, happy.StatusCode);
        Assert.Contains("synthetic-access", await happy.Content.ReadAsStringAsync(), StringComparison.Ordinal);
        using var invalid = await client.PostAsJsonAsync("/api/v1/auth/google", new { idToken = string.Empty });
        Assert.Equal(HttpStatusCode.UnprocessableEntity, invalid.StatusCode);
    }

    private sealed class GoogleAdapterHandler : IRequestHandler<GoogleLoginCommand, AuthSessionDto>
    {
        public Task<AuthSessionDto> Handle(GoogleLoginCommand request, CancellationToken cancellationToken) =>
            Task.FromResult(new AuthSessionDto(
                "synthetic-access",
                "synthetic-refresh",
                DateTimeOffset.UtcNow.AddMinutes(1),
                new UserDto("synthetic-user", "adapter@g05.test", "Adapter", null, null, ["Author"], true, true, DateTimeOffset.UtcNow)));
    }
}
