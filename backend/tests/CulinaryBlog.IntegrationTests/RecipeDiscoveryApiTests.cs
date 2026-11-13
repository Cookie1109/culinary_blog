// Culinary Blog. Authorship is recorded in the repository history.

using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using CulinaryBlog.Application.Abstractions.Caching;
using CulinaryBlog.Application.Content;
using CulinaryBlog.Domain.Categories;
using CulinaryBlog.Domain.Recipes;
using CulinaryBlog.Infrastructure.Content;
using CulinaryBlog.Infrastructure.Identity;
using CulinaryBlog.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace CulinaryBlog.IntegrationTests;

/// <summary>Contains regression checks for recipe discovery api tests.</summary>
/// <param name="factory">The factory supplied to this operation.</param>
public sealed class RecipeDiscoveryApiTests(AuthApiFactory factory) : IClassFixture<AuthApiFactory>
{
    private readonly HttpClient sharedClient = factory.CreateClient();

    /// <summary>Verifies that the accent predicate is executed as bounded PostgreSQL SQL rather than a .NET regex.</summary>
    [Fact]
    public void AccentPreferenceRegexIsTranslatedToPostgres()
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        string sql = db.Recipes.Where(ContentService.BuildAccentWordPredicate("phở")).ToQueryString();
        Assert.Contains("~*", sql, StringComparison.Ordinal);
        Assert.Equal(30, db.Database.GetCommandTimeout());
    }

    /// <summary>Verifies that sort aliases match canonical order reject conflicts and vary category cache.</summary>
    /// <returns>A task representing the operation and its result.</returns>
    [Fact]
    public async Task SortAliasesMatchCanonicalOrderRejectConflictsAndVaryCategoryCache()
    {
        var data = await this.SeedDiscoveryDataAsync();
        string path = $"/api/v1/recipes?categoryId={data.VietnameseCategoryId}";
        var canonical = await this.sharedClient.GetFromJsonAsync<RecipePage>(path + "&sort=-title");
        var alias = await this.sharedClient.GetFromJsonAsync<RecipePage>(path + "&sortBy=title&sortOrder=desc");
        Assert.Equal(canonical!.Data.Select(item => item.Title), alias!.Data.Select(item => item.Title));
        using var same = await this.sharedClient.GetAsync(path + "&sort=-title&sortBy=title&sortOrder=desc");
        Assert.Equal(HttpStatusCode.OK, same.StatusCode);
        using var conflict = await this.sharedClient.GetAsync(path + "&sort=title&sortBy=title&sortOrder=desc");
        Assert.Equal(HttpStatusCode.BadRequest, conflict.StatusCode);
        using var defaultDirection = await this.sharedClient.GetAsync(path + "&sortBy=title");
        Assert.Equal(HttpStatusCode.OK, defaultDirection.StatusCode);
        using var defaultField = await this.sharedClient.GetAsync(path + "&sortOrder=asc");
        Assert.Equal(HttpStatusCode.OK, defaultField.StatusCode);
        string categoryPath = $"/api/v1/categories/{data.VietnameseSlug}?sortBy=title";
        var ascending = await this.sharedClient.GetFromJsonAsync<System.Text.Json.JsonElement>(categoryPath + "&sortOrder=asc");
        var descending = await this.sharedClient.GetFromJsonAsync<System.Text.Json.JsonElement>(categoryPath + "&sortOrder=desc");
        string?[] first = [.. ascending.GetProperty("data").GetProperty("recipes").EnumerateArray().Select(item => item.GetProperty("title").GetString())];
        string?[] second = [.. descending.GetProperty("data").GetProperty("recipes").EnumerateArray().Select(item => item.GetProperty("title").GetString())];
        Assert.Equal(first.Reverse(), second);
        using var categoryConflict = await this.sharedClient.GetAsync(categoryPath + "&sort=title&sortOrder=desc");
        Assert.Equal(HttpStatusCode.BadRequest, categoryConflict.StatusCode);
    }

    /// <summary>Verifies that browsing accepts category slug and total time without a keyword.</summary>
    /// <returns>A task representing the operation and its result.</returns>
    [Fact]
    public async Task BrowsingAcceptsCategorySlugAndTotalTimeWithoutAKeyword()
    {
        var data = await this.SeedDiscoveryDataAsync();
        string path = $"/api/v1/recipes?category={data.VietnameseSlug}&maxTime=35&maxCookTime=25&minServings=4&minPrepTime=5&maxPrepTime=15";
        var result = await this.sharedClient.GetFromJsonAsync<RecipePage>(path);
        Assert.Equal("Phở bò truyền thống", Assert.Single(result!.Data).Title);
        Assert.Null(Assert.Single(result.Data).RelevanceScore);
        using var invalidTime = await this.sharedClient.GetAsync("/api/v1/recipes?maxTime=0");
        Assert.Equal(HttpStatusCode.UnprocessableEntity, invalidTime.StatusCode);
    }

    /// <summary>Verifies that keyword search rejects missing or short queries.</summary>
    /// <param name="search">The search supplied to this operation.</param>
    /// <returns>A task representing the operation and its result.</returns>
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("x")]
    public async Task KeywordSearchRejectsMissingOrShortQueries(string? search)
    {
        string query = search is null ? string.Empty : "?q=" + Uri.EscapeDataString(search);
        using var response = await this.sharedClient.GetAsync("/api/v1/recipes/search" + query);
        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
    }

    /// <summary>Verifies that public categories are alphabetical even when admin order indexes are reversed.</summary>
    /// <returns>A task representing the operation and its result.</returns>
    [Fact]
    public async Task PublicCategoriesAreAlphabeticalEvenWhenAdminOrderIndexesAreReversed()
    {
        string suffix = Guid.NewGuid().ToString("N");
        var alphabeticallyFirst = Category.Create(Guid.NewGuid(), "Aaa " + suffix, "aaa-" + suffix, null, null, 999);
        var alphabeticallyLast = Category.Create(Guid.NewGuid(), "Zzz " + suffix, "zzz-" + suffix, null, null, 0);
        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            db.Categories.AddRange(alphabeticallyLast, alphabeticallyFirst);
            await db.SaveChangesAsync();
            await scope.ServiceProvider.GetRequiredService<IApplicationCache>().RemoveByTagAsync(PublicCacheKeys.CategoriesTag);
        }

        var response = await this.sharedClient.GetFromJsonAsync<DataEnvelope<List<CategoryDto>>>("/api/v1/categories");
        Assert.NotNull(response);
        Assert.True(response.Data.FindIndex(category => category.Id == alphabeticallyFirst.Id) < response.Data.FindIndex(category => category.Id == alphabeticallyLast.Id));
        Assert.Contains(response.Data, category => category.Id == alphabeticallyFirst.Id && category.OrderIndex == 999);
    }

    /// <summary>Verifies that search combines advanced filters and keeps each cached filter combination separate.</summary>
    /// <returns>A task representing the operation and its result.</returns>
    [Fact]
    public async Task SearchCombinesAdvancedFiltersAndKeepsEachCachedFilterCombinationSeparate()
    {
        var data = await this.SeedDiscoveryDataAsync();
        string path = $"/api/v1/recipes/search?q=bo&categoryId={data.VietnameseCategoryId}";
        var combined = await this.sharedClient.GetFromJsonAsync<RecipePage>(path + $"&category={data.VietnameseSlug}&difficulty=easy&maxTime=35&maxCookTime=25&minServings=4&minPrepTime=5&maxPrepTime=15");
        Assert.Equal("Phở bò truyền thống", Assert.Single(combined!.Data).Title);
        Assert.True(Assert.Single(combined.Data).RelevanceScore > 0);

        var larger = await this.sharedClient.GetFromJsonAsync<RecipePage>(path + "&minServings=6");
        Assert.Equal("Bún bò Huế", Assert.Single(larger!.Data).Title);
        var longerPreparation = await this.sharedClient.GetFromJsonAsync<RecipePage>(path + "&minPrepTime=15&maxPrepTime=25");
        Assert.Equal("Bún bò Huế", Assert.Single(longerPreparation!.Data).Title);
        var noCooking = await this.sharedClient.GetFromJsonAsync<RecipePage>(path + "&maxCookTime=0");
        Assert.Empty(noCooking!.Data);
        var incompatibleCategory = await this.sharedClient.GetFromJsonAsync<RecipePage>(path + "&category=nonexistent-category");
        Assert.Empty(incompatibleCategory!.Data);
        var normalizedCategory = await this.sharedClient.GetFromJsonAsync<RecipePage>(path + $"&category={data.VietnameseSlug.ToUpperInvariant()}&maxCookTime=24");
        Assert.Equal("Phở bò truyền thống", Assert.Single(normalizedCategory!.Data).Title);
        using var reversedRange = await this.sharedClient.GetAsync(path + "&minPrepTime=30&maxPrepTime=10");
        Assert.Equal(HttpStatusCode.UnprocessableEntity, reversedRange.StatusCode);
    }

    /// <summary>Verifies that browsing supports field directions with stable pagination.</summary>
    /// <param name="sort">The sort supplied to this operation.</param>
    /// <param name="firstTitle">The first title supplied to this operation.</param>
    /// <returns>A task representing the operation and its result.</returns>
    [Theory]
    [InlineData("title", "Bún bò Huế")]
    [InlineData("-title", "Phở bò truyền thống")]
    [InlineData("az", "Bún bò Huế")]
    [InlineData("za", "Phở bò truyền thống")]
    [InlineData("cookTime", "Phở bò truyền thống")]
    [InlineData("-cookTime", "Bún bò Huế")]
    [InlineData("prepTime", "Phở bò truyền thống")]
    [InlineData("-prepTime", "Bún bò Huế")]
    [InlineData("servings", "Phở bò truyền thống")]
    [InlineData("-servings", "Bún bò Huế")]
    [InlineData("quickest", "Phở bò truyền thống")]
    [InlineData("createdAt", "Phở bò truyền thống")]
    [InlineData("oldest", "Phở bò truyền thống")]
    [InlineData("-createdAt", "Bún bò Huế")]
    [InlineData("newest", "Bún bò Huế")]
    public async Task BrowsingSupportsFieldDirectionsWithStablePagination(string sort, string firstTitle)
    {
        var data = await this.SeedDiscoveryDataAsync();
        string path = $"/api/v1/recipes?categoryId={data.VietnameseCategoryId}&sort={sort}&pageSize=1";
        var first = await this.sharedClient.GetFromJsonAsync<RecipePage>(path + "&page=1");
        var second = await this.sharedClient.GetFromJsonAsync<RecipePage>(path + "&page=2");
        Assert.Equal(firstTitle, Assert.Single(first!.Data).Title);
        Assert.NotEqual(firstTitle, Assert.Single(second!.Data).Title);
        Assert.Null(Assert.Single(first.Data).RelevanceScore);
        Assert.Equal(2, first.Meta.Total);
        Assert.Equal(2, first.Meta.TotalPages);
    }

    /// <summary>Verifies that default keyword sort uses weighted rank and exposes scores without returning drafts.</summary>
    /// <returns>A task representing the operation and its result.</returns>
    [Fact]
    public async Task DefaultKeywordSortUsesWeightedRankAndExposesScoresWithoutReturningDrafts()
    {
        string term = "rank" + Guid.NewGuid().ToString("N");
        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            string authorId = await db.Users.Select(user => user.Id).FirstAsync();
            var category = Category.Create(Guid.NewGuid(), "Rank " + term, term, null, null, 0);
            var titleMatch = CreateRecipe(authorId, category.Id, term + "-title", "title " + term + " " + term, 10, 10, 4, RecipeDifficulty.Easy);
            titleMatch.Publish(DateTimeOffset.UtcNow.AddDays(-1), true, 1, [1]);
            var otherTitleMatch = CreateRecipe(authorId, category.Id, term + "-other-title", "Other " + term, 10, 10, 4, RecipeDifficulty.Easy);
            otherTitleMatch.Publish(DateTimeOffset.UtcNow, true, 1, [1]);
            var descriptionMatch = CreateRecipe(authorId, category.Id, term + "-description", "description match recipe", 10, 10, 4, RecipeDifficulty.Easy, term);
            descriptionMatch.Publish(DateTimeOffset.UtcNow, true, 1, [1]);
            var draft = CreateRecipe(authorId, category.Id, term + "-draft", "Private " + term, 10, 10, 4, RecipeDifficulty.Easy);
            db.AddRange(category, titleMatch, otherTitleMatch, descriptionMatch, draft);
            await db.SaveChangesAsync();
        }

        var result = await this.sharedClient.GetFromJsonAsync<RecipePage>($"/api/v1/recipes/search?q={term}");
        Assert.Equal(2, result!.Data.Count);
        Assert.Equal("title " + term + " " + term, result.Data[0].Title);
        Assert.DoesNotContain(result.Data, recipe => recipe.Title == "description match recipe");
        Assert.True(result.Data[0].RelevanceScore > result.Data[1].RelevanceScore);
        Assert.All(result.Data, recipe => Assert.True(recipe.RelevanceScore > 0));
        Assert.DoesNotContain(result.Data, recipe => recipe.Status != "published");
    }

    /// <summary>Verifies that list applies all filters sort and pagination to published recipes only.</summary>
    /// <returns>A task representing the operation and its result.</returns>
    [Fact]
    public async Task ListAppliesAllFiltersSortAndPaginationToPublishedRecipesOnly()
    {
        var data = await this.SeedDiscoveryDataAsync();

        var response = await this.sharedClient.GetFromJsonAsync<RecipePage>(
            $"/api/v1/recipes?categoryId={data.VietnameseCategoryId}" +
            "&difficulty=easy&maxCookTime=25&minServings=4&minPrepTime=5&maxPrepTime=15" +
            "&sort=title&page=1&pageSize=1");

        Assert.NotNull(response);
        Assert.Single(response.Data);
        Assert.Equal("Phở bò truyền thống", response.Data[0].Title);
        Assert.Equal("published", response.Data[0].Status);
        Assert.Equal(1, response.Meta.Total);
        Assert.Equal(1, response.Meta.TotalPages);

        var paged = await this.sharedClient.GetFromJsonAsync<RecipePage>(
            $"/api/v1/recipes?categoryId={data.VietnameseCategoryId}&page=1&pageSize=1");
        Assert.NotNull(paged);
        Assert.Single(paged.Data);
        Assert.Equal(2, paged.Meta.Total);
        Assert.Equal(2, paged.Meta.TotalPages);
        Assert.True(paged.Meta.HasNextPage);

        using var invalidSort = await this.sharedClient.GetAsync("/api/v1/recipes?sort=title%3Bdrop%20table%20recipes");
        Assert.Equal(HttpStatusCode.UnprocessableEntity, invalidSort.StatusCode);
    }

    /// <summary>Verifies that search is accent insensitive ranked and never returns drafts.</summary>
    /// <returns>A task representing the operation and its result.</returns>
    [Fact]
    public async Task SearchIsAccentInsensitiveRankedAndNeverReturnsDrafts()
    {
        var data = await this.SeedDiscoveryDataAsync();
        await using var scope = factory.Services.CreateAsyncScope();
        string category = await scope.ServiceProvider.GetRequiredService<AppDbContext>().Categories
            .Where(item => item.Id == data.VietnameseCategoryId).Select(item => item.Slug).SingleAsync();

        var unaccented = await this.sharedClient.GetFromJsonAsync<RecipePage>(
            $"/api/v1/recipes/search?q=pho&page=1&pageSize=12&category={category}");
        var prefix = await this.sharedClient.GetFromJsonAsync<RecipePage>(
            $"/api/v1/recipes/search?q=ph&page=1&pageSize=12&category={category}");
        var multiTerm = await this.sharedClient.GetFromJsonAsync<RecipePage>(
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

    /// <summary>Verifies that search handles special characters and empty results.</summary>
    /// <returns>A task representing the operation and its result.</returns>
    [Fact]
    public async Task SearchHandlesSpecialCharactersAndEmptyResults()
    {
        await this.SeedDiscoveryDataAsync();

        var specialCharacters = await this.sharedClient.GetFromJsonAsync<RecipePage>(
            "/api/v1/recipes/search?q=%25%27%3B--&page=1&pageSize=12");
        var noMatch = await this.sharedClient.GetFromJsonAsync<RecipePage>(
            $"/api/v1/recipes/search?q=khong-ton-tai-{Guid.NewGuid():N}&page=1&pageSize=12");

        Assert.NotNull(specialCharacters);
        Assert.Empty(specialCharacters.Data);
        Assert.False(string.IsNullOrWhiteSpace(specialCharacters.Message));
        Assert.Equal(0, specialCharacters.Meta.Total);
        Assert.NotNull(noMatch);
        Assert.Empty(noMatch.Data);
        Assert.False(string.IsNullOrWhiteSpace(noMatch.Message));
        Assert.Equal(0, noMatch.Meta.Total);
    }

    /// <summary>Verifies that public cache never leaks private recipes across guest author and admin.</summary>
    /// <returns>A task representing the operation and its result.</returns>
    [Fact]
    public async Task PublicCacheNeverLeaksPrivateRecipesAcrossGuestAuthorAndAdmin()
    {
        using var client = factory.CreateClient();
        string suffix = Guid.NewGuid().ToString("N");
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
                author.User.Id,
                categoryId,
                $"public-isolation-{suffix}",
                $"Public isolation {suffix}",
                10,
                20,
                4,
                RecipeDifficulty.Easy);
            published.Publish(DateTimeOffset.UtcNow, true, 1, [1]);
            var draft = CreateRecipe(
                author.User.Id,
                categoryId,
                $"private-isolation-{suffix}",
                $"Private isolation {suffix}",
                10,
                20,
                4,
                RecipeDifficulty.Easy);
            draftId = draft.Id;
            publishedId = published.Id;
            dbContext.AddRange(category, published, draft);
            await dbContext.SaveChangesAsync();
        }

        admin = await LoginAsync(client, admin.User.Email);
        string publicPath = $"/api/v1/recipes?categoryId={categoryId}&page=1&pageSize=50";

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

    /// <summary>Verifies that short search prefers whole words and preserves incomplete prefixes.</summary>
    /// <param name="search">The search supplied to this operation.</param>
    /// <returns>A task representing the operation and its result.</returns>
    [Theory]
    [InlineData("ga")]
    [InlineData("gà")]
    public async Task ShortSearchPrefersWholeWordsAndPreservesIncompletePrefixes(string search)
    {
        var data = await this.SeedDiscoveryDataAsync();
        await using var scope = factory.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var category = await dbContext.Categories.SingleAsync(item => item.Id == data.VietnameseCategoryId);
        string authorId = await dbContext.Recipes.Where(recipe => recipe.CategoryId == category.Id)
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
        foreach (var (title, description) in examples)
        {
            var recipe = Recipe.Create(
                Guid.NewGuid(),
                authorId,
                $"short-search-{Guid.NewGuid():N}",
                title,
                description,
                category.Id,
                10,
                20,
                4,
                RecipeDifficulty.Easy,
                "Hướng dẫn",
                null);
            recipe.Publish(DateTimeOffset.UtcNow, true, 1, [1]);
            dbContext.Recipes.Add(recipe);
        }

        var draft = CreateRecipe(
            authorId,
            category.Id,
            $"draft-ga-{Guid.NewGuid():N}",
            "Gà bí mật",
            10,
            20,
            4,
            RecipeDifficulty.Easy);
        dbContext.Recipes.Add(draft);
        await dbContext.SaveChangesAsync();

        var result = await this.sharedClient.GetFromJsonAsync<RecipePage>(
            $"/api/v1/recipes/search?q={Uri.EscapeDataString(search)}&category={category.Slug}&pageSize=50");

        Assert.NotNull(result);
        Assert.Equal(1, result.Meta.Total);
        Assert.Equal(
            examples.Take(1).Select(example => example.Title).Order(),
            result.Data.Select(recipe => recipe.Title).Order());
        Assert.All(result.Data, recipe => Assert.Equal("published", recipe.Status));

        var prefix = await this.sharedClient.GetFromJsonAsync<RecipePage>(
            $"/api/v1/recipes/search?q=ph&category={category.Slug}&pageSize=50");
        Assert.NotNull(prefix);
        Assert.Contains(prefix.Data, recipe => recipe.Title == "Phở bò truyền thống");
        Assert.DoesNotContain(prefix.Data, recipe => recipe.Status != "published");
    }

    /// <summary>Verifies that vietnamese search requires all words and prefers accurate matches.</summary>
    /// <param name="search">The search supplied to this operation.</param>
    /// <param name="expectedTotal">The expected total supplied to this operation.</param>
    /// <param name="expectedFirst">The expected first supplied to this operation.</param>
    /// <returns>A task representing the operation and its result.</returns>
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
        var data = await this.SeedDiscoveryDataAsync();
        await using var scope = factory.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var category = await dbContext.Categories.SingleAsync(item => item.Id == data.VietnameseCategoryId);
        string authorId = await dbContext.Recipes.Where(recipe => recipe.CategoryId == category.Id)
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
        foreach (var (title, description) in examples)
        {
            var recipe = Recipe.Create(
                Guid.NewGuid(),
                authorId,
                $"vietnamese-{Guid.NewGuid():N}",
                title,
                description,
                category.Id,
                10,
                20,
                4,
                RecipeDifficulty.Easy,
                "Hướng dẫn",
                null);
            recipe.Publish(DateTimeOffset.UtcNow, true, 1, [1]);
            dbContext.Recipes.Add(recipe);
        }

        await dbContext.SaveChangesAsync();

        var result = await this.sharedClient.GetFromJsonAsync<RecipePage>(
            $"/api/v1/recipes/search?q={Uri.EscapeDataString(search)}&category={category.Slug}&pageSize=50");
        Assert.NotNull(result);
        Assert.Equal(expectedTotal, result.Meta.Total);
        Assert.All(result.Data, recipe => Assert.Equal("published", recipe.Status));
        if (expectedFirst is not null)
        {
            Assert.Equal(expectedFirst, result.Data[0].Title);
        }

        if (search.Contains("nu", StringComparison.OrdinalIgnoreCase))
        {
            Assert.DoesNotContain(result.Data, recipe => recipe.Title == "Nem lụi Huế");
        }
    }

    /// <summary>Verifies that redis outage does not crash public read api.</summary>
    /// <returns>A task representing the operation and its result.</returns>
    [Fact]
    public async Task RedisOutageDoesNotCrashPublicReadApi()
    {
        var outageFactory = new AuthApiFactory();
        await outageFactory.InitializeAsync();
        try
        {
            using var client = outageFactory.CreateClient();
            var data = await this.SeedDiscoveryDataAsync(outageFactory);
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
            await ((IAsyncDisposable)outageFactory).DisposeAsync();
            await ((IAsyncLifetime)outageFactory).DisposeAsync();
        }
    }

    /// <summary>Verifies that discovery migration creates required extensions and indexes.</summary>
    /// <returns>A task representing the operation and its result.</returns>
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

    private static Recipe CreateRecipe(
        string authorId,
        Guid categoryId,
        string slug,
        string title,
        int prepTime,
        int cookTime,
        int servings,
        RecipeDifficulty difficulty,
        string description = "Mô tả công thức dùng cho kiểm thử discovery.") =>
        Recipe.Create(
            Guid.NewGuid(),
            authorId,
            slug,
            title,
            description,
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

    private async Task<DiscoverySeed> SeedDiscoveryDataAsync(AuthApiFactory? targetFactory = null)
    {
        await using var scope = (targetFactory ?? factory).Services.CreateAsyncScope();
        var services = scope.ServiceProvider;
        var dbContext = services.GetRequiredService<AppDbContext>();
        var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
        string suffix = Guid.NewGuid().ToString("N");
        var author = new ApplicationUser
        {
            Id = Guid.NewGuid().ToString(),
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
        var earlier = DateTimeOffset.UtcNow.AddDays(-3);
        var later = DateTimeOffset.UtcNow.AddDays(-2);
        await dbContext.Recipes.Where(recipe => recipe.Id == pho.Id || recipe.Id == bunBo.Id)
            .ExecuteUpdateAsync(setters => setters.SetProperty(recipe => recipe.CreatedAt, recipe => recipe.Id == pho.Id ? earlier : later));
        return new DiscoverySeed(vietnamese.Id, vietnamese.Slug);
    }

    private sealed record DiscoverySeed(Guid VietnameseCategoryId, string VietnameseSlug);

    private sealed record RecipePage(IReadOnlyList<RecipeItem> Data, PageMeta Meta, string? Message);

    private sealed record RecipeItem(string Title, string Status, float? RelevanceScore);

    private sealed record RecipePageWithId(IReadOnlyList<RecipeItemWithId> Data, PageMeta Meta);

    private sealed record RecipeItemWithId(Guid Id, string Title, string Status);

    private sealed record DataEnvelope<T>(T Data);

    private sealed record AuthSession(string AccessToken, AuthUser User);

    private sealed record AuthUser(string Id, string Email);

    private sealed record PageMeta(int Page, int PageSize, int Total, int TotalPages, bool HasNextPage);
}
