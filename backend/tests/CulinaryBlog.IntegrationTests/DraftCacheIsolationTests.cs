// Culinary Blog. Authorship is recorded in the repository history.

using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using CulinaryBlog.Application.Abstractions.Caching;
using Microsoft.Extensions.DependencyInjection;

namespace CulinaryBlog.IntegrationTests;

/// <summary>Checks that private draft writes preserve unrelated public cache entries.</summary>
/// <param name="factory">Provides the isolated application and its real dependencies.</param>
public sealed class DraftCacheIsolationTests(AuthApiFactory factory) : IClassFixture<AuthApiFactory>
{
    /// <summary>Verifies draft composition, edits and deletion without evicting public responses.</summary>
    /// <returns>A task representing the integration check.</returns>
    [Fact]
    public async Task DraftWritesPreservePublicGenerationAndCachedResponses()
    {
        using var author = factory.CreateClient();
        using var guest = factory.CreateClient();
        var suffix = Guid.NewGuid().ToString("N");
        using var registration = await author.PostAsJsonAsync("/api/v1/auth/register", new
        {
            email = $"draft-cache-{suffix}@example.test",
            password = "Synthetic-Only-123!",
            displayName = "Private draft author",
        });
        Assert.Equal(HttpStatusCode.Created, registration.StatusCode);
        var session = (await registration.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("data");
        author.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", session.GetProperty("accessToken").GetString());
        var categories = await guest.GetFromJsonAsync<JsonElement>("/api/v1/categories");
        var categoryId = categories.GetProperty("data")[0].GetProperty("id").GetGuid();
        var paths = new[] { $"/api/v1/categories?probe={suffix}", $"/api/v1/recipes?probe={suffix}" };
        foreach (var path in paths)
        {
            using var warm = await guest.GetAsync(path);
            Assert.Equal(HttpStatusCode.OK, warm.StatusCode);
        }

        var generation = await Generation();
        var body = new
        {
            title = "Private draft " + suffix,
            description = "Private cache isolation fixture.",
            categoryId,
            prepTime = 10,
            cookTime = 20,
            servings = 2,
            difficulty = "easy",
            ingredients = new[] { new { name = "Rice" } },
            steps = new[] { new { title = "Cook", description = "Cook rice." } },
        };
        using var created = await author.PostAsJsonAsync("/api/v1/recipes", body);
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        var recipe = (await created.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("data");
        var id = recipe.GetProperty("id").GetGuid();
        await PublicCacheIsUnchanged();
        using var hidden = await guest.GetAsync($"/api/v1/recipes/{recipe.GetProperty("slug").GetString()}");
        Assert.Equal(HttpStatusCode.NotFound, hidden.StatusCode);

        using var update = new HttpRequestMessage(HttpMethod.Put, $"/api/v1/recipes/{id}");
        update.Headers.IfMatch.Add(new EntityTagHeaderValue($"\"{recipe.GetProperty("version").GetInt32()}\""));
        update.Content = JsonContent.Create(new { title = "Edited draft " + suffix });
        using var updated = await author.SendAsync(update);
        Assert.Equal(HttpStatusCode.OK, updated.StatusCode);
        recipe = (await updated.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("data");
        await PublicCacheIsUnchanged();

        using var delete = new HttpRequestMessage(HttpMethod.Delete, $"/api/v1/recipes/{id}");
        delete.Headers.IfMatch.Add(new EntityTagHeaderValue($"\"{recipe.GetProperty("version").GetInt32()}\""));
        using var deleted = await author.SendAsync(delete);
        Assert.Equal(HttpStatusCode.NoContent, deleted.StatusCode);
        await PublicCacheIsUnchanged();

        async Task<long> Generation()
        {
            await using var scope = factory.Services.CreateAsyncScope();
            return await scope.ServiceProvider.GetRequiredService<IPublicCacheVersion>().GetAsync();
        }

        async Task PublicCacheIsUnchanged()
        {
            Assert.Equal(generation, await Generation());
            foreach (var path in paths)
            {
                using var hit = await guest.GetAsync(path);
                Assert.Equal(HttpStatusCode.OK, hit.StatusCode);
                Assert.NotNull(hit.Headers.Age);
            }
        }
    }
}
