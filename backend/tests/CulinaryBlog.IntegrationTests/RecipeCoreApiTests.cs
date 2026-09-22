using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using CulinaryBlog.Infrastructure.Identity;
using CulinaryBlog.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace CulinaryBlog.IntegrationTests;

public sealed class RecipeCoreApiTests(AuthApiFactory factory) : IClassFixture<AuthApiFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task DraftOwnershipVisibilityAndSoftDeleteAreEnforced()
    {
        var owner = await RegisterAsync($"owner-{Guid.NewGuid():N}@example.com");
        var other = await RegisterAsync($"other-{Guid.NewGuid():N}@example.com");
        var categoryId = await GetCategoryIdAsync();
        SetToken(owner.AccessToken);

        using var createResponse = await _client.PostAsJsonAsync("/api/v1/recipes", RecipeBody(categoryId, "Private family recipe"));
        var created = await createResponse.Content.ReadFromJsonAsync<DataEnvelope<RecipeResponse>>();
        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);
        Assert.Equal("draft", created?.Data.Status);
        Assert.Equal("\"1\"", createResponse.Headers.ETag?.Tag);

        using var collidingResponse = await _client.PostAsJsonAsync(
            "/api/v1/recipes",
            RecipeBody(categoryId, "Private family recipe", includeNutrition: false));
        var colliding = await collidingResponse.Content.ReadFromJsonAsync<DataEnvelope<RecipeResponse>>();
        Assert.Equal(HttpStatusCode.Created, collidingResponse.StatusCode);
        Assert.Equal($"{created!.Data.Slug}-2", colliding?.Data.Slug);

        SetToken(null);
        using var publicResponse = await _client.GetAsync($"/api/v1/recipes/{created.Data.Slug}");
        Assert.Equal(HttpStatusCode.NotFound, publicResponse.StatusCode);

        SetToken(other.AccessToken);
        using var forbiddenResponse = await _client.GetAsync($"/api/v1/me/recipes/{created.Data.Id}");
        Assert.Equal(HttpStatusCode.Forbidden, forbiddenResponse.StatusCode);

        using var forbiddenUpdate = CreatePut(
            created.Data.Id, 1, RecipeBody(categoryId, "Another author's update"));
        using var forbiddenUpdateResponse = await _client.SendAsync(forbiddenUpdate);
        Assert.Equal(HttpStatusCode.Forbidden, forbiddenUpdateResponse.StatusCode);

        await using (var adminScope = factory.Services.CreateAsyncScope())
        {
            var userManager = adminScope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var promotedUser = await userManager.FindByEmailAsync(other.User.Email);
            Assert.NotNull(promotedUser);
            Assert.True((await userManager.AddToRoleAsync(promotedUser, "Admin")).Succeeded);
        }

        var admin = await LoginAsync(other.User.Email);
        SetToken(admin.AccessToken);
        using var adminResponse = await _client.GetAsync($"/api/v1/admin/recipes/{created.Data.Id}");
        Assert.Equal(HttpStatusCode.OK, adminResponse.StatusCode);

        SetToken(owner.AccessToken);
        using var deleteRequest = new HttpRequestMessage(HttpMethod.Delete, $"/api/v1/recipes/{created.Data.Id}");
        deleteRequest.Headers.IfMatch.Add(new EntityTagHeaderValue("\"1\""));
        using var deleteResponse = await _client.SendAsync(deleteRequest);
        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);

        using var afterDeleteResponse = await _client.GetAsync($"/api/v1/me/recipes/{created.Data.Id}");
        Assert.Equal(HttpStatusCode.NotFound, afterDeleteResponse.StatusCode);
        await using var scope = factory.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        Assert.True((await dbContext.Recipes.IgnoreQueryFilters().SingleAsync(recipe => recipe.Id == created.Data.Id)).IsDeleted);
    }

    [Fact]
    public async Task StaleRecipeUpdateReturnsConflictAndCurrentEtag()
    {
        var owner = await RegisterAsync($"concurrency-{Guid.NewGuid():N}@example.com");
        var categoryId = await GetCategoryIdAsync();
        SetToken(owner.AccessToken);
        using var createResponse = await _client.PostAsJsonAsync("/api/v1/recipes", RecipeBody(categoryId, "Concurrent recipe"));
        var created = (await createResponse.Content.ReadFromJsonAsync<DataEnvelope<RecipeResponse>>())!.Data;

        using var missingPrecondition = await _client.PutAsJsonAsync(
            $"/api/v1/recipes/{created.Id}",
            RecipeBody(categoryId, "Update without precondition"));
        var preconditionProblem = await missingPrecondition.Content.ReadFromJsonAsync<Problem>();
        Assert.Equal(HttpStatusCode.BadRequest, missingPrecondition.StatusCode);
        Assert.Equal("PRECONDITION_REQUIRED", preconditionProblem?.Code);

        using var firstUpdate = CreatePut(created.Id, 1, RecipeBody(categoryId, "First concurrent update"));
        using var secondUpdate = CreatePut(created.Id, 1, RecipeBody(categoryId, "Second concurrent update"));
        var responses = await Task.WhenAll(_client.SendAsync(firstUpdate), _client.SendAsync(secondUpdate));
        using var acceptedResponse = responses.Single(response => response.StatusCode == HttpStatusCode.OK);
        using var staleResponse = responses.Single(response => response.StatusCode == HttpStatusCode.Conflict);
        Assert.Equal("\"2\"", acceptedResponse.Headers.ETag?.Tag);
        var problem = await staleResponse.Content.ReadFromJsonAsync<Problem>();
        Assert.Equal("RECIPE_CONCURRENCY_CONFLICT", problem?.Code);
        Assert.Equal("\"2\"", problem?.CurrentETag);
        Assert.True(
            staleResponse.Headers.TryGetValues("ETag", out var etagValues),
            $"Response headers: {staleResponse.Headers}");
        Assert.Equal("\"2\"", etagValues.Single());
    }

    [Fact]
    public async Task AdminCategorySlugIsStableAndDeleteWithRecipeConflicts()
    {
        var adminEmail = $"admin-{Guid.NewGuid():N}@example.com";
        await RegisterAsync(adminEmail);
        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var seededAdmin = await userManager.FindByEmailAsync(adminEmail);
            Assert.NotNull(seededAdmin);
            Assert.True((await userManager.AddToRoleAsync(seededAdmin, "Admin")).Succeeded);
        }

        var admin = await LoginAsync(adminEmail);
        SetToken(admin.AccessToken);
        var raceName = $"Danh mục race {Guid.NewGuid():N}";
        var raceResponses = await Task.WhenAll(
            _client.PostAsJsonAsync(
                "/api/v1/categories",
                new { name = raceName, description = "First contender", orderIndex = 30 }),
            _client.PostAsJsonAsync(
                "/api/v1/categories",
                new { name = raceName, description = "Second contender", orderIndex = 31 }));
        Assert.Single(raceResponses, response => response.StatusCode == HttpStatusCode.Created);
        var rejectedRace = Assert.Single(raceResponses, response => response.StatusCode == HttpStatusCode.Conflict);
        var raceProblem = await rejectedRace.Content.ReadFromJsonAsync<Problem>();
        Assert.Equal("CATEGORY_NAME_EXISTS", raceProblem?.Code);
        foreach (var response in raceResponses)
        {
            response.Dispose();
        }

        using var createCategoryResponse = await _client.PostAsJsonAsync(
            "/api/v1/categories",
            new { name = "Đồ uống thử nghiệm", description = "Test", orderIndex = 20 });
        var category = (await createCategoryResponse.Content.ReadFromJsonAsync<DataEnvelope<CategoryResponse>>())!.Data;
        Assert.Equal(HttpStatusCode.Created, createCategoryResponse.StatusCode);

        using var updateCategoryResponse = await _client.PutAsJsonAsync(
            $"/api/v1/categories/{category.Id}",
            new { name = "Nước uống thử nghiệm", description = "Updated", orderIndex = 21 });
        var updated = (await updateCategoryResponse.Content.ReadFromJsonAsync<DataEnvelope<CategoryResponse>>())!.Data;
        Assert.Equal(category.Slug, updated.Slug);

        using var recipeResponse = await _client.PostAsJsonAsync(
            "/api/v1/recipes",
            RecipeBody(category.Id, "Admin owned recipe"));
        Assert.Equal(HttpStatusCode.Created, recipeResponse.StatusCode);

        using var deleteCategoryResponse = await _client.DeleteAsync($"/api/v1/categories/{category.Id}");
        var problem = await deleteCategoryResponse.Content.ReadFromJsonAsync<Problem>();
        Assert.Equal(HttpStatusCode.Conflict, deleteCategoryResponse.StatusCode);
        Assert.Equal("CATEGORY_DELETE_HAS_RECIPES", problem?.Code);
    }

    private async Task<Guid> GetCategoryIdAsync()
    {
        SetToken(null);
        var response = await _client.GetFromJsonAsync<DataEnvelope<CategoryResponse[]>>("/api/v1/categories");
        return response!.Data[0].Id;
    }

    private async Task<AuthSession> RegisterAsync(string email)
    {
        SetToken(null);
        using var response = await _client.PostAsJsonAsync(
            "/api/v1/auth/register",
            new { displayName = "Recipe Test User", email, password = "Valid#Password1" });
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<DataEnvelope<AuthSession>>())!.Data;
    }

    private async Task<AuthSession> LoginAsync(string email)
    {
        SetToken(null);
        using var response = await _client.PostAsJsonAsync(
            "/api/v1/auth/login",
            new { email, password = "Valid#Password1" });
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<DataEnvelope<AuthSession>>())!.Data;
    }

    private void SetToken(string? token) => _client.DefaultRequestHeaders.Authorization = token is null
        ? null
        : new AuthenticationHeaderValue("Bearer", token);

    private static HttpRequestMessage CreatePut(Guid id, long version, object body)
    {
        var request = new HttpRequestMessage(HttpMethod.Put, $"/api/v1/recipes/{id}")
        {
            Content = JsonContent.Create(body),
        };
        request.Headers.IfMatch.Add(new EntityTagHeaderValue($"\"{version}\""));
        return request;
    }

    private static object RecipeBody(Guid categoryId, string title, bool includeNutrition = true) => new
    {
        title,
        description = "A recipe used by the integration test suite.",
        categoryId,
        prepTime = 15,
        cookTime = 20,
        servings = 4,
        difficulty = "medium",
        instructions = "Keep this summary private while the recipe is a draft.",
        nutrition = includeNutrition
            ? new { calories = 250, protein = 10, carbohydrates = 30, fat = 8, fiber = 3, sodium = 120 }
            : null,
    };

    private sealed record DataEnvelope<T>(T Data);

    private sealed record AuthSession(string AccessToken, AuthUser User);

    private sealed record AuthUser(string Email);

    private sealed record RecipeResponse(Guid Id, string Slug, string Status);

    private sealed record CategoryResponse(Guid Id, string Slug);

    private sealed record Problem(string Code, string? CurrentETag = null);
}
