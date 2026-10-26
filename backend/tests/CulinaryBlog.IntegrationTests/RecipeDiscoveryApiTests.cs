using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using CulinaryBlog.Domain.Categories;
using CulinaryBlog.Domain.Recipes;
using CulinaryBlog.Infrastructure.Identity;
using CulinaryBlog.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace CulinaryBlog.IntegrationTests;

public sealed class RecipeDiscoveryApiTests(AuthApiFactory factory) : IClassFixture<AuthApiFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task ListAppliesAllFiltersSortAndPaginationToPublishedRecipesOnly()
    {
        var data = await SeedDiscoveryDataAsync();

        var response = await _client.GetFromJsonAsync<RecipePage>(
            $"/api/v1/recipes?categoryId={data.VietnameseCategoryId}" +
            "&difficulty=easy&maxCookTime=25&minServings=4&minPrepTime=5&maxPrepTime=15" +
            "&sort=title&page=1&pageSize=1");

        Assert.NotNull(response);
        Assert.Single(response.Data);
        Assert.Equal("Phở bò truyền thống", response.Data[0].Title);
        Assert.Equal("published", response.Data[0].Status);
        Assert.Equal(1, response.Meta.Total);
        Assert.Equal(1, response.Meta.TotalPages);

        var paged = await _client.GetFromJsonAsync<RecipePage>(
            $"/api/v1/recipes?categoryId={data.VietnameseCategoryId}&page=1&pageSize=1");
        Assert.NotNull(paged);
        Assert.Single(paged.Data);
        Assert.Equal(2, paged.Meta.Total);
        Assert.Equal(2, paged.Meta.TotalPages);
        Assert.True(paged.Meta.HasNextPage);

        using var invalidSort = await _client.GetAsync("/api/v1/recipes?sort=title%3Bdrop%20table%20recipes");
        Assert.Equal(HttpStatusCode.UnprocessableEntity, invalidSort.StatusCode);
    }

    [Fact]
    public async Task SearchIsAccentInsensitiveRankedAndNeverReturnsDrafts()
    {
        var data = await SeedDiscoveryDataAsync();
        await using var scope = factory.Services.CreateAsyncScope();
        var category = await scope.ServiceProvider.GetRequiredService<AppDbContext>().Categories
            .Where(item => item.Id == data.VietnameseCategoryId).Select(item => item.Slug).SingleAsync();

        var unaccented = await _client.GetFromJsonAsync<RecipePage>(
            $"/api/v1/recipes/search?q=pho&page=1&pageSize=12&category={category}");
        var prefix = await _client.GetFromJsonAsync<RecipePage>(
            $"/api/v1/recipes/search?q=ph&page=1&pageSize=12&category={category}");
        var multiTerm = await _client.GetFromJsonAsync<RecipePage>(
            $"/api/v1/recipes/search?q=bun%20bo&page=1&pageSize=12&category={category}");

        Assert.NotNull(unaccented);
        Assert.Contains(unaccented.Data, recipe => recipe.Title == "Phở bò truyền thống");
        Assert.DoesNotContain(unaccented.Data, recipe => recipe.Status != "published");
        Assert.DoesNotContain(unaccented.Data, recipe => recipe.Title == "Phở bí mật");
        Assert.NotNull(prefix);
        Assert.Contains(prefix.Data, recipe => recipe.Title == "Phở bò truyền thống");
        Assert.NotNull(multiTerm);
        Assert.Equal("Bún bò Huế", multiTerm.Data[0].Title);
    }

    [Fact]
    public async Task SearchHandlesSpecialCharactersAndEmptyResults()
    {
        await SeedDiscoveryDataAsync();

        var specialCharacters = await _client.GetFromJsonAsync<RecipePage>(
            "/api/v1/recipes/search?q=%25%27%3B--&page=1&pageSize=12");
        var noMatch = await _client.GetFromJsonAsync<RecipePage>(
            $"/api/v1/recipes/search?q=khong-ton-tai-{Guid.NewGuid():N}&page=1&pageSize=12");

        Assert.NotNull(specialCharacters);
        Assert.Empty(specialCharacters.Data);
        Assert.Equal(0, specialCharacters.Meta.Total);
        Assert.NotNull(noMatch);
        Assert.Empty(noMatch.Data);
        Assert.Equal(0, noMatch.Meta.Total);
    }

    [Fact]
    public async Task PublicCacheNeverLeaksPrivateRecipesAcrossGuestAuthorAndAdmin()
    {
        using var client = factory.CreateClient();
        var suffix = Guid.NewGuid().ToString("N");
        var author = await RegisterAsync(client, $"cache-author-{suffix}@example.com");
        var admin = await RegisterAsync(client, $"cache-admin-{suffix}@example.com");
        Guid categoryId;
        Guid draftId;
        Guid publishedId;

        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var services = scope.ServiceProvider;
            var dbContext = services.GetRequiredService<AppDbContext>();
            var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
            var adminUser = await userManager.FindByIdAsync(admin.User.Id);
            Assert.NotNull(adminUser);
            Assert.True((await userManager.AddToRoleAsync(adminUser, "Admin")).Succeeded);

            var category = Category.Create(
                Guid.NewGuid(), $"Cache isolation {suffix}", $"cache-isolation-{suffix}", null, null, 0);
            categoryId = category.Id;
            var published = CreateRecipe(
                Guid.Parse(author.User.Id), categoryId, $"public-isolation-{suffix}",
                $"Public isolation {suffix}", 10, 20, 4, RecipeDifficulty.Easy);
            published.Publish(DateTimeOffset.UtcNow, true, 1, [1]);
            var draft = CreateRecipe(
                Guid.Parse(author.User.Id), categoryId, $"private-isolation-{suffix}",
                $"Private isolation {suffix}", 10, 20, 4, RecipeDifficulty.Easy);
            draftId = draft.Id;
            publishedId = published.Id;
            dbContext.AddRange(category, published, draft);
            await dbContext.SaveChangesAsync();
        }

        admin = await LoginAsync(client, admin.User.Email);
        var publicPath = $"/api/v1/recipes?categoryId={categoryId}&page=1&pageSize=50";

        SetToken(client, null);
        var guest = await client.GetFromJsonAsync<RecipePageWithId>(publicPath);
        SetToken(client, author.AccessToken);
        var authorView = await client.GetFromJsonAsync<RecipePageWithId>(publicPath);
        var authorPrivate = await client.GetFromJsonAsync<RecipePageWithId>(
            "/api/v1/me/recipes?status=Draft&page=1&pageSize=50");
        SetToken(client, admin.AccessToken);
        var adminView = await client.GetFromJsonAsync<RecipePageWithId>(publicPath);
        var adminPrivate = await client.GetFromJsonAsync<RecipePageWithId>(
            $"/api/v1/admin/recipes?status=Draft&authorId={author.User.Id}&page=1&pageSize=50");

        Assert.NotNull(guest);
        Assert.NotNull(authorView);
        Assert.NotNull(adminView);
        Assert.Equal([publishedId], guest.Data.Select(recipe => recipe.Id));
        Assert.Equal(guest.Data, authorView.Data);
        Assert.Equal(guest.Data, adminView.Data);
        Assert.Contains(authorPrivate!.Data, recipe => recipe.Id == draftId);
        Assert.Contains(adminPrivate!.Data, recipe => recipe.Id == draftId);
        Assert.DoesNotContain(guest.Data, recipe => recipe.Id == draftId);
    }

    [Theory]
    [InlineData("ga")]
    [InlineData("gà")]
    public async Task ShortSearchPrefersWholeWordsAndPreservesIncompletePrefixes(string search)
    {
        var data = await SeedDiscoveryDataAsync();
        await using var scope = factory.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var category = await dbContext.Categories.SingleAsync(item => item.Id == data.VietnameseCategoryId);
        var authorId = await dbContext.Recipes.Where(recipe => recipe.CategoryId == category.Id)
            .Select(recipe => recipe.AuthorId).FirstAsync();
        var examples = new[]
        {
            (Title: "Gà kho gừng", Description: "Gà ta kho gừng."),
            (Title: "Xôi xéo", Description: "Xôi đậu xanh với mỡ gà."),
            (Title: "Súp cua", Description: "Thịt cua và gà xé."),
            (Title: "Xôi gấc", Description: "Xôi nếp nấu cùng gấc."),
            (Title: "Cháo lòng", Description: "Cháo gạo rang và lòng heo."),
            (Title: "Nem lụi Huế", Description: "Thịt heo với sốt gan."),
            (Title: "Bột chiên", Description: "Bột gạo chiên với trứng."),
        };
        foreach (var example in examples)
        {
            var recipe = Recipe.Create(Guid.NewGuid(), authorId, $"short-search-{Guid.NewGuid():N}",
                example.Title, example.Description, category.Id, 10, 20, 4, RecipeDifficulty.Easy,
                "Hướng dẫn", null);
            recipe.Publish(DateTimeOffset.UtcNow, true, 1, [1]);
            dbContext.Recipes.Add(recipe);
        }
        var draft = CreateRecipe(authorId, category.Id, $"draft-ga-{Guid.NewGuid():N}",
            "Gà bí mật", 10, 20, 4, RecipeDifficulty.Easy);
        dbContext.Recipes.Add(draft);
        await dbContext.SaveChangesAsync();

        var result = await _client.GetFromJsonAsync<RecipePage>(
            $"/api/v1/recipes/search?q={Uri.EscapeDataString(search)}&category={category.Slug}&pageSize=50");

        Assert.NotNull(result);
        Assert.Equal(1, result.Meta.Total);
        Assert.Equal(examples.Take(1).Select(example => example.Title).Order(),
            result.Data.Select(recipe => recipe.Title).Order());
        Assert.All(result.Data, recipe => Assert.Equal("published", recipe.Status));

        var prefix = await _client.GetFromJsonAsync<RecipePage>(
            $"/api/v1/recipes/search?q=ph&category={category.Slug}&pageSize=50");
        Assert.NotNull(prefix);
        Assert.Contains(prefix.Data, recipe => recipe.Title == "Phở bò truyền thống");
        Assert.DoesNotContain(prefix.Data, recipe => recipe.Status != "published");
    }

    [Theory]
    [InlineData("ga nuong", 1, "Gà nướng muối ớt")]
    [InlineData("  GÀ   NƯỚNG  ", 1, "Gà nướng muối ớt")]
    [InlineData("gà nướng", 1, "Gà nướng muối ớt")]
    [InlineData("ga nuon", 1, "Gà nướng muối ớt")]
    [InlineData("ga nuongg", 1, "Gà nướng muối ớt")]
    [InlineData("pho bo", 1, "Phở bò truyền thống")]
    [InlineData("phở bò", 1, "Phở bò truyền thống")]
    [InlineData("cá", 1, "Cá kho tộ")]
    [InlineData("ca", 2, null)]
    [InlineData("bò", 4, null)]
    [InlineData("bơ", 1, "Bơ dầm")]
    [InlineData("bo", 5, null)]
    [InlineData("ga xyznotfound", 0, null)]
    [InlineData("ingredientonly", 0, null)]
    [InlineData("ingredien", 0, null)]
    public async Task VietnameseSearchRequiresAllWordsAndPrefersAccurateMatches(
        string search, int expectedTotal, string? expectedFirst)
    {
        var data = await SeedDiscoveryDataAsync();
        await using var scope = factory.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var category = await dbContext.Categories.SingleAsync(item => item.Id == data.VietnameseCategoryId);
        var authorId = await dbContext.Recipes.Where(recipe => recipe.CategoryId == category.Id)
            .Select(recipe => recipe.AuthorId).FirstAsync();
        var examples = new[]
        {
            (Title: "Gà nướng muối ớt", Description: "Gà nướng cùng muối ớt."),
            (Title: "Phở gà", Description: "Nước dùng có gừng nướng và gà xé."),
            (Title: "Nem lụi Huế", Description: "Thịt nướng với sốt gan và đậu phộng ingredientonly."),
            (Title: "Gỏi đu đủ khô bò", Description: "Gỏi bò với đậu phộng."),
            (Title: "Cá kho tộ", Description: "Cá kho trong nồi đất."),
            (Title: "Cà ri chay", Description: "Cà ri với rau củ."),
            (Title: "Bò kho", Description: "Thịt bò hầm."),
            (Title: "Bơ dầm", Description: "Bơ chín dầm sữa."),
        };
        foreach (var example in examples)
        {
            var recipe = Recipe.Create(Guid.NewGuid(), authorId, $"vietnamese-{Guid.NewGuid():N}",
                example.Title, example.Description, category.Id, 10, 20, 4, RecipeDifficulty.Easy,
                "Hướng dẫn", null);
            recipe.Publish(DateTimeOffset.UtcNow, true, 1, [1]);
            dbContext.Recipes.Add(recipe);
        }
        await dbContext.SaveChangesAsync();

        var result = await _client.GetFromJsonAsync<RecipePage>(
            $"/api/v1/recipes/search?q={Uri.EscapeDataString(search)}&category={category.Slug}&pageSize=50");
        Assert.NotNull(result);
        Assert.Equal(expectedTotal, result.Meta.Total);
        Assert.All(result.Data, recipe => Assert.Equal("published", recipe.Status));
        if (expectedFirst is not null) Assert.Equal(expectedFirst, result.Data[0].Title);
        if (search.Contains("nu", StringComparison.OrdinalIgnoreCase))
            Assert.DoesNotContain(result.Data, recipe => recipe.Title == "Nem lụi Huế");
    }

    [Fact]
    public async Task RedisOutageDoesNotCrashPublicReadApi()
    {
        var outageFactory = new AuthApiFactory();
        await outageFactory.InitializeAsync();
        try
        {
            using var client = outageFactory.CreateClient();
            var data = await SeedDiscoveryDataAsync(outageFactory);
            await outageFactory.StopRedisAsync();
            using var response = await client.GetAsync(
                $"/api/v1/recipes?categoryId={data.VietnameseCategoryId}&page=1&pageSize=12");
            var page = await response.Content.ReadFromJsonAsync<RecipePage>();

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.NotNull(page);
            Assert.Equal(2, page.Meta.Total);
        }
        finally
        {
            outageFactory.Dispose();
            await ((IAsyncLifetime)outageFactory).DisposeAsync();
        }
    }

    [Fact]
    public async Task DiscoveryMigrationCreatesRequiredExtensionsAndIndexes()
    {
        await using var scope = factory.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var extensions = await dbContext.Database.SqlQueryRaw<string>(
                "SELECT extname AS \"Value\" FROM pg_extension WHERE extname IN ('unaccent', 'pg_trgm')")
            .ToListAsync();
        var indexes = await dbContext.Database.SqlQueryRaw<string>(
                "SELECT indexname AS \"Value\" FROM pg_indexes WHERE tablename = 'Recipes'")
            .ToListAsync();

        Assert.Contains("unaccent", extensions);
        Assert.Contains("pg_trgm", extensions);
        Assert.Contains("IX_Recipes_SearchVector", indexes);
        Assert.Contains("IX_Recipes_SearchTitle", indexes);
        Assert.Contains("IX_Recipes_Status_CategoryId_Difficulty_PublishedAt", indexes);
        Assert.Contains("IX_Recipes_Status_CreatedAt", indexes);
    }

    private async Task<DiscoverySeed> SeedDiscoveryDataAsync(AuthApiFactory? targetFactory = null)
    {
        await using var scope = (targetFactory ?? factory).Services.CreateAsyncScope();
        var services = scope.ServiceProvider;
        var dbContext = services.GetRequiredService<AppDbContext>();
        var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
        var suffix = Guid.NewGuid().ToString("N");
        var author = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            UserName = $"discovery-{suffix}@example.com",
            Email = $"discovery-{suffix}@example.com",
            DisplayName = "Discovery Author",
            IsActive = true,
        };
        Assert.True((await userManager.CreateAsync(author)).Succeeded);

        var vietnamese = Category.Create(Guid.NewGuid(), $"Món Việt {suffix}", $"mon-viet-{suffix}", null, null, 0);
        var dessert = Category.Create(Guid.NewGuid(), $"Món ngọt {suffix}", $"mon-ngot-{suffix}", null, null, 1);
        dbContext.Categories.AddRange(vietnamese, dessert);

        var pho = CreateRecipe(author.Id, vietnamese.Id, $"pho-bo-{suffix}", "Phở bò truyền thống", 10, 20, 4, RecipeDifficulty.Easy);
        pho.Publish(DateTimeOffset.UtcNow.AddMinutes(-30), true, 1, [1]);
        var bunBo = CreateRecipe(author.Id, vietnamese.Id, $"bun-bo-{suffix}", "Bún bò Huế", 20, 30, 6, RecipeDifficulty.Medium);
        bunBo.Publish(DateTimeOffset.UtcNow.AddMinutes(-20), true, 1, [1]);
        var cake = CreateRecipe(author.Id, dessert.Id, $"banh-ngot-{suffix}", "Bánh ngọt chocolate", 40, 45, 8, RecipeDifficulty.Hard);
        cake.Publish(DateTimeOffset.UtcNow.AddMinutes(-10), true, 1, [1]);
        var draft = CreateRecipe(author.Id, vietnamese.Id, $"pho-bi-mat-{suffix}", "Phở bí mật", 5, 10, 2, RecipeDifficulty.Easy);
        dbContext.Recipes.AddRange(pho, bunBo, cake, draft);
        await dbContext.SaveChangesAsync();
        return new DiscoverySeed(vietnamese.Id);
    }

    private static Recipe CreateRecipe(
        Guid authorId,
        Guid categoryId,
        string slug,
        string title,
        int prepTime,
        int cookTime,
        int servings,
        RecipeDifficulty difficulty) =>
        Recipe.Create(
            Guid.NewGuid(),
            authorId,
            slug,
            title,
            "Mô tả công thức dùng cho kiểm thử discovery.",
            categoryId,
            prepTime,
            cookTime,
            servings,
            difficulty,
            "Hướng dẫn",
            null);

    private static async Task<AuthSession> RegisterAsync(HttpClient client, string email)
    {
        SetToken(client, null);
        using var response = await client.PostAsJsonAsync(
            "/api/v1/auth/register",
            new { displayName = "Cache Test", email, password = "Valid#Password1" });
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<DataEnvelope<AuthSession>>())!.Data;
    }

    private static async Task<AuthSession> LoginAsync(HttpClient client, string email)
    {
        SetToken(client, null);
        using var response = await client.PostAsJsonAsync(
            "/api/v1/auth/login",
            new { email, password = "Valid#Password1" });
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<DataEnvelope<AuthSession>>())!.Data;
    }

    private static void SetToken(HttpClient client, string? token) => client.DefaultRequestHeaders.Authorization = token is null
        ? null
        : new AuthenticationHeaderValue("Bearer", token);

    private sealed record DiscoverySeed(Guid VietnameseCategoryId);

    private sealed record RecipePage(IReadOnlyList<RecipeItem> Data, PageMeta Meta);

    private sealed record RecipeItem(string Title, string Status);

    private sealed record RecipePageWithId(IReadOnlyList<RecipeItemWithId> Data, PageMeta Meta);

    private sealed record RecipeItemWithId(Guid Id, string Title, string Status);

    private sealed record DataEnvelope<T>(T Data);

    private sealed record AuthSession(string AccessToken, AuthUser User);

    private sealed record AuthUser(string Id, string Email);

    private sealed record PageMeta(int Page, int PageSize, int Total, int TotalPages, bool HasNextPage);
}
