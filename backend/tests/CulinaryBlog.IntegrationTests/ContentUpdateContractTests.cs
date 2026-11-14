// Culinary Blog. Authorship is recorded in the repository history.

using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using CulinaryBlog.Application.Content;
using CulinaryBlog.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace CulinaryBlog.IntegrationTests;

/// <summary>Contains regression checks for content update contract tests.</summary>
/// <param name="factory">The factory supplied to this operation.</param>
public sealed class ContentUpdateContractTests(AuthApiFactory factory) : IClassFixture<AuthApiFactory>
{
    /// <summary>Verifies that partial write uses current owned nutrition after acquiring the recipe lock.</summary>
    /// <returns>A task representing the operation and its result.</returns>
    [Fact]
    public async Task PartialWriteUsesCurrentOwnedNutritionAfterAcquiringTheRecipeLock()
    {
        using var client = factory.CreateClient();
        var recipe = await CreateAsync(client);
        var id = recipe.GetProperty("id").GetGuid();
        await using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var tracked = await db.Recipes.SingleAsync(item => item.Id == id);
        Assert.Equal(1m, tracked.Nutrition!.Protein);
        await db.Database.ExecuteSqlInterpolatedAsync($"UPDATE \"Recipes\" SET \"Nutrition_Protein\" = 20, \"Version\" = 2 WHERE \"Id\" = {id}");
        var update = new RecipeUpdateRequest { CookTime = new InputField<int?>(0) };
        var result = await scope.ServiceProvider.GetRequiredService<IContentService>()
            .UpdateRecipeAsync(id, tracked.AuthorId, false, 2, update, CancellationToken.None);
        Assert.Equal(20m, result.Nutrition!.Protein);
        Assert.Equal(3, result.Version);
    }

    /// <summary>Verifies that partial updates preserve omitted fields clear nullable values and reject invalid values atomically.</summary>
    /// <returns>A task representing the operation and its result.</returns>
    [Fact]
    public async Task PartialUpdatesPreserveOmittedFieldsClearNullableValuesAndRejectInvalidValuesAtomically()
    {
        using var client = factory.CreateClient();
        var recipe = await CreateAsync(client);
        var path = $"/api/v1/recipes/{recipe.GetProperty("id").GetGuid()}";
        using var update = await Mutation(client, HttpMethod.Put, path, 1, new { cookTime = 0 });
        var changed = await Data(update);
        Assert.Equal(recipe.GetProperty("title").GetString(), changed.GetProperty("title").GetString());
        Assert.Equal(0, changed.GetProperty("cookTime").GetInt32());
        Assert.Equal(string.Empty, changed.GetProperty("instructions").GetString());
        using var nutrition = await Mutation(client, HttpMethod.Put, path, 2, new { nutrition = new { calories = (decimal?)null, protein = 2m } });
        var nutritional = (await Data(nutrition)).GetProperty("nutrition");
        Assert.Equal(JsonValueKind.Null, nutritional.GetProperty("calories").ValueKind);
        Assert.Equal(2m, nutritional.GetProperty("protein").GetDecimal());
        Assert.Equal(8m, nutritional.GetProperty("fat").GetDecimal());

        using var invalid = await Mutation(client, HttpMethod.Put, path, 3, new { title = (string?)null, description = "Must not be saved." });
        Assert.Equal(HttpStatusCode.UnprocessableEntity, invalid.StatusCode);
        using var noOp = await Mutation(client, HttpMethod.Put, path, 3, new { });
        var unchanged = await Data(noOp);
        Assert.Equal(3, unchanged.GetProperty("version").GetInt64());
        Assert.Equal(recipe.GetProperty("description").GetString(), unchanged.GetProperty("description").GetString());
        using var clear = await Mutation(client, HttpMethod.Put, path, 3, new { nutrition = (object?)null, instructions = (string?)null });
        var cleared = await Data(clear);
        Assert.Equal(JsonValueKind.Null, cleared.GetProperty("nutrition").ValueKind);
        Assert.Equal(string.Empty, cleared.GetProperty("instructions").GetString());

        var ingredientId = recipe.GetProperty("ingredients")[0].GetProperty("id").GetGuid();
        using var ingredient = await Mutation(client, HttpMethod.Put, $"{path}/ingredients/{ingredientId}", 4, new { quantity = (decimal?)null });
        var ingredientData = await Data(ingredient);
        Assert.Equal("Salt", ingredientData.GetProperty("name").GetString());
        Assert.Equal(JsonValueKind.Null, ingredientData.GetProperty("quantity").ValueKind);
        Assert.Equal("g", ingredientData.GetProperty("unit").GetString());
        var stepId = recipe.GetProperty("steps")[0].GetProperty("id").GetGuid();
        using var step = await Mutation(client, HttpMethod.Put, $"{path}/steps/{stepId}", 5, new { description = "Mix carefully." });
        var stepData = await Data(step);
        Assert.Equal("Prepare", stepData.GetProperty("title").GetString());
        Assert.Equal(5, stepData.GetProperty("timerMinutes").GetInt32());
        using var timer = await Mutation(client, HttpMethod.Put, $"{path}/steps/{stepId}", 6, new { timerMinutes = (int?)null });
        Assert.Equal(JsonValueKind.Null, (await Data(timer)).GetProperty("timerMinutes").ValueKind);
    }

