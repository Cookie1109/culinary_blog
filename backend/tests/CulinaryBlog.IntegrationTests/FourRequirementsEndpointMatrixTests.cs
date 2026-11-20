// Culinary Blog. Authorship is recorded in the repository history.

using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace CulinaryBlog.IntegrationTests;

/// <summary>Contains regression checks for four requirements endpoint matrix tests.</summary>
/// <param name="factory">The factory supplied to this operation.</param>
public sealed class FourRequirementsEndpointMatrixTests(AuthApiFactory factory) : IClassFixture<AuthApiFactory>, IDisposable
{
    private readonly WebApplicationFactory<Program> app = factory.WithWebHostBuilder(builder =>
        builder.UseSetting("RateLimiting:GlobalPermitLimit", "10000").UseSetting("RateLimiting:AuthPermitLimit", "1000"));

    /// <summary>Verifies that dispose.</summary>
    public void Dispose() => this.app.Dispose();

    /// <summary>Verifies that contract inventory matches current api.</summary>
    /// <returns>A task representing the operation and its result.</returns>
    [Fact]
    public async Task ContractInventoryMatchesCurrentApi()
    {
        using var client = this.app.CreateClient();
        string json = await client.GetStringAsync("/openapi/v1.json");
        using var contract = JsonDocument.Parse(json);
        using var matrix = JsonDocument.Parse(await File.ReadAllTextAsync(Path.Combine(AppContext.BaseDirectory, "endpoint-matrix.json")));
        foreach (var entry in matrix.RootElement.GetProperty("operations").EnumerateArray())
        {
            string[] operation = entry.GetProperty("operation").GetString()!.Split(' ', 2);
            Assert.True(contract.RootElement.GetProperty("paths").TryGetProperty(operation[1], out var path), operation[1]);
            Assert.True(path.TryGetProperty(operation[0].ToLowerInvariant(), out _), entry.ToString());
        }

        string? output = Environment.GetEnvironmentVariable("CULINARY_CONTRACT_OUTPUT");
        if (!string.IsNullOrEmpty(output))
        {
            Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(output))!);
            await File.WriteAllTextAsync(output, json);
        }
    }

    /// <summary>Verifies that protected recipe mutations reject guests and other authors.</summary>
    /// <param name="method">The method supplied to this operation.</param>
    /// <param name="suffix">The suffix supplied to this operation.</param>
    /// <returns>A task representing the operation and its result.</returns>
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
        using var client = this.app.CreateClient();
        async Task<string> Register()
        {
            using var response = await client.PostAsJsonAsync("/api/v1/auth/register", new { email = $"matrix-{Guid.NewGuid():N}@example.test", displayName = "Matrix Author", password = "Synthetic#Matrix123" });
            response.EnsureSuccessStatusCode();
            return (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("data").GetProperty("accessToken").GetString()!;
        }

        string owner = await Register();
        string other = await Register();
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
