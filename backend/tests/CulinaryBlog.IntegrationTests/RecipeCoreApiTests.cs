// Culinary Blog. Authorship is recorded in the repository history.

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

/// <summary>Contains regression checks for recipe core api tests.</summary>
/// <param name="factory">The factory supplied to this operation.</param>
public sealed class RecipeCoreApiTests(AuthApiFactory factory) : IClassFixture<AuthApiFactory>
{
    private readonly HttpClient client = factory.CreateClient();

    /// <summary>Verifies that create persists nested composition and rejects invalid children without partial writes.</summary>
    /// <returns>A task representing the operation and its result.</returns>
    [Fact]
    public async Task CreatePersistsNestedCompositionAndRejectsInvalidChildrenWithoutPartialWrites()
    {
        var owner = await this.RegisterAsync($"nested-{Guid.NewGuid():N}@example.test");
        var categoryId = await this.GetCategoryIdAsync();
        this.SetToken(owner.AccessToken);
        var title = $"Nested recipe {Guid.NewGuid():N}";
        object Body(string ingredientName) => new
        {
            title,
            description = "Nested composition must be saved together.",
            categoryId,
            prepTime = 10,
            cookTime = 0,
            servings = 2,
            difficulty = "easy",
            ingredients = new[] { new { name = ingredientName, quantity = (decimal?)null, unit = (string?)null, notes = "To taste", orderIndex = 0 } },
            steps = new[]
            {
                new { title = "Prepare", description = "Prepare the ingredients.", stepNumber = 8 },
                new { title = "Serve", description = "Serve without cooking.", stepNumber = 8 },
            },
        };

        using var invalid = await this.client.PostAsJsonAsync("/api/v1/recipes", Body(new string('a', 201)));
        Assert.Equal(HttpStatusCode.UnprocessableEntity, invalid.StatusCode);
        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            Assert.False(await db.Recipes.AnyAsync(recipe => recipe.Title == title));
        }

