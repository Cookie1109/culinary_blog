// Culinary Blog. Authorship is recorded in the repository history.

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

/// <summary>Contains regression checks for delete recipe command handler tests.</summary>
public class DeleteRecipeCommandHandlerTests
{
    private readonly IUnitOfWork unitOfWork;
    private readonly IRecipeRepository recipeRepository;
    private readonly DeleteRecipeCommandHandler handler;

    /// <summary>Initializes a new instance of the <see cref="DeleteRecipeCommandHandlerTests"/> class.</summary>
    public DeleteRecipeCommandHandlerTests()
    {
        this.unitOfWork = Substitute.For<IUnitOfWork>();
        this.recipeRepository = Substitute.For<IRecipeRepository>();
        this.unitOfWork.Recipes.Returns(this.recipeRepository);
        this.handler = new DeleteRecipeCommandHandler(this.unitOfWork);
    }

    /// <summary>Verifies that handle throws not found when recipe does not exist.</summary>
    /// <returns>A task representing the operation and its result.</returns>
    [Fact]
    public async Task HandleThrowsNotFoundWhenRecipeDoesNotExist()
    {
        // Arrange
        var command = new DeleteRecipeCommand(Guid.NewGuid(), Guid.NewGuid().ToString(), false, 1);
        this.recipeRepository.GetByIdAsync(command.Id, Arg.Any<CancellationToken>()).Returns((Recipe?)null);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ContentProblemException>(() => this.handler.Handle(command, CancellationToken.None));
        Assert.Equal("RECIPE_NOT_FOUND", exception.Code);
        Assert.Equal(ContentProblemKind.NotFound, exception.Kind);
    }

    /// <summary>Verifies that handle throws forbidden when user is not author and not admin.</summary>
    /// <returns>A task representing the operation and its result.</returns>
    [Fact]
    public async Task HandleThrowsForbiddenWhenUserIsNotAuthorAndNotAdmin()
    {
        // Arrange
        var command = new DeleteRecipeCommand(Guid.NewGuid(), Guid.NewGuid().ToString(), false, 1);
        var recipe = Recipe.Create(command.Id, Guid.NewGuid().ToString(), "slug", "Title", "Desc", Guid.NewGuid(), 10, 20, 2, RecipeDifficulty.Easy, null, null);
        this.recipeRepository.GetByIdAsync(command.Id, Arg.Any<CancellationToken>()).Returns(recipe);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ContentProblemException>(() => this.handler.Handle(command, CancellationToken.None));
        Assert.Equal("FORBIDDEN", exception.Code);
        Assert.Equal(ContentProblemKind.Forbidden, exception.Kind);
    }

    /// <summary>Verifies that handle throws conflict when expected version does not match.</summary>
    /// <returns>A task representing the operation and its result.</returns>
    [Fact]
    public async Task HandleThrowsConflictWhenExpectedVersionDoesNotMatch()
    {
        // Arrange
        var authorId = Guid.NewGuid().ToString();
        var command = new DeleteRecipeCommand(Guid.NewGuid(), authorId, false, 2);
        var recipe = Recipe.Create(command.Id, authorId, "slug", "Title", "Desc", Guid.NewGuid(), 10, 20, 2, RecipeDifficulty.Easy, null, null);

        // Default version is 1, so expected 2 will fail.
        this.recipeRepository.GetByIdAsync(command.Id, Arg.Any<CancellationToken>()).Returns(recipe);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ContentProblemException>(() => this.handler.Handle(command, CancellationToken.None));
        Assert.Equal("RECIPE_CONCURRENCY_CONFLICT", exception.Code);
        Assert.Equal(ContentProblemKind.Conflict, exception.Kind);
        Assert.Equal(recipe.Version, exception.CurrentVersion);
    }

    /// <summary>Verifies that handle deletes recipe and saves changes when user is author.</summary>
    /// <returns>A task representing the operation and its result.</returns>
    [Fact]
    public async Task HandleDeletesRecipeAndSavesChangesWhenUserIsAuthor()
    {
        // Arrange
        var authorId = Guid.NewGuid().ToString();
        var command = new DeleteRecipeCommand(Guid.NewGuid(), authorId, false, 1);
        var recipe = Recipe.Create(command.Id, authorId, "slug", "Title", "Desc", Guid.NewGuid(), 10, 20, 2, RecipeDifficulty.Easy, null, null);
        this.recipeRepository.GetByIdAsync(command.Id, Arg.Any<CancellationToken>()).Returns(recipe);

        // Act
        await this.handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.True(recipe.IsDeleted);
        await this.unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    /// <summary>Verifies that handle deletes recipe and saves changes when user is admin.</summary>
    /// <returns>A task representing the operation and its result.</returns>
    [Fact]
    public async Task HandleDeletesRecipeAndSavesChangesWhenUserIsAdmin()
    {
        // Arrange
        var adminId = Guid.NewGuid();
        var authorId = Guid.NewGuid().ToString(); // Different from admin
        var command = new DeleteRecipeCommand(Guid.NewGuid(), adminId.ToString(), true, 1);
        var recipe = Recipe.Create(command.Id, authorId, "slug", "Title", "Desc", Guid.NewGuid(), 10, 20, 2, RecipeDifficulty.Easy, null, null);
        this.recipeRepository.GetByIdAsync(command.Id, Arg.Any<CancellationToken>()).Returns(recipe);

        // Act
        await this.handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.True(recipe.IsDeleted);
        await this.unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
