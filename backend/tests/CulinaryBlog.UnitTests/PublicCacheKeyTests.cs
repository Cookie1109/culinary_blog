using CulinaryBlog.Application.Abstractions.Caching;
using CulinaryBlog.Application.Features.Recipes.Queries;
using CulinaryBlog.Domain.Recipes;

namespace CulinaryBlog.UnitTests;

public sealed class PublicCacheKeyTests
{
    [Fact]
    public void SearchKeyNormalizesTextAndContainsEveryParameter()
    {
        var first = new SearchPublishedRecipesQuery(
            "  PHỞ   BÒ ", " MÓN-VIỆT ", RecipeDifficulty.Easy, 45, "newest", 2, 24);
        var second = new SearchPublishedRecipesQuery(
            "phở bò", "món-việt", RecipeDifficulty.Easy, 45, "newest", 2, 24);

        Assert.Equal(first.CacheKey, second.CacheKey);
        Assert.StartsWith("cb:v1:search:", first.CacheKey, StringComparison.Ordinal);
        Assert.Contains("q=ph%E1%BB%9F%20b%C3%B2", first.CacheKey, StringComparison.Ordinal);
        Assert.Contains("category=m%C3%B3n-vi%E1%BB%87t", first.CacheKey, StringComparison.Ordinal);
        Assert.Contains("difficulty=easy", first.CacheKey, StringComparison.Ordinal);
        Assert.Contains("maxTime=45", first.CacheKey, StringComparison.Ordinal);
        Assert.Contains("sort=newest", first.CacheKey, StringComparison.Ordinal);
        Assert.Contains("page=2", first.CacheKey, StringComparison.Ordinal);
        Assert.Contains("pageSize=24", first.CacheKey, StringComparison.Ordinal);
        Assert.Contains("locale=default", first.CacheKey, StringComparison.Ordinal);
        Assert.Equal(TimeSpan.FromMinutes(1), first.CacheDuration);
        Assert.Contains(PublicCacheKeys.RecipesTag, first.CacheTags);
    }

    [Fact]
    public void RecipeListKeySeparatesDifferentFiltersAndUsesFifteenMinuteTtl()
    {
        var categoryId = Guid.NewGuid();
        var first = new ListPublishedRecipesQuery(
            categoryId, RecipeDifficulty.Medium, 30, 4, 5, 15, "title", 1, 12);
        var differentPage = first with { Page = 2 };

        Assert.NotEqual(first.CacheKey, differentPage.CacheKey);
        Assert.StartsWith("cb:v1:recipe-list:", first.CacheKey, StringComparison.Ordinal);
        Assert.Contains($"categoryId={categoryId:N}", first.CacheKey, StringComparison.Ordinal);
        Assert.Contains("difficulty=medium", first.CacheKey, StringComparison.Ordinal);
        Assert.Contains("maxCookTime=30", first.CacheKey, StringComparison.Ordinal);
        Assert.Contains("minServings=4", first.CacheKey, StringComparison.Ordinal);
        Assert.Contains("minPrepTime=5", first.CacheKey, StringComparison.Ordinal);
        Assert.Contains("maxPrepTime=15", first.CacheKey, StringComparison.Ordinal);
        Assert.Equal(TimeSpan.FromMinutes(15), first.CacheDuration);
        Assert.Contains(PublicCacheKeys.CategoriesTag, first.CacheTags);
    }

    [Fact]
    public void DetailAndCategoryKeysUsePublicVersionedNamespace()
    {
        Assert.Equal("cb:v1:recipe:slug=pho-bo&locale=default", PublicCacheKeys.Recipe(" PHO-BO "));
        Assert.Equal("cb:v1:categories:locale=default", PublicCacheKeys.Categories());
        Assert.Equal(
            "cb:v1:category:slug=mon-viet&page=1&pageSize=12&locale=default",
            PublicCacheKeys.Category("Mon-Viet", 1, 12));
    }
}
