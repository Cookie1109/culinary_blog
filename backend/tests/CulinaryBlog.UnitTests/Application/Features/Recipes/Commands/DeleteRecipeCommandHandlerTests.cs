using System;
using System.Threading;
using System.Threading.Tasks;
using CulinaryBlog.Application.Abstractions.Persistence;
using CulinaryBlog.Application.Content;
using CulinaryBlog.Application.Features.Recipes.Commands;
using CulinaryBlog.Domain.Recipes;
using NSubstitute;
using Xunit;

namespace CulinaryBlog.UnitTests.Application.Features.Recipes.Commands;

public class DeleteRecipeCommandHandlerTests
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IRecipeRepository _recipeRepository;
    private readonly DeleteRecipeCommandHandler _handler;

    public DeleteRecipeCommandHandlerTests()
    {
        _unitOfWork = Substitute.For<IUnitOfWork>();
        _recipeRepository = Substitute.For<IRecipeRepository>();
        _unitOfWork.Recipes.Returns(_recipeRepository);
        _handler = new DeleteRecipeCommandHandler(_unitOfWork);
    }

    [Fact]
    public async Task Handle_Should_Throw_ContentProblemException_NotFound_When_RecipeDoesNotExist()
    {
        // Arrange
        var command = new DeleteRecipeCommand(Guid.NewGuid(), Guid.NewGuid(), false, 1);
        _recipeRepository.GetByIdAsync(command.Id, Arg.Any<CancellationToken>()).Returns((Recipe?)null);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ContentProblemException>(() => _handler.Handle(command, CancellationToken.None));
        Assert.Equal("RECIPE_NOT_FOUND", exception.Code);
        Assert.Equal(ContentProblemKind.NotFound, exception.Kind);
    }

    [Fact]
    public async Task Handle_Should_Throw_ContentProblemException_Forbidden_When_UserIsNotAuthorAndNotAdmin()
    {
        // Arrange
        var command = new DeleteRecipeCommand(Guid.NewGuid(), Guid.NewGuid(), false, 1);
        var recipe = Recipe.Create(command.Id, Guid.NewGuid(), "slug", "Title", "Desc", Guid.NewGuid(), 10, 20, 2, RecipeDifficulty.Easy, [], null);
        _recipeRepository.GetByIdAsync(command.Id, Arg.Any<CancellationToken>()).Returns(recipe);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ContentProblemException>(() => _handler.Handle(command, CancellationToken.None));
        Assert.Equal("FORBIDDEN", exception.Code);
        Assert.Equal(ContentProblemKind.Forbidden, exception.Kind);
    }

    [Fact]
    public async Task Handle_Should_Throw_ContentProblemException_Conflict_When_ExpectedVersionDoesNotMatch()
    {
        // Arrange
        var authorId = Guid.NewGuid();
        var command = new DeleteRecipeCommand(Guid.NewGuid(), authorId, false, 2);
        var recipe = Recipe.Create(command.Id, authorId, "slug", "Title", "Desc", Guid.NewGuid(), 10, 20, 2, RecipeDifficulty.Easy, [], null);
        // Default version is 1, so expected 2 will fail.
        _recipeRepository.GetByIdAsync(command.Id, Arg.Any<CancellationToken>()).Returns(recipe);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ContentProblemException>(() => _handler.Handle(command, CancellationToken.None));
        Assert.Equal("RECIPE_CONCURRENCY_CONFLICT", exception.Code);
        Assert.Equal(ContentProblemKind.Conflict, exception.Kind);
        Assert.Equal(recipe.Version, exception.CurrentVersion);
    }

    [Fact]
    public async Task Handle_Should_DeleteRecipe_And_SaveChanges_When_CommandIsValid_And_UserIsAuthor()
    {
        // Arrange
        var authorId = Guid.NewGuid();
        var command = new DeleteRecipeCommand(Guid.NewGuid(), authorId, false, 1);
        var recipe = Recipe.Create(command.Id, authorId, "slug", "Title", "Desc", Guid.NewGuid(), 10, 20, 2, RecipeDifficulty.Easy, [], null);
        _recipeRepository.GetByIdAsync(command.Id, Arg.Any<CancellationToken>()).Returns(recipe);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.True(recipe.IsDeleted);
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_Should_DeleteRecipe_And_SaveChanges_When_CommandIsValid_And_UserIsAdmin()
    {
        // Arrange
        var adminId = Guid.NewGuid();
        var authorId = Guid.NewGuid(); // Different from admin
        var command = new DeleteRecipeCommand(Guid.NewGuid(), adminId, true, 1);
        var recipe = Recipe.Create(command.Id, authorId, "slug", "Title", "Desc", Guid.NewGuid(), 10, 20, 2, RecipeDifficulty.Easy, [], null);
        _recipeRepository.GetByIdAsync(command.Id, Arg.Any<CancellationToken>()).Returns(recipe);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.True(recipe.IsDeleted);
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