        using var created = await this.client.PostAsJsonAsync("/api/v1/recipes", Body("Salt"));
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        using var document = System.Text.Json.JsonDocument.Parse(await created.Content.ReadAsStringAsync());
        var data = document.RootElement.GetProperty("data");
        Assert.Equal("draft", data.GetProperty("status").GetString());
        Assert.Equal("\"1\"", created.Headers.ETag?.Tag);
        var id = data.GetProperty("id").GetGuid();
        using var read = await this.client.GetAsync($"/api/v1/me/recipes/{id}");
        using var persisted = System.Text.Json.JsonDocument.Parse(await read.Content.ReadAsStringAsync());
        var detail = persisted.RootElement.GetProperty("data");
        Assert.Equal("Salt", Assert.Single(detail.GetProperty("ingredients").EnumerateArray()).GetProperty("name").GetString());
        Assert.Collection(
            detail.GetProperty("steps").EnumerateArray(),
            step => Assert.Equal(1, step.GetProperty("stepNumber").GetInt32()),
            step => Assert.Equal(2, step.GetProperty("stepNumber").GetInt32()));
    }

    /// <summary>Verifies that managed search filters before paging and preserves ownership and admin scope.</summary>
    /// <returns>A task representing the operation and its result.</returns>
    [Fact]
    public async Task ManagedSearchFiltersBeforePagingAndPreservesOwnershipAndAdminScope()
    {
        var owner = await this.RegisterAsync($"search-owner-{Guid.NewGuid():N}@example.com");
        var other = await this.RegisterAsync($"search-other-{Guid.NewGuid():N}@example.com");
        var categoryId = await this.GetCategoryIdAsync();
        var term = $"Needle{Guid.NewGuid():N}";
        this.SetToken(owner.AccessToken);
        foreach (var title in new[] { $"{term} Alpha", $"{term} Beta", "Other managed recipe" })
        {
            using var response = await this.client.PostAsJsonAsync("/api/v1/recipes", RecipeBody(categoryId, title));
            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        }

        this.SetToken(other.AccessToken);
        using var otherCreate = await this.client.PostAsJsonAsync("/api/v1/recipes", RecipeBody(categoryId, $"{term} Foreign"));
        Assert.Equal(HttpStatusCode.Created, otherCreate.StatusCode);

        this.SetToken(owner.AccessToken);
        var first = await this.client.GetFromJsonAsync<RecipePage>($"/api/v1/me/recipes?q={term.ToUpperInvariant()}&pageSize=1");
        var second = await this.client.GetFromJsonAsync<RecipePage>($"/api/v1/me/recipes?q=%20{term}%20&page=2&pageSize=1");
        Assert.Equal(2, first!.Meta.Total);
        Assert.Equal(2, first.Meta.TotalPages);
        Assert.Single(first.Data);
        Assert.Single(second!.Data);
        Assert.NotEqual(first.Data[0].Id, second.Data[0].Id);

        var categories = await this.client.GetFromJsonAsync<DataEnvelope<CategoryResponse[]>>("/api/v1/categories");
        var categoryName = categories!.Data.Single(category => category.Id == categoryId).Name;
        var byCategory = await this.client.GetFromJsonAsync<RecipePage>($"/api/v1/me/recipes?q={Uri.EscapeDataString(categoryName)}");
        Assert.Equal(3, byCategory!.Meta.Total);
        var literalWildcard = await this.client.GetFromJsonAsync<RecipePage>("/api/v1/me/recipes?q=%25");
        Assert.Equal(0, literalWildcard!.Meta.Total);
        using var oversized = await this.client.GetAsync($"/api/v1/me/recipes?q={new string('a', 101)}");
        Assert.Equal(HttpStatusCode.UnprocessableEntity, oversized.StatusCode);

        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var manager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var user = await manager.FindByEmailAsync(other.User.Email);
            Assert.True((await manager.AddToRoleAsync(user!, "Admin")).Succeeded);
        }

        var admin = await this.LoginAsync(other.User.Email);
        this.SetToken(admin.AccessToken);
        var all = await this.client.GetFromJsonAsync<RecipePage>($"/api/v1/admin/recipes?q={term}&pageSize=1");
        Assert.Equal(3, all!.Meta.Total);
        var scoped = await this.client.GetFromJsonAsync<RecipePage>($"/api/v1/admin/recipes?q={term}&authorId={owner.User.Id}");
        Assert.Equal(2, scoped!.Meta.Total);
        var byAuthor = await this.client.GetFromJsonAsync<RecipePage>($"/api/v1/admin/recipes?q=recipe%20test%20user&authorId={owner.User.Id}");
        Assert.Equal(3, byAuthor!.Meta.Total);
    }

    /// <summary>Verifies that p7 ownership role escalation and admin override are enforced.</summary>
    /// <returns>A task representing the operation and its result.</returns>
    [Fact]
    public async Task P7OwnershipRoleEscalationAndAdminOverrideAreEnforced()
    {
        var owner = await this.RegisterAsync($"owner-{Guid.NewGuid():N}@example.com");
        var other = await this.RegisterAsync($"other-{Guid.NewGuid():N}@example.com");
        var categoryId = await this.GetCategoryIdAsync();
        this.SetToken(owner.AccessToken);

        using var createResponse = await this.client.PostAsJsonAsync("/api/v1/recipes", RecipeBody(categoryId, "Private family recipe"));
        var created = await createResponse.Content.ReadFromJsonAsync<DataEnvelope<RecipeResponse>>();
        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);
        Assert.Equal("draft", created?.Data.Status);
        Assert.Equal("\"1\"", createResponse.Headers.ETag?.Tag);

        using var lowercaseStatusResponse = await this.client.GetAsync("/api/v1/me/recipes?status=draft&page=1&pageSize=1");
        Assert.Equal(HttpStatusCode.OK, lowercaseStatusResponse.StatusCode);

        using var collidingResponse = await this.client.PostAsJsonAsync(
            "/api/v1/recipes",
            RecipeBody(categoryId, "Private family recipe", includeNutrition: false));
        var colliding = await collidingResponse.Content.ReadFromJsonAsync<DataEnvelope<RecipeResponse>>();
        Assert.Equal(HttpStatusCode.Created, collidingResponse.StatusCode);
        Assert.Equal($"{created!.Data.Slug}-2", colliding?.Data.Slug);

        this.SetToken(null);
        using var publicResponse = await this.client.GetAsync($"/api/v1/recipes/{created.Data.Slug}");
        Assert.Equal(HttpStatusCode.NotFound, publicResponse.StatusCode);

        this.SetToken(other.AccessToken);
        using var forbiddenResponse = await this.client.GetAsync($"/api/v1/me/recipes/{created.Data.Id}");
        Assert.Equal(HttpStatusCode.Forbidden, forbiddenResponse.StatusCode);

        using var forbiddenUpdate = CreatePut(
            created.Data.Id, 1, RecipeBody(categoryId, "Another author's update"));
        using var forbiddenUpdateResponse = await this.client.SendAsync(forbiddenUpdate);
        Assert.Equal(HttpStatusCode.Forbidden, forbiddenUpdateResponse.StatusCode);

        using var forbiddenAdminList = await this.client.GetAsync("/api/v1/admin/recipes");
        Assert.Equal(HttpStatusCode.Forbidden, forbiddenAdminList.StatusCode);

        using var forbiddenCategoryCreate = await this.client.PostAsJsonAsync(
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

        using var staleAuthorTokenResponse = await this.client.GetAsync("/api/v1/admin/recipes");
        Assert.Equal(HttpStatusCode.Forbidden, staleAuthorTokenResponse.StatusCode);

        var admin = await this.LoginAsync(other.User.Email);
        this.SetToken(admin.AccessToken);
        using var adminResponse = await this.client.GetAsync($"/api/v1/admin/recipes/{created.Data.Id}");
        Assert.Equal(HttpStatusCode.OK, adminResponse.StatusCode);

        using var adminUpdate = CreatePut(
            created.Data.Id, 1, RecipeBody(categoryId, "Admin authorized update"));
        using var adminUpdateResponse = await this.client.SendAsync(adminUpdate);
        Assert.Equal(HttpStatusCode.OK, adminUpdateResponse.StatusCode);
        Assert.Equal("\"2\"", adminUpdateResponse.Headers.ETag?.Tag);

        this.SetToken(owner.AccessToken);
        using var deleteRequest = new HttpRequestMessage(HttpMethod.Delete, $"/api/v1/recipes/{created.Data.Id}");
        deleteRequest.Headers.IfMatch.Add(new EntityTagHeaderValue("\"2\""));
        using var deleteResponse = await this.client.SendAsync(deleteRequest);
        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);

        using var afterDeleteResponse = await this.client.GetAsync($"/api/v1/me/recipes/{created.Data.Id}");
        Assert.Equal(HttpStatusCode.NotFound, afterDeleteResponse.StatusCode);
        await using var scope = factory.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        Assert.True((await dbContext.Recipes.IgnoreQueryFilters().SingleAsync(recipe => recipe.Id == created.Data.Id)).IsDeleted);
    }

    /// <summary>Verifies that stale recipe update returns conflict and current etag.</summary>
    /// <returns>A task representing the operation and its result.</returns>
    [Fact]
    public async Task StaleRecipeUpdateReturnsConflictAndCurrentEtag()
    {
        var owner = await this.RegisterAsync($"concurrency-{Guid.NewGuid():N}@example.com");
        var categoryId = await this.GetCategoryIdAsync();
        this.SetToken(owner.AccessToken);
        using var createResponse = await this.client.PostAsJsonAsync("/api/v1/recipes", RecipeBody(categoryId, "Concurrent recipe"));
        var created = (await createResponse.Content.ReadFromJsonAsync<DataEnvelope<RecipeResponse>>())!.Data;

        using var missingPrecondition = await this.client.PutAsJsonAsync(
            $"/api/v1/recipes/{created.Id}",
            RecipeBody(categoryId, "Update without precondition"));
        var preconditionProblem = await missingPrecondition.Content.ReadFromJsonAsync<Problem>();
        Assert.Equal(HttpStatusCode.BadRequest, missingPrecondition.StatusCode);
        Assert.Equal("PRECONDITION_REQUIRED", preconditionProblem?.Code);

        using var firstUpdate = CreatePut(created.Id, 1, RecipeBody(categoryId, "First concurrent update"));
        using var secondUpdate = CreatePut(created.Id, 1, RecipeBody(categoryId, "Second concurrent update"));
        var responses = await Task.WhenAll(this.client.SendAsync(firstUpdate), this.client.SendAsync(secondUpdate));
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

    /// <summary>Verifies that admin category slug is stable and delete with recipe conflicts.</summary>
    /// <returns>A task representing the operation and its result.</returns>
    [Fact]
    public async Task AdminCategorySlugIsStableAndDeleteWithRecipeConflicts()
    {
        var adminEmail = $"admin-{Guid.NewGuid():N}@example.com";
        await this.RegisterAsync(adminEmail);
        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var seededAdmin = await userManager.FindByEmailAsync(adminEmail);
            Assert.NotNull(seededAdmin);
            Assert.True((await userManager.AddToRoleAsync(seededAdmin, "Admin")).Succeeded);
        }

        var admin = await this.LoginAsync(adminEmail);
        this.SetToken(admin.AccessToken);
        var raceName = $"Danh mục race {Guid.NewGuid():N}";
        var raceResponses = await Task.WhenAll(
            this.client.PostAsJsonAsync(
                "/api/v1/categories",
                new { name = raceName, description = "First contender", orderIndex = 30 }),
            this.client.PostAsJsonAsync(
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

        using var createCategoryResponse = await this.client.PostAsJsonAsync(
            "/api/v1/categories",
            new { name = "Đồ uống thử nghiệm", description = "Test", orderIndex = 20 });
        var category = (await createCategoryResponse.Content.ReadFromJsonAsync<DataEnvelope<CategoryResponse>>())!.Data;
        Assert.Equal(HttpStatusCode.Created, createCategoryResponse.StatusCode);

        using var updateCategoryResponse = await this.client.PutAsJsonAsync(
            $"/api/v1/categories/{category.Id}",
            new { name = "Nước uống thử nghiệm", description = "Updated", orderIndex = 21 });
        var updated = (await updateCategoryResponse.Content.ReadFromJsonAsync<DataEnvelope<CategoryResponse>>())!.Data;
        Assert.Equal(category.Slug, updated.Slug);

        using var recipeResponse = await this.client.PostAsJsonAsync(
            "/api/v1/recipes",
            RecipeBody(category.Id, "Admin owned recipe"));
        Assert.Equal(HttpStatusCode.Created, recipeResponse.StatusCode);

        using var deleteCategoryResponse = await this.client.DeleteAsync($"/api/v1/categories/{category.Id}");
        var problem = await deleteCategoryResponse.Content.ReadFromJsonAsync<Problem>();
        Assert.Equal(HttpStatusCode.Conflict, deleteCategoryResponse.StatusCode);
        Assert.Equal("CATEGORY_DELETE_HAS_RECIPES", problem?.Code);
    }

    /// <summary>Verifies that category name bounds apply to create and update without changing storage.</summary>
    /// <param name="length">The length supplied to this operation.</param>
    /// <param name="valid">The valid supplied to this operation.</param>
    /// <returns>A task representing the operation and its result.</returns>
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
        await this.RegisterAsync(email);
        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var user = await users.FindByEmailAsync(email);
            Assert.NotNull(user);
            Assert.True((await users.AddToRoleAsync(user, "Admin")).Succeeded);
        }

        this.SetToken((await this.LoginAsync(email)).AccessToken);
        var plainName = length == 2 ? "QZ" : new string('x', Math.Max(0, length));
        var name = length < 0 ? "<b>Món chay</b>" : plainName;
        await using var checkScope = factory.Services.CreateAsyncScope();
        var db = checkScope.ServiceProvider.GetRequiredService<AppDbContext>();
        var before = await db.Categories.CountAsync();
        using var create = await this.client.PostAsJsonAsync("/api/v1/categories", new { name, orderIndex = 0 });
        Assert.Equal(valid ? HttpStatusCode.Created : HttpStatusCode.UnprocessableEntity, create.StatusCode);
        Assert.Equal(before + (valid ? 1 : 0), await db.Categories.CountAsync());

        var originalName = $"Category {Guid.NewGuid():N}";
        using var targetResponse = await this.client.PostAsJsonAsync("/api/v1/categories", new { name = originalName, orderIndex = 0 });
        targetResponse.EnsureSuccessStatusCode();
        var target = (await targetResponse.Content.ReadFromJsonAsync<DataEnvelope<CategoryResponse>>())!.Data;
        var updatedName = valid ? "Y" + name[1..] : name;
        using var update = await this.client.PutAsJsonAsync($"/api/v1/categories/{target.Id}", new { name = updatedName, orderIndex = 0 });
        Assert.Equal(valid ? HttpStatusCode.OK : HttpStatusCode.UnprocessableEntity, update.StatusCode);
        Assert.Equal(
            valid ? updatedName : originalName,
            await db.Categories.AsNoTracking().Where(item => item.Id == target.Id).Select(item => item.Name).SingleAsync());
        Assert.Equal(100, db.Model.FindEntityType(typeof(CulinaryBlog.Domain.Categories.Category))!
            .FindProperty(nameof(CulinaryBlog.Domain.Categories.Category.Name))!.GetMaxLength());
    }

    /// <summary>Verifies that malformed recipe json remains bad request and does not write.</summary>
    /// <returns>A task representing the operation and its result.</returns>
    [Fact]
    public async Task MalformedRecipeJsonRemainsBadRequestAndDoesNotWrite()
    {
        var author = await this.RegisterAsync($"malformed-{Guid.NewGuid():N}@example.com");
        this.SetToken(author.AccessToken);
        await using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var before = await db.Recipes.CountAsync();
        using var body = new StringContent("{", System.Text.Encoding.UTF8, "application/json");
        using var response = await this.client.PostAsync("/api/v1/recipes", body);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal(before, await db.Recipes.CountAsync());
    }

    /// <summary>Verifies that invalid category reference is422 without partial create or update.</summary>
    /// <returns>A task representing the operation and its result.</returns>
    [Fact]
    public async Task InvalidCategoryReferenceIs422WithoutPartialCreateOrUpdate()
    {
        var categoryId = await this.GetCategoryIdAsync();
        var author = await this.RegisterAsync($"foreign-category-{Guid.NewGuid():N}@example.com");
        this.SetToken(author.AccessToken);
        var missingCategory = Guid.NewGuid();
        await using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var count = await db.Recipes.CountAsync();
        using var invalidCreate = await this.client.PostAsJsonAsync("/api/v1/recipes", RecipeBody(missingCategory, "Invalid category recipe"));
        Assert.Equal(HttpStatusCode.UnprocessableEntity, invalidCreate.StatusCode);
        Assert.Equal(count, await db.Recipes.CountAsync());
        using var problem = JsonDocument.Parse(await invalidCreate.Content.ReadAsStringAsync());
        Assert.Contains(problem.RootElement.GetProperty("errors").EnumerateObject(), field =>
            field.Name.Equals("CategoryId", StringComparison.OrdinalIgnoreCase));
        using var createdResponse = await this.client.PostAsJsonAsync("/api/v1/recipes", RecipeBody(categoryId, "Valid category recipe"));
        createdResponse.EnsureSuccessStatusCode();
        var created = (await createdResponse.Content.ReadFromJsonAsync<DataEnvelope<RecipeResponse>>())!.Data;
        var before = await db.Recipes.AsNoTracking().Where(item => item.Id == created.Id)
            .Select(item => new { item.Title, item.CategoryId, item.Version }).SingleAsync();
        using var request = CreatePut(created.Id, 1, RecipeBody(missingCategory, "Rejected category update"));
        using var invalidUpdate = await this.client.SendAsync(request);
        Assert.Equal(HttpStatusCode.UnprocessableEntity, invalidUpdate.StatusCode);
        var after = await db.Recipes.AsNoTracking().Where(item => item.Id == created.Id)
            .Select(item => new { item.Title, item.CategoryId, item.Version }).SingleAsync();
        Assert.Equal(before, after);
        using var missingResource = await this.client.GetAsync($"/api/v1/categories/missing-{missingCategory:N}");
        Assert.Equal(HttpStatusCode.NotFound, missingResource.StatusCode);
    }

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

    private async Task<Guid> GetCategoryIdAsync()
    {
        this.SetToken(null);
        var response = await this.client.GetFromJsonAsync<DataEnvelope<CategoryResponse[]>>("/api/v1/categories");
        return response!.Data[0].Id;
    }

    private async Task<AuthSession> RegisterAsync(string email)
    {
        this.SetToken(null);
        using var response = await this.client.PostAsJsonAsync(
            "/api/v1/auth/register",
            new { displayName = "Recipe Test User", email, password = "Valid#Password1" });
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<DataEnvelope<AuthSession>>())!.Data;
    }

    private async Task<AuthSession> LoginAsync(string email)
    {
        this.SetToken(null);
        using var response = await this.client.PostAsJsonAsync(
            "/api/v1/auth/login",
            new { email, password = "Valid#Password1" });
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<DataEnvelope<AuthSession>>())!.Data;
    }

    private void SetToken(string? token) => this.client.DefaultRequestHeaders.Authorization = token is null
        ? null
        : new AuthenticationHeaderValue("Bearer", token);

    private sealed record DataEnvelope<T>(T Data);

    private sealed record AuthSession(string AccessToken, AuthUser User);

    private sealed record AuthUser(string Id, string Email);

    private sealed record RecipeResponse(Guid Id, string Slug, string Status);

    private sealed record CategoryResponse(Guid Id, string Slug, string Name);

    private sealed record RecipePage(RecipeResponse[] Data, PageMeta Meta);

    private sealed record PageMeta(int Total, int TotalPages);

    private sealed record Problem(string Code, string? CurrentETag = null);
}
