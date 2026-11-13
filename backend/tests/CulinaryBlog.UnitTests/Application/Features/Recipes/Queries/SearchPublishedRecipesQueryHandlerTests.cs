// Culinary Blog. Authorship is recorded in the repository history.

using System.Threading;
using System.Threading.Tasks;
using CulinaryBlog.Application.Abstractions.Persistence;
using CulinaryBlog.Application.Content;
using CulinaryBlog.Application.Features.Recipes.Queries;
using CulinaryBlog.Domain.Recipes;
using NSubstitute;
using Xunit;

namespace CulinaryBlog.UnitTests.Application.Features.Recipes.Queries;

/// <summary>Contains regression checks for search published recipes query handler tests.</summary>
public class SearchPublishedRecipesQueryHandlerTests
{
    private readonly IRecipeSearchRepository repository;
    private readonly SearchPublishedRecipesQueryHandler handler;

    /// <summary>Initializes a new instance of the <see cref="SearchPublishedRecipesQueryHandlerTests"/> class.</summary>
    public SearchPublishedRecipesQueryHandlerTests()
    {
        this.repository = Substitute.For<IRecipeSearchRepository>();
        this.handler = new SearchPublishedRecipesQueryHandler(this.repository);
    }

    /// <summary>Verifies that handle calls repository with correct arguments.</summary>
    /// <returns>A task representing the operation and its result.</returns>
    [Fact]
    public async Task HandleCallsRepositoryWithCorrectArguments()
    {
        // Arrange
        var request = new SearchPublishedRecipesQuery(
            Search: "pasta",
            Category: "Italian",
            Difficulty: RecipeDifficulty.Medium,
            MaxTime: 45,
            Sort: "newest",
            Page: 2,
            PageSize: 10);

        var expectedResult = new PageEnvelope<RecipeDto>(
            [],
            new PageMeta(2, 10, 0, 0, false, true));

        this.repository.SearchPublishedRecipesAsync(
            request.Search,
            request.Category,
            request.Difficulty,
            request.MaxTime,
            request.CanonicalSort,
            request.Page,
            request.PageSize,
            request.CategoryId,
            request.MaxCookTime,
            request.MinServings,
            request.MinPrepTime,
            request.MaxPrepTime,
            Arg.Any<CancellationToken>())
        .Returns(expectedResult);

        // Act
        var result = await this.handler.Handle(request, CancellationToken.None);

        // Assert
        Assert.Same(expectedResult, result);
        await this.repository.Received(1).SearchPublishedRecipesAsync(
            "pasta",
            "Italian",
            RecipeDifficulty.Medium,
            45,
            "-createdAt",
            2,
            10,
            null,
            null,
            null,
            null,
            null,
            Arg.Any<CancellationToken>());
    }
}
