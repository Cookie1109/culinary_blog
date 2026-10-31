// Culinary Blog. Authorship is recorded in the repository history.

using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;

namespace CulinaryBlog.IntegrationTests;

/// <summary>Contains regression checks for api documentation tests.</summary>
public sealed class ApiDocumentationTests : IClassFixture<ApiFactory>
{
    private readonly HttpClient sharedClient;

    /// <summary>Initializes a new instance of the <see cref="ApiDocumentationTests"/> class.</summary>
    /// <param name="factory">The factory supplied to this operation.</param>
    public ApiDocumentationTests(ApiFactory factory)
    {
        this.sharedClient = factory.CreateClient();
    }

    /// <summary>Verifies that semantic validation and category bounds are documented.</summary>
    /// <returns>A task representing the operation and its result.</returns>
    [Fact]
    public async Task SemanticValidationAndCategoryBoundsAreDocumented()
    {
        using var document = JsonDocument.Parse(await this.sharedClient.GetStringAsync("/openapi/v1.json"));
        var root = document.RootElement;
        var name = root.GetProperty("components").GetProperty("schemas").GetProperty("CategoryWriteRequest")
            .GetProperty("properties").GetProperty("name");
        Assert.Equal(2, name.GetProperty("minLength").GetInt32());
        Assert.Equal(50, name.GetProperty("maxLength").GetInt32());
        Assert.True(root.GetProperty("paths").GetProperty("/api/v1/categories").GetProperty("post")
            .GetProperty("responses").TryGetProperty("422", out _));
    }

    /// <summary>Verifies that generated document includes versioned endpoints and response schemas.</summary>
    /// <returns>A task representing the operation and its result.</returns>
    [Fact]
    public async Task GeneratedDocumentIncludesVersionedEndpointsAndResponseSchemas()
    {
        using var document = JsonDocument.Parse(await this.sharedClient.GetStringAsync("/openapi/v1.json"));
        var root = document.RootElement;
        Assert.Equal("Culinary Blog API", root.GetProperty("info").GetProperty("title").GetString());
        Assert.Equal("1.0.0", root.GetProperty("info").GetProperty("version").GetString());
        var paths = root.GetProperty("paths");
        Assert.True(paths.TryGetProperty("/api/v1/auth/register", out var registration));
        Assert.True(paths.TryGetProperty("/api/v1/recipes/search", out _));
        Assert.True(paths.TryGetProperty("/api/v1/admin/audit-logs", out _));
        Assert.False(paths.TryGetProperty("/scalar", out _));
        Assert.False(paths.TryGetProperty("/openapi/v1.json", out _));
        var responseSchema = registration.GetProperty("post").GetProperty("responses")
            .GetProperty("201").GetProperty("content").GetProperty("application/json").GetProperty("schema");
        var envelope = ResolveSchema(root, responseSchema);
        var session = ResolveSchema(root, envelope.GetProperty("properties").GetProperty("data"));
        Assert.True(session.GetProperty("properties").TryGetProperty("accessToken", out _));
        Assert.True(session.GetProperty("properties").TryGetProperty("refreshToken", out _));
        var profile = ResolveSchema(root, paths.GetProperty("/api/v1/auth/me").GetProperty("patch")
            .GetProperty("requestBody").GetProperty("content").GetProperty("application/json").GetProperty("schema"));
        Assert.True(profile.GetProperty("properties").TryGetProperty("displayName", out _));
        Assert.False(profile.GetProperty("properties").TryGetProperty("updateDisplayName", out _));
    }

