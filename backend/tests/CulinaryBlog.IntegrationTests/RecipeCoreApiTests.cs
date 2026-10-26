using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
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
    public async Task ManagedSearchFiltersBeforePagingAndPreservesOwnershipAndAdminScope()
    {
        var owner = await RegisterAsync($"search-owner-{Guid.NewGuid():N}@example.com");
        var other = await RegisterAsync($"search-other-{Guid.NewGuid():N}@example.com");
        var categoryId = await GetCategoryIdAsync();
        var term = $"Needle{Guid.NewGuid():N}";
        SetToken(owner.AccessToken);
        foreach (var title in new[] { $"{term} Alpha", $"{term} Beta", "Other managed recipe" })
        {
            using var response = await _client.PostAsJsonAsync("/api/v1/recipes", RecipeBody(categoryId, title));
            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        }

        SetToken(other.AccessToken);
        using var otherCreate = await _client.PostAsJsonAsync("/api/v1/recipes", RecipeBody(categoryId, $"{term} Foreign"));
        Assert.Equal(HttpStatusCode.Created, otherCreate.StatusCode);

        SetToken(owner.AccessToken);
        var first = await _client.GetFromJsonAsync<RecipePage>($"/api/v1/me/recipes?q={term.ToUpperInvariant()}&pageSize=1");
        var second = await _client.GetFromJsonAsync<RecipePage>($"/api/v1/me/recipes?q=%20{term}%20&page=2&pageSize=1");
        Assert.Equal(2, first!.Meta.Total);
        Assert.Equal(2, first.Meta.TotalPages);
        Assert.Single(first.Data);
        Assert.Single(second!.Data);
        Assert.NotEqual(first.Data[0].Id, second.Data[0].Id);

        var categories = await _client.GetFromJsonAsync<DataEnvelope<CategoryResponse[]>>("/api/v1/categories");
        var categoryName = categories!.Data.Single(category => category.Id == categoryId).Name;
        var byCategory = await _client.GetFromJsonAsync<RecipePage>($"/api/v1/me/recipes?q={Uri.EscapeDataString(categoryName)}");
        Assert.Equal(3, byCategory!.Meta.Total);
        var literalWildcard = await _client.GetFromJsonAsync<RecipePage>("/api/v1/me/recipes?q=%25");
        Assert.Equal(0, literalWildcard!.Meta.Total);
        using var oversized = await _client.GetAsync($"/api/v1/me/recipes?q={new string('a', 101)}");
        Assert.Equal(HttpStatusCode.UnprocessableEntity, oversized.StatusCode);

        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var manager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var user = await manager.FindByEmailAsync(other.User.Email);
            Assert.True((await manager.AddToRoleAsync(user!, "Admin")).Succeeded);
        }

        var admin = await LoginAsync(other.User.Email);
        SetToken(admin.AccessToken);
        var all = await _client.GetFromJsonAsync<RecipePage>($"/api/v1/admin/recipes?q={term}&pageSize=1");
        Assert.Equal(3, all!.Meta.Total);
        var scoped = await _client.GetFromJsonAsync<RecipePage>($"/api/v1/admin/recipes?q={term}&authorId={owner.User.Id}");
        Assert.Equal(2, scoped!.Meta.Total);
        var byAuthor = await _client.GetFromJsonAsync<RecipePage>($"/api/v1/admin/recipes?q=recipe%20test%20user&authorId={owner.User.Id}");
        Assert.Equal(3, byAuthor!.Meta.Total);
    }

    [Fact]
    public async Task P7OwnershipRoleEscalationAndAdminOverrideAreEnforced()
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

        using var lowercaseStatusResponse = await _client.GetAsync("/api/v1/me/recipes?status=draft&page=1&pageSize=1");
        Assert.Equal(HttpStatusCode.OK, lowercaseStatusResponse.StatusCode);

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

        using var forbiddenAdminList = await _client.GetAsync("/api/v1/admin/recipes");
        Assert.Equal(HttpStatusCode.Forbidden, forbiddenAdminList.StatusCode);

        using var forbiddenCategoryCreate = await _client.PostAsJsonAsync(
            "/api/v1/categories",
            new { name = $"Escalation {Guid.NewGuid():N}", description = "Must be forbidden", orderIndex = 99 });
        Assert.Equal(HttpStatusCode.Forbidden, forbiddenCategoryCreate.StatusCode);

        await using (var adminScope = factory.Services.CreateAsyncScope())
        {
            var userManager = adminScope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var promotedUser = await userManager.FindByEmailAsync(other.User.Email);
            Assert.NotNull(promotedUser);
            Assert.True((await userManager.AddToRoleAsync(promotedUser, "Admin")).Succeeded);
        }

        using var staleAuthorTokenResponse = await _client.GetAsync("/api/v1/admin/recipes");
        Assert.Equal(HttpStatusCode.Forbidden, staleAuthorTokenResponse.StatusCode);

        var admin = await LoginAsync(other.User.Email);
        SetToken(admin.AccessToken);
        using var adminResponse = await _client.GetAsync($"/api/v1/admin/recipes/{created.Data.Id}");
        Assert.Equal(HttpStatusCode.OK, adminResponse.StatusCode);

        using var adminUpdate = CreatePut(
            created.Data.Id, 1, RecipeBody(categoryId, "Admin authorized update"));
        using var adminUpdateResponse = await _client.SendAsync(adminUpdate);
        Assert.Equal(HttpStatusCode.OK, adminUpdateResponse.StatusCode);
        Assert.Equal("\"2\"", adminUpdateResponse.Headers.ETag?.Tag);

        SetToken(owner.AccessToken);
        using var deleteRequest = new HttpRequestMessage(HttpMethod.Delete, $"/api/v1/recipes/{created.Data.Id}");
        deleteRequest.Headers.IfMatch.Add(new EntityTagHeaderValue("\"2\""));
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

    [Theory]
    [InlineData(0, false)]
    [InlineData(1, false)]
    [InlineData(2, true)]
    [InlineData(50, true)]
    [InlineData(51, false)]
    [InlineData(100, false)]
    [InlineData(-1, false)]
    public async Task CategoryNameBoundsApplyToCreateAndUpdateWithoutChangingStorage(int length, bool valid)
    {
        var email = $"category-name-{Guid.NewGuid():N}@example.com";
        await RegisterAsync(email);
        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var user = await users.FindByEmailAsync(email);
            Assert.NotNull(user);
            Assert.True((await users.AddToRoleAsync(user, "Admin")).Succeeded);
        }
        SetToken((await LoginAsync(email)).AccessToken);
        var name = length < 0 ? "<b>Món chay</b>" : length == 2 ? "QZ" : new string('x', length);
        await using var checkScope = factory.Services.CreateAsyncScope();
        var db = checkScope.ServiceProvider.GetRequiredService<AppDbContext>();
        var before = await db.Categories.CountAsync();
        using var create = await _client.PostAsJsonAsync("/api/v1/categories", new { name, orderIndex = 0 });
        Assert.Equal(valid ? HttpStatusCode.Created : HttpStatusCode.UnprocessableEntity, create.StatusCode);
        Assert.Equal(before + (valid ? 1 : 0), await db.Categories.CountAsync());

        var originalName = $"Category {Guid.NewGuid():N}";
        using var targetResponse = await _client.PostAsJsonAsync("/api/v1/categories", new { name = originalName, orderIndex = 0 });
        targetResponse.EnsureSuccessStatusCode();
        var target = (await targetResponse.Content.ReadFromJsonAsync<DataEnvelope<CategoryResponse>>())!.Data;
        var updatedName = valid ? "Y" + name[1..] : name;
        using var update = await _client.PutAsJsonAsync($"/api/v1/categories/{target.Id}", new { name = updatedName, orderIndex = 0 });
        Assert.Equal(valid ? HttpStatusCode.OK : HttpStatusCode.UnprocessableEntity, update.StatusCode);
        Assert.Equal(valid ? updatedName : originalName,
            await db.Categories.AsNoTracking().Where(item => item.Id == target.Id).Select(item => item.Name).SingleAsync());
        Assert.Equal(100, db.Model.FindEntityType(typeof(CulinaryBlog.Domain.Categories.Category))!
            .FindProperty(nameof(CulinaryBlog.Domain.Categories.Category.Name))!.GetMaxLength());
    }


    [Fact]
    public async Task MalformedRecipeJsonRemainsBadRequestAndDoesNotWrite()
    {
        var author = await RegisterAsync($"malformed-{Guid.NewGuid():N}@example.com");
        SetToken(author.AccessToken);
        await using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var before = await db.Recipes.CountAsync();
        using var body = new StringContent("{", System.Text.Encoding.UTF8, "application/json");
        using var response = await _client.PostAsync("/api/v1/recipes", body);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal(before, await db.Recipes.CountAsync());
    }

    [Fact]
    public async Task InvalidCategoryReferenceIs422WithoutPartialCreateOrUpdate()
    {
        var categoryId = await GetCategoryIdAsync();
        var author = await RegisterAsync($"foreign-category-{Guid.NewGuid():N}@example.com");
        SetToken(author.AccessToken);
        var missingCategory = Guid.NewGuid();
        await using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var count = await db.Recipes.CountAsync();
        using var invalidCreate = await _client.PostAsJsonAsync("/api/v1/recipes", RecipeBody(missingCategory, "Invalid category recipe"));
        Assert.Equal(HttpStatusCode.UnprocessableEntity, invalidCreate.StatusCode);
        Assert.Equal(count, await db.Recipes.CountAsync());
        using var problem = JsonDocument.Parse(await invalidCreate.Content.ReadAsStringAsync());
        Assert.Contains(problem.RootElement.GetProperty("errors").EnumerateObject(), field =>
            field.Name.Equals("CategoryId", StringComparison.OrdinalIgnoreCase));
        using var createdResponse = await _client.PostAsJsonAsync("/api/v1/recipes", RecipeBody(categoryId, "Valid category recipe"));
        createdResponse.EnsureSuccessStatusCode();
        var created = (await createdResponse.Content.ReadFromJsonAsync<DataEnvelope<RecipeResponse>>())!.Data;
        var before = await db.Recipes.AsNoTracking().Where(item => item.Id == created.Id)
            .Select(item => new { item.Title, item.CategoryId, item.Version }).SingleAsync();
        using var request = CreatePut(created.Id, 1, RecipeBody(missingCategory, "Rejected category update"));
        using var invalidUpdate = await _client.SendAsync(request);
        Assert.Equal(HttpStatusCode.UnprocessableEntity, invalidUpdate.StatusCode);
        var after = await db.Recipes.AsNoTracking().Where(item => item.Id == created.Id)
            .Select(item => new { item.Title, item.CategoryId, item.Version }).SingleAsync();
        Assert.Equal(before, after);
        using var missingResource = await _client.GetAsync($"/api/v1/categories/missing-{missingCategory:N}");
        Assert.Equal(HttpStatusCode.NotFound, missingResource.StatusCode);
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

    private sealed record AuthUser(Guid Id, string Email);

    private sealed record RecipeResponse(Guid Id, string Slug, string Status);

    private sealed record CategoryResponse(Guid Id, string Slug, string Name);

    private sealed record RecipePage(RecipeResponse[] Data, PageMeta Meta);

    private sealed record PageMeta(int Total, int TotalPages);

    private sealed record Problem(string Code, string? CurrentETag = null);
}