    /// <summary>Verifies that child deletes advance version reject stale writes and keep steps continuous.</summary>
    /// <returns>A task representing the operation and its result.</returns>
    [Fact]
    public async Task ChildDeletesAdvanceVersionRejectStaleWritesAndKeepStepsContinuous()
    {
        using var client = factory.CreateClient();
        var recipe = await CreateAsync(client);
        var path = $"/api/v1/recipes/{recipe.GetProperty("id").GetGuid()}";
        var ingredientId = recipe.GetProperty("ingredients")[0].GetProperty("id").GetGuid();
        var stepId = recipe.GetProperty("steps")[0].GetProperty("id").GetGuid();
        using var ingredient = await Mutation(client, HttpMethod.Delete, $"{path}/ingredients/{ingredientId}", 1);
        Assert.Equal(HttpStatusCode.NoContent, ingredient.StatusCode);
        Assert.Equal("\"2\"", ingredient.Headers.ETag?.Tag);
        using var staleStep = await Mutation(client, HttpMethod.Delete, $"{path}/steps/{stepId}", 1);
        Assert.Equal(HttpStatusCode.Conflict, staleStep.StatusCode);
        using var step = await Mutation(client, HttpMethod.Delete, $"{path}/steps/{stepId}", 2);
        Assert.Equal(HttpStatusCode.NoContent, step.StatusCode);
        Assert.Equal("\"3\"", step.Headers.ETag?.Tag);
        using var missing = await Mutation(client, HttpMethod.Delete, $"{path}/ingredients/{ingredientId}", 3);
        Assert.Equal(HttpStatusCode.NotFound, missing.StatusCode);
        using var detail = await client.GetAsync($"/api/v1/me/recipes/{recipe.GetProperty("id").GetGuid()}");
        var current = await Data(detail);
        Assert.Empty(current.GetProperty("ingredients").EnumerateArray());
        Assert.Empty(current.GetProperty("steps").EnumerateArray());
        Assert.Equal(3, current.GetProperty("version").GetInt64());
    }

    /// <summary>Verifies that historical slugs redirect directly only while published and cannot be claimed by another recipe.</summary>
    /// <returns>A task representing the operation and its result.</returns>
    [Fact]
    public async Task HistoricalSlugsRedirectDirectlyOnlyWhilePublishedAndCannotBeClaimedByAnotherRecipe()
    {
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        var recipe = await CreateAsync(client);
        var id = recipe.GetProperty("id").GetGuid();
        var original = recipe.GetProperty("slug").GetString()!;
        var path = $"/api/v1/recipes/{id}";
        using var rename = await Mutation(client, HttpMethod.Put, path, 1, new { title = $"Second slug {Guid.NewGuid():N}" });
        var second = (await Data(rename)).GetProperty("slug").GetString();
        using var renameAgain = await Mutation(client, HttpMethod.Put, path, 2, new { title = $"Final slug {Guid.NewGuid():N}" });
        var final = (await Data(renameAgain)).GetProperty("slug").GetString();
        using var guest = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        using var hidden = await guest.GetAsync($"/api/v1/recipes/{original}");
        Assert.Equal(HttpStatusCode.NotFound, hidden.StatusCode);
        using var collision = await client.PostAsJsonAsync("/api/v1/recipes", new
        {
            title = recipe.GetProperty("title").GetString(),
            description = "Alias collision check.",
            categoryId = recipe.GetProperty("category").GetProperty("id").GetGuid(),
            prepTime = 1,
            cookTime = 0,
            servings = 1,
            difficulty = "easy",
        });
        Assert.Equal(original + "-2", (await Data(collision)).GetProperty("slug").GetString());
        using var publish = await Mutation(client, HttpMethod.Patch, path + "/publish", 3);
        Assert.Equal(HttpStatusCode.OK, publish.StatusCode);
        foreach (var alias in new[] { original, second })
        {
            using var response = await guest.GetAsync($"/api/v1/recipes/{alias}");
            Assert.Equal(HttpStatusCode.MovedPermanently, response.StatusCode);
            Assert.Equal($"/api/v1/recipes/{final}", response.Headers.Location?.OriginalString);
        }

        using var titleAfterPublish = await Mutation(client, HttpMethod.Put, path, 4, new { title = "Published title stays stable" });
        Assert.Equal(final, (await Data(titleAfterPublish)).GetProperty("slug").GetString());
        using var unpublish = await Mutation(client, HttpMethod.Patch, path + "/unpublish", 5);
        Assert.Equal(HttpStatusCode.OK, unpublish.StatusCode);
        using var afterUnpublish = await guest.GetAsync($"/api/v1/recipes/{original}");
        Assert.Equal(HttpStatusCode.NotFound, afterUnpublish.StatusCode);
    }