    /// <summary>Verifies that update schemas expose optional typed fields and create includes composition.</summary>
    /// <returns>A task representing the operation and its result.</returns>
    [Fact]
    public async Task UpdateSchemasExposeOptionalTypedFieldsAndCreateIncludesComposition()
    {
        var json = await this.sharedClient.GetStringAsync("/openapi/v1.json");
        await File.WriteAllTextAsync(Path.Combine(AppContext.BaseDirectory, "g04-openapi.json"), json);
        using var document = JsonDocument.Parse(json);
        var root = document.RootElement;
        var paths = root.GetProperty("paths");
        var update = ResolveSchema(root, paths.GetProperty("/api/v1/recipes/{id}").GetProperty("put")
            .GetProperty("requestBody").GetProperty("content").GetProperty("application/json").GetProperty("schema"));
        Assert.False(update.TryGetProperty("required", out var required) && required.EnumerateArray().Any());
        var title = ResolveSchema(root, update.GetProperty("properties").GetProperty("title"));
        Assert.False(title.TryGetProperty("properties", out var wrapper) && wrapper.TryGetProperty("isSpecified", out _));
        Assert.Contains("string", title.GetProperty("type").ToString(), StringComparison.Ordinal);
        var create = ResolveSchema(root, paths.GetProperty("/api/v1/recipes").GetProperty("post")
            .GetProperty("requestBody").GetProperty("content").GetProperty("application/json").GetProperty("schema"));
        Assert.True(create.GetProperty("properties").TryGetProperty("ingredients", out _));
        Assert.True(create.GetProperty("properties").TryGetProperty("steps", out _));
        Assert.Contains(
            paths.GetProperty("/api/v1/recipes").GetProperty("get").GetProperty("parameters")
                .EnumerateArray(),
            parameter => parameter.GetProperty("name").GetString() == "sortBy");
    }

    /// <summary>Verifies that bearer security is required only on protected operations.</summary>
    /// <returns>A task representing the operation and its result.</returns>
    [Fact]
    public async Task BearerSecurityIsRequiredOnlyOnProtectedOperations()
    {
        using var document = JsonDocument.Parse(await this.sharedClient.GetStringAsync("/openapi/v1.json"));
        var root = document.RootElement;
        var bearer = root.GetProperty("components").GetProperty("securitySchemes").GetProperty("Bearer");
        Assert.Equal("http", bearer.GetProperty("type").GetString());
        Assert.Equal("bearer", bearer.GetProperty("scheme").GetString());
        var paths = root.GetProperty("paths");
        Assert.True(paths.GetProperty("/api/v1/auth/me").GetProperty("get")
            .GetProperty("security")[0].TryGetProperty("Bearer", out _));
        Assert.False(paths.GetProperty("/api/v1/auth/login").GetProperty("post").TryGetProperty("security", out _));
        Assert.False(paths.GetProperty("/api/v1/recipes").GetProperty("get").TryGetProperty("security", out _));
    }

    /// <summary>Verifies that recipe mutations document concurrency and multipart upload.</summary>
    /// <returns>A task representing the operation and its result.</returns>
    [Fact]
    public async Task RecipeMutationsDocumentConcurrencyAndMultipartUpload()
    {
        using var document = JsonDocument.Parse(await this.sharedClient.GetStringAsync("/openapi/v1.json"));
        var paths = document.RootElement.GetProperty("paths");
        var update = paths.GetProperty("/api/v1/recipes/{id}").GetProperty("put");
        var etag = Assert.Single(
            update.GetProperty("parameters").EnumerateArray(),
            parameter => parameter.GetProperty("name").GetString() == "If-Match");
        Assert.Equal("header", etag.GetProperty("in").GetString());
        Assert.True(etag.GetProperty("required").GetBoolean());
        Assert.Equal("\"1\"", etag.GetProperty("example").GetString());
        var upload = paths.GetProperty("/api/v1/recipes/{id}/images").GetProperty("post");
        var form = upload.GetProperty("requestBody").GetProperty("content")
            .GetProperty("multipart/form-data").GetProperty("schema");
        Assert.Equal("file", form.GetProperty("required")[0].GetString());
        Assert.Equal("binary", form.GetProperty("properties").GetProperty("file").GetProperty("format").GetString());
        Assert.True(upload.GetProperty("responses").TryGetProperty("201", out _));
        Assert.True(upload.GetProperty("responses").GetProperty("4XX").GetProperty("content")
            .TryGetProperty("application/problem+json", out _));
        Assert.True(paths.GetProperty("/api/v1/recipes/{id}").GetProperty("delete")
            .GetProperty("responses").TryGetProperty("204", out _));
        Assert.True(update.GetProperty("responses").GetProperty("200").GetProperty("headers").TryGetProperty("ETag", out _));
        Assert.True(paths.GetProperty("/api/v1/recipes/{slug}").GetProperty("get").GetProperty("responses")
            .GetProperty("301").GetProperty("headers").TryGetProperty("Location", out _));
        var media = paths.GetProperty("/api/v1/media/{imageId}/{variant}").GetProperty("get")
            .GetProperty("responses").GetProperty("200").GetProperty("content");
        foreach (var contentType in new[] { "image/jpeg", "image/png", "image/webp", "image/avif" })
        {
            Assert.Equal("binary", media.GetProperty(contentType).GetProperty("schema").GetProperty("format").GetString());
        }
    }

