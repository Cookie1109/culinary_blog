using CulinaryBlog.Application.Abstractions.Persistence;
using CulinaryBlog.Application.Content;
using CulinaryBlog.Application.Features.Recipes.Queries;
using CulinaryBlog.Domain.Recipes;

namespace CulinaryBlog.UnitTests;

public sealed class SearchPublishedRecipesQueryTests
{
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
            MaxCookTime: 45,
            MinServings: 2,
            MinPrepTime: 10,
            MaxPrepTime: 60,
            Sort: "newest",
            Page: 1,
            PageSize: 10);
        using var cts = new CancellationTokenSource();

        var result = await handler.Handle(query, cts.Token);

        Assert.Same(expectedEnvelope, result);
        Assert.Equal("phở bò", fakeRepo.LastSearch);
        Assert.Equal("mon-nuoc", fakeRepo.LastCategory);
        Assert.Equal(RecipeDifficulty.Medium, fakeRepo.LastDifficulty);
        Assert.Equal(45, fakeRepo.LastMaxCookTime);
        Assert.Equal(2, fakeRepo.LastMinServings);
        Assert.Equal(10, fakeRepo.LastMinPrepTime);
        Assert.Equal(60, fakeRepo.LastMaxPrepTime);
        Assert.Equal("newest", fakeRepo.LastSort);
        Assert.Equal(1, fakeRepo.LastPage);
        Assert.Equal(10, fakeRepo.LastPageSize);
        Assert.Equal(cts.Token, fakeRepo.LastCancellationToken);
    }

    [Fact]
    public async Task HandlerPropagatesExceptionWhenRepositoryFails()
    {
        var fakeRepo = new ThrowingRecipeSearchRepository(new InvalidOperationException("db failure"));
        var handler = new SearchPublishedRecipesQueryHandler(fakeRepo);
        var query = new SearchPublishedRecipesQuery(
            Search: "test",
            Category: null,
            Difficulty: null,
            MaxCookTime: null,
            MinServings: null,
            MinPrepTime: null,
            MaxPrepTime: null,
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

        public int? LastMaxCookTime { get; private set; }

        public int? LastMinServings { get; private set; }

        public int? LastMinPrepTime { get; private set; }

        public int? LastMaxPrepTime { get; private set; }

        public string? LastSort { get; private set; }

        public int LastPage { get; private set; }

        public int LastPageSize { get; private set; }

        public CancellationToken LastCancellationToken { get; private set; }

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
            CancellationToken cancellationToken) =>
            Task.FromResult(result);

        public Task<PageEnvelope<RecipeDto>> SearchPublishedRecipesAsync(
            string? search,
            string? category,
            RecipeDifficulty? difficulty,
            int? maxCookTime,
            int? minServings,
            int? minPrepTime,
            int? maxPrepTime,
            string? sort,
            int page,
            int pageSize,
            CancellationToken cancellationToken)
        {
            LastSearch = search;
            LastCategory = category;
            LastDifficulty = difficulty;
            LastMaxCookTime = maxCookTime;
            LastMinServings = minServings;
            LastMinPrepTime = minPrepTime;
            LastMaxPrepTime = maxPrepTime;
            LastSort = sort;
            LastPage = page;
            LastPageSize = pageSize;
            LastCancellationToken = cancellationToken;
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
            CancellationToken cancellationToken) =>
            Task.FromException<PageEnvelope<RecipeDto>>(exception);

        public Task<PageEnvelope<RecipeDto>> SearchPublishedRecipesAsync(
            string? search,
            string? category,
            RecipeDifficulty? difficulty,
            int? maxCookTime,
            int? minServings,
            int? minPrepTime,
            int? maxPrepTime,
            string? sort,
            int page,
            int pageSize,
            CancellationToken cancellationToken) =>
            Task.FromException<PageEnvelope<RecipeDto>>(exception);
    }
}