    /// <summary>Verifies that timers retain hours and seconds through create, update and reload.</summary>
    /// <param name="timerMinutes">The duration in minutes.</param>
    /// <returns>A task representing the operation and its result.</returns>
    [Theory]
    [InlineData(120d)]
    [InlineData(0.5d)]
    [InlineData(1d / 60)]
    public async Task StepTimersRetainHoursAndSeconds(double timerMinutes)
    {
        using var client = factory.CreateClient();
        var recipe = await CreateAsync(client);
        var recipeId = recipe.GetProperty("id").GetGuid();
        var path = $"/api/v1/recipes/{recipeId}/steps";
        using var created = await Mutation(client, HttpMethod.Post, path, 1, new
        {
            title = "Timed step",
            description = "Cook for the selected duration.",
            timerMinutes,
        });
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        var step = await Data(created);
        Assert.Equal(timerMinutes, step.GetProperty("timerMinutes").GetDouble());
        var stepId = step.GetProperty("id").GetGuid();
        using var loaded = await client.GetAsync($"/api/v1/me/recipes/{recipeId}");
        var savedStep = (await Data(loaded)).GetProperty("steps").EnumerateArray().Single(item => item.GetProperty("id").GetGuid() == stepId);
        Assert.Equal(timerMinutes, savedStep.GetProperty("timerMinutes").GetDouble());
        using var updated = await Mutation(client, HttpMethod.Put, $"{path}/{stepId}", 2, new { timerMinutes = 1d / 60 });
        Assert.Equal(1d / 60, (await Data(updated)).GetProperty("timerMinutes").GetDouble());
        using var reloaded = await client.GetAsync($"/api/v1/me/recipes/{recipeId}");
        var updatedStep = (await Data(reloaded)).GetProperty("steps").EnumerateArray().Single(item => item.GetProperty("id").GetGuid() == stepId);
        Assert.Equal(1d / 60, updatedStep.GetProperty("timerMinutes").GetDouble());
    }

    private static async Task<JsonElement> CreateAsync(HttpClient client)
    {
        using var registration = await client.PostAsJsonAsync("/api/v1/auth/register", new
        {
            email = $"update-{Guid.NewGuid():N}@example.test",
            displayName = "Update Author",
            password = "Valid#Password1",
        });
        var session = await Data(registration);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", session.GetProperty("accessToken").GetString());
        using var categories = await client.GetAsync("/api/v1/categories");
        var categoryId = (await Data(categories))[0].GetProperty("id").GetGuid();
        using var created = await client.PostAsJsonAsync("/api/v1/recipes", new
        {
            title = $"Original recipe {Guid.NewGuid():N}",
            description = "Keep the original description.",
            categoryId,
            prepTime = 10,
            cookTime = 20,
            servings = 2,
            difficulty = "easy",
            nutrition = new { calories = 100m, protein = 1m, fat = 8m },
            ingredients = new[] { new { name = "Salt", quantity = 1m, unit = "g" } },
            steps = new[] { new { title = "Prepare", description = "Prepare ingredients.", timerMinutes = 5 } },
        });
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        return await Data(created);
    }

    private static async Task<JsonElement> Data(HttpResponseMessage response)
    {
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("data");
    }

    private static async Task<HttpResponseMessage> Mutation(HttpClient client, HttpMethod method, string path, long version, object? body = null)
    {
        using var request = new HttpRequestMessage(method, path) { Content = body is null ? null : JsonContent.Create(body) };
        request.Headers.IfMatch.Add(new EntityTagHeaderValue($"\"{version}\""));
        return await client.SendAsync(request);
    }
}
