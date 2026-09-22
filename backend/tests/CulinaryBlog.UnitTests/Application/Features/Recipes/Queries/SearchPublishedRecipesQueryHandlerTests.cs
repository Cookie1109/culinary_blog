using System.Threading;
using System.Threading.Tasks;
using CulinaryBlog.Application;
using CulinaryBlog.Application.Features.Recipes.Queries;
using CulinaryBlog.Domain.Recipes;
using NSubstitute;
using Xunit;

namespace CulinaryBlog.UnitTests.Application.Features.Recipes.Queries;

public class SearchPublishedRecipesQueryHandlerTests
{
    private readonly IRecipeSearchRepository _repository;
    private readonly SearchPublishedRecipesQueryHandler _handler;

    public SearchPublishedRecipesQueryHandlerTests()
    {
        _repository = Substitute.For<IRecipeSearchRepository>();
        _handler = new SearchPublishedRecipesQueryHandler(_repository);
    }

    [Fact]
    public async Task Handle_Should_Call_Repository_With_Correct_Arguments()
    {
        // Arrange
        var request = new SearchPublishedRecipesQuery(
            Search: "pasta",
            Category: "Italian",
            Difficulty: RecipeDifficulty.Medium,
            MaxTime: 45,
            Sort: "newest",
            Page: 2,
            PageSize: 10
        );

        var expectedResult = new PageEnvelope<RecipeDto>(
            [], 
            0,
            2,
            10
        );

        _repository.SearchPublishedRecipesAsync(
            request.Search,
            request.Category,
            request.Difficulty,
            request.MaxTime,
            request.Sort,
            request.Page,
            request.PageSize,
            Arg.Any<CancellationToken>()
        ).Returns(expectedResult);

        // Act
        var result = await _handler.Handle(request, CancellationToken.None);

        // Assert
        Assert.Same(expectedResult, result);
        await _repository.Received(1).SearchPublishedRecipesAsync(
            "pasta",
            "Italian",
            RecipeDifficulty.Medium,
            45,
            "newest",
            2,
            10,
            Arg.Any<CancellationToken>()
        );
    }
}
