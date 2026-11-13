// Culinary Blog. Authorship is recorded in the repository history.

using CulinaryBlog.Application.Abstractions.Persistence;
using CulinaryBlog.Application.Content;
using CulinaryBlog.Application.Features.Recipes.Queries;
using CulinaryBlog.Domain.Recipes;

namespace CulinaryBlog.UnitTests;

/// <summary>Contains regression checks for search published recipes query tests.</summary>
public sealed class SearchPublishedRecipesQueryTests
{
    /// <summary>Verifies that handler delegates to repository with exact parameters and returns result.</summary>
    /// <returns>A task representing the operation and its result.</returns>
    [Fact]
    public async Task HandlerDelegatesToRepositoryWithExactParametersAndReturnsResult()
    {
        var expectedEnvelope = new PageEnvelope<RecipeDto>(
            [],
            new PageMeta(1, 10, 0, 0, false, false));
        var fakeRepo = new FakeRecipeSearchRepository(expectedEnvelope);
        var handler = new SearchPublishedRecipesQueryHandler(fakeRepo);
        var query = new SearchPublishedRecipesQuery(
            Search: "phở bò",
            Category: "mon-nuoc",
            Difficulty: RecipeDifficulty.Medium,
            MaxTime: 45,
            Sort: "newest",
            Page: 1,
            PageSize: 10,
            CategoryId: Guid.NewGuid(),
            MaxCookTime: 30,
            MinServings: 4,
            MinPrepTime: 5,
            MaxPrepTime: 20);
        using var cts = new CancellationTokenSource();

        var result = await handler.Handle(query, cts.Token);

        Assert.Same(expectedEnvelope, result);
        Assert.Equal("phở bò", fakeRepo.LastSearch);
        Assert.Equal("mon-nuoc", fakeRepo.LastCategory);
        Assert.Equal(RecipeDifficulty.Medium, fakeRepo.LastDifficulty);
        Assert.Equal(45, fakeRepo.LastMaxTime);
        Assert.Equal("-createdAt", fakeRepo.LastSort);
        Assert.Equal(1, fakeRepo.LastPage);
        Assert.Equal(10, fakeRepo.LastPageSize);
        Assert.Equal(cts.Token, fakeRepo.LastCancellationToken);
        Assert.Equal((query.CategoryId, 30, 4, 5, 20), fakeRepo.LastAdvancedFilters);
    }

    /// <summary>Verifies that handler propagates exception when repository fails.</summary>
    /// <returns>A task representing the operation and its result.</returns>
    [Fact]
    public async Task HandlerPropagatesExceptionWhenRepositoryFails()
    {
        var fakeRepo = new ThrowingRecipeSearchRepository(new InvalidOperationException("db failure"));
        var handler = new SearchPublishedRecipesQueryHandler(fakeRepo);
        var query = new SearchPublishedRecipesQuery(
            Search: "test",
            Category: null,
            Difficulty: null,
            MaxTime: null,
            Sort: null,
            Page: 1,
            PageSize: 10);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            handler.Handle(query, CancellationToken.None));

        Assert.Equal("db failure", exception.Message);
    }

    private sealed class FakeRecipeSearchRepository(PageEnvelope<RecipeDto> result) : IRecipeSearchRepository
    {
        public string? LastSearch { get; private set; }

        public string? LastCategory { get; private set; }

        public RecipeDifficulty? LastDifficulty { get; private set; }

        public int? LastMaxTime { get; private set; }

        public string? LastSort { get; private set; }

        public int LastPage { get; private set; }

        public int LastPageSize { get; private set; }

        public CancellationToken LastCancellationToken { get; private set; }
        public (Guid? CategoryId, int? MaxCookTime, int? MinServings, int? MinPrepTime, int? MaxPrepTime) LastAdvancedFilters { get; private set; }

        public Task<PageEnvelope<RecipeDto>> ListPublishedRecipesAsync(
            Guid? categoryId,
            RecipeDifficulty? difficulty,
            int? maxCookTime,
            int? minServings,
            int? minPrepTime,
            int? maxPrepTime,
            string? sort,
            int page,
            int pageSize,
            string? category,
            int? maxTime,
            CancellationToken cancellationToken) =>
            Task.FromResult(result);

        public Task<PageEnvelope<RecipeDto>> SearchPublishedRecipesAsync(
            string? search,
            string? category,
            RecipeDifficulty? difficulty,
            int? maxTime,
            string? sort,
            int page,
            int pageSize,
            Guid? categoryId,
            int? maxCookTime,
            int? minServings,
            int? minPrepTime,
            int? maxPrepTime,
            CancellationToken cancellationToken)
        {
            this.LastAdvancedFilters = (categoryId, maxCookTime, minServings, minPrepTime, maxPrepTime);
            this.LastSearch = search;
            this.LastCategory = category;
            this.LastDifficulty = difficulty;
            this.LastMaxTime = maxTime;
            this.LastSort = sort;
            this.LastPage = page;
            this.LastPageSize = pageSize;
            this.LastCancellationToken = cancellationToken;
            return Task.FromResult(result);
        }
    }

    private sealed class ThrowingRecipeSearchRepository(Exception exception) : IRecipeSearchRepository
    {
        public Task<PageEnvelope<RecipeDto>> ListPublishedRecipesAsync(
            Guid? categoryId,
            RecipeDifficulty? difficulty,
            int? maxCookTime,
            int? minServings,
            int? minPrepTime,
            int? maxPrepTime,
            string? sort,
            int page,
            int pageSize,
            string? category,
            int? maxTime,
            CancellationToken cancellationToken) =>
            Task.FromException<PageEnvelope<RecipeDto>>(exception);

        public Task<PageEnvelope<RecipeDto>> SearchPublishedRecipesAsync(
            string? search,
            string? category,
            RecipeDifficulty? difficulty,
            int? maxTime,
            string? sort,
            int page,
            int pageSize,
            Guid? categoryId,
            int? maxCookTime,
            int? minServings,
            int? minPrepTime,
            int? maxPrepTime,
            CancellationToken cancellationToken) =>
            Task.FromException<PageEnvelope<RecipeDto>>(exception);
    }
}
