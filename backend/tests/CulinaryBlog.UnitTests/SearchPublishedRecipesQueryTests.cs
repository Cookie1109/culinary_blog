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
            MaxTime: 45,
            Sort: "newest",
            Page: 1,
            PageSize: 10);
        using var cts = new CancellationTokenSource();

        var result = await handler.Handle(query, cts.Token);

        Assert.Same(expectedEnvelope, result);
        Assert.Equal("phở bò", fakeRepo.LastSearch);
        Assert.Equal("mon-nuoc", fakeRepo.LastCategory);
        Assert.Equal(RecipeDifficulty.Medium, fakeRepo.LastDifficulty);
        Assert.Equal(45, fakeRepo.LastMaxTime);
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

        public Task<PageEnvelope<RecipeDto>> SearchPublishedRecipesAsync(
            string? search,
            string? category,
            RecipeDifficulty? difficulty,
            int? maxTime,
            string? sort,
            int page,
            int pageSize,
            CancellationToken cancellationToken)
        {
            LastSearch = search;
            LastCategory = category;
            LastDifficulty = difficulty;
            LastMaxTime = maxTime;
            LastSort = sort;
            LastPage = page;
            LastPageSize = pageSize;
            LastCancellationToken = cancellationToken;
            return Task.FromResult(result);
        }
    }

    private sealed class ThrowingRecipeSearchRepository(Exception exception) : IRecipeSearchRepository
    {
        public Task<PageEnvelope<RecipeDto>> SearchPublishedRecipesAsync(
            string? search,
            string? category,
            RecipeDifficulty? difficulty,
            int? maxTime,
            string? sort,
            int page,
            int pageSize,
            CancellationToken cancellationToken) =>
            Task.FromException<PageEnvelope<RecipeDto>>(exception);
    }
}