    /// <summary>Verifies that documentation is available only in development and testing.</summary>
    /// <param name="environment">The environment supplied to this operation.</param>
    /// <param name="expected">The expected supplied to this operation.</param>
    /// <returns>A task representing the operation and its result.</returns>
    [Theory]
    [InlineData("Development", HttpStatusCode.OK)]
    [InlineData("Staging", HttpStatusCode.NotFound)]
    [InlineData("Production", HttpStatusCode.NotFound)]
    public async Task DocumentationIsAvailableOnlyInDevelopmentAndTesting(string environment, HttpStatusCode expected)
    {
        using var factory = new DocumentationApiFactory(environment);
        using var client = factory.CreateClient();
        using var specification = await client.GetAsync("/openapi/v1.json");
        using var reference = await client.GetAsync("/scalar/v1");
        Assert.Equal(expected, specification.StatusCode);
        Assert.Equal(expected, reference.StatusCode);
        if (expected == HttpStatusCode.OK)
        {
            var html = await reference.Content.ReadAsStringAsync();
            Assert.Contains("openapi/v1.json", html, StringComparison.Ordinal);
            Assert.Contains("Culinary Blog API v1", html, StringComparison.Ordinal);
        }
    }

    /// <summary>Verifies that scalar serves its java script assets locally.</summary>
    /// <param name="path">The path supplied to this operation.</param>
    /// <returns>A task representing the operation and its result.</returns>
    [Theory]
    [InlineData("/scalar/scalar.js")]
    [InlineData("/scalar/scalar.aspnetcore.js")]
    public async Task ScalarServesItsJavaScriptAssetsLocally(string path)
    {
        using var response = await this.sharedClient.GetAsync(path);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("javascript", response.Content.Headers.ContentType?.MediaType ?? string.Empty, StringComparison.Ordinal);
        Assert.True(response.Content.Headers.ContentLength > 0);
    }

    private static JsonElement ResolveSchema(JsonElement document, JsonElement schema)
    {
        if (schema.TryGetProperty("oneOf", out var alternatives))
        {
            schema = Assert.Single(alternatives.EnumerateArray(), candidate => candidate.TryGetProperty("$ref", out _));
        }

        if (!schema.TryGetProperty("$ref", out var reference))
        {
            return schema;
        }

        var name = reference.GetString()!.Split('/')[^1];
        return document.GetProperty("components").GetProperty("schemas").GetProperty(name);
    }

    private sealed class DocumentationApiFactory(string environment) : WebApplicationFactory<Program>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment(environment);
            builder.UseSetting("BackgroundJobs:Enabled", "false");
            builder.ConfigureTestServices(AdapterRateLimit.Disable);
            builder.UseSetting("ConnectionStrings:Redis", "127.0.0.1:1,abortConnect=false,connectTimeout=100,syncTimeout=100");
            builder.UseSetting("Database:ApplyMigrationsOnStartup", "false");
            builder.UseSetting("Seq:ServerUrl", string.Empty);
            builder.UseSetting("AllowedHosts", "localhost");
            builder.UseSetting("ConnectionStrings:Database", "Host=localhost;Database=culinary;Username=app;Password=test-documentation-password");
            builder.UseSetting("Jwt:SigningKey", "test-documentation-signing-key-at-least-32-characters");
            builder.UseSetting("ObjectStorage:AccessKey", "test-documentation-access-key");
            builder.UseSetting("ObjectStorage:SecretKey", "test-documentation-secret-key");
            builder.UseSetting("Sitemap:PublicBaseUrl", "https://localhost");
            builder.UseSetting("Cors:AllowedOrigins:0", "https://localhost");
            builder.UseSetting("Cors:AllowedOrigins:1", "https://localhost");
            builder.UseSetting("Release:CommitSha", new string('a', 40));
            builder.UseSetting("Release:ApiImageDigest", $"sha256:{new string('b', 64)}");
            builder.UseSetting("Release:WebImageDigest", $"sha256:{new string('c', 64)}");
        }
    }
}
