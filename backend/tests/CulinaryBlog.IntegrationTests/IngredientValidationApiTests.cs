using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using CulinaryBlog.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace CulinaryBlog.IntegrationTests;

public sealed class IngredientValidationApiTests(AuthApiFactory factory) : IClassFixture<AuthApiFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    [Theory]
    [InlineData(100, true)]
    [InlineData(101, false)]
    [InlineData(200, false)]
    public async Task IngredientNameUsesBusinessBoundWithoutPartialWrites(int length, bool valid)
    {
        using var registration = await _client.PostAsJsonAsync("/api/v1/auth/register", new { displayName = "Ingredient bounds", email = $"ingredient-{Guid.NewGuid():N}@example.test", password = "Valid#Password1" });
        registration.EnsureSuccessStatusCode();
        using var session = JsonDocument.Parse(await registration.Content.ReadAsStringAsync());
        using var categories = await _client.GetAsync("/api/v1/categories");
        categories.EnsureSuccessStatusCode();
        using var categoryDocument = JsonDocument.Parse(await categories.Content.ReadAsStringAsync());
        var categoryId = categoryDocument.RootElement.GetProperty("data")[0].GetProperty("id").GetGuid();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", session.RootElement.GetProperty("data").GetProperty("accessToken").GetString());
        using var created = await _client.PostAsJsonAsync("/api/v1/recipes", new { title = "Ingredient bounds", description = "Ingredient validation bounds", categoryId, prepTime = 10, cookTime = 5, servings = 2, difficulty = "easy" });
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        using var recipeDocument = JsonDocument.Parse(await created.Content.ReadAsStringAsync());
        var id = recipeDocument.RootElement.GetProperty("data").GetProperty("id").GetGuid();
        var name = new string('x', length);
        using var addRequest = new HttpRequestMessage(HttpMethod.Post, $"/api/v1/recipes/{id}/ingredients")
        {
            Content = JsonContent.Create(new { name }),
        };
        addRequest.Headers.TryAddWithoutValidation("If-Match", "\"1\"");
        using var add = await _client.SendAsync(addRequest);
        Assert.Equal(valid ? HttpStatusCode.Created : HttpStatusCode.UnprocessableEntity, add.StatusCode);
        await using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        Assert.Equal(valid ? 1 : 0, await db.RecipeIngredients.CountAsync(ingredient => ingredient.RecipeId == id));
        Assert.Equal(valid ? 2 : 1, await db.Recipes.Where(recipe => recipe.Id == id).Select(recipe => recipe.Version).SingleAsync());
        if (!valid)
        {
            using var seedRequest = new HttpRequestMessage(HttpMethod.Post, $"/api/v1/recipes/{id}/ingredients")
            {
                Content = JsonContent.Create(new { name = "Salt" }),
            };
            seedRequest.Headers.TryAddWithoutValidation("If-Match", "\"1\"");
            using var seed = await _client.SendAsync(seedRequest);
            Assert.Equal(HttpStatusCode.Created, seed.StatusCode);
        }
        var ingredient = await db.RecipeIngredients.AsNoTracking().SingleAsync(item => item.RecipeId == id);
        var updatedName = valid ? "y" + name[1..] : name;
        using var updateRequest = new HttpRequestMessage(HttpMethod.Put, $"/api/v1/recipes/{id}/ingredients/{ingredient.Id}")
        {
            Content = JsonContent.Create(new { name = updatedName }),
        };
        updateRequest.Headers.TryAddWithoutValidation("If-Match", "\"2\"");
        using var update = await _client.SendAsync(updateRequest);
        Assert.Equal(valid ? HttpStatusCode.OK : HttpStatusCode.UnprocessableEntity, update.StatusCode);
        Assert.Equal(valid ? updatedName : "Salt", await db.RecipeIngredients.Where(item => item.Id == ingredient.Id).Select(item => item.Name).SingleAsync());
        Assert.Equal(valid ? 3 : 2, await db.Recipes.Where(recipe => recipe.Id == id).Select(recipe => recipe.Version).SingleAsync());
        Assert.Equal(200, db.Model.FindEntityType(typeof(CulinaryBlog.Domain.Recipes.RecipeIngredient))!.FindProperty("Name")!.GetMaxLength());
    }

}
