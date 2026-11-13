// Culinary Blog. Authorship is recorded in the repository history.

using CulinaryBlog.Application.Abstractions.Persistence;
using CulinaryBlog.Application.Content;
using CulinaryBlog.Application.Features.Recipes.Commands;
using CulinaryBlog.Domain.Recipes;

namespace CulinaryBlog.UnitTests;

/// <summary>Contains regression checks for delete recipe command tests.</summary>
public sealed class DeleteRecipeCommandTests
{
    /// <summary>Verifies that owner can soft delete recipe.</summary>
    /// <returns>A task representing the operation and its result.</returns>
    [Fact]
    public async Task OwnerCanSoftDeleteRecipe()
    {
        var ownerId = Guid.NewGuid().ToString();
        var recipe = CreateRecipe(ownerId);
        var unitOfWork = new FakeUnitOfWork(recipe);

        await new DeleteRecipeCommandHandler(unitOfWork).Handle(
            new DeleteRecipeCommand(recipe.Id, ownerId, false, recipe.Version), CancellationToken.None);

        Assert.True(recipe.IsDeleted);
        Assert.Equal(1, unitOfWork.SaveCount);
    }

    /// <summary>Verifies that other author cannot delete recipe.</summary>
    /// <returns>A task representing the operation and its result.</returns>
    [Fact]
    public async Task OtherAuthorCannotDeleteRecipe()
    {
        var recipe = CreateRecipe(Guid.NewGuid().ToString());
        var unitOfWork = new FakeUnitOfWork(recipe);

        var exception = await Assert.ThrowsAsync<ContentProblemException>(() =>
            new DeleteRecipeCommandHandler(unitOfWork).Handle(
                new DeleteRecipeCommand(recipe.Id, Guid.NewGuid().ToString(), false, recipe.Version), CancellationToken.None));

        Assert.Equal(ContentProblemKind.Forbidden, exception.Kind);
        Assert.False(recipe.IsDeleted);
        Assert.Equal(0, unitOfWork.SaveCount);
    }

    /// <summary>Verifies that stale version does not delete recipe.</summary>
    /// <returns>A task representing the operation and its result.</returns>
    [Fact]
    public async Task StaleVersionDoesNotDeleteRecipe()
    {
        var ownerId = Guid.NewGuid().ToString();
        var recipe = CreateRecipe(ownerId);
        var unitOfWork = new FakeUnitOfWork(recipe);

        var exception = await Assert.ThrowsAsync<ContentProblemException>(() =>
            new DeleteRecipeCommandHandler(unitOfWork).Handle(
                new DeleteRecipeCommand(recipe.Id, ownerId, false, recipe.Version + 1), CancellationToken.None));

        Assert.Equal("RECIPE_CONCURRENCY_CONFLICT", exception.Code);
        Assert.Equal(recipe.Version, exception.CurrentVersion);
        Assert.False(recipe.IsDeleted);
        Assert.Equal(0, unitOfWork.SaveCount);
    }

    /// <summary>Verifies that admin can soft delete recipe of other author.</summary>
    /// <returns>A task representing the operation and its result.</returns>
    [Fact]
    public async Task AdminCanSoftDeleteRecipeOfOtherAuthor()
    {
        var ownerId = Guid.NewGuid().ToString();
        var adminId = Guid.NewGuid();
        var recipe = CreateRecipe(ownerId);
        var unitOfWork = new FakeUnitOfWork(recipe);

        await new DeleteRecipeCommandHandler(unitOfWork).Handle(
            new DeleteRecipeCommand(recipe.Id, adminId.ToString(), true, recipe.Version), CancellationToken.None);

        Assert.True(recipe.IsDeleted);
        Assert.Equal(1, unitOfWork.SaveCount);
    }

    /// <summary>Verifies that non existent recipe throws not found.</summary>
    /// <returns>A task representing the operation and its result.</returns>
    [Fact]
    public async Task NonExistentRecipeThrowsNotFound()
    {
        var unitOfWork = new FakeUnitOfWork(null);

        var exception = await Assert.ThrowsAsync<ContentProblemException>(() =>
            new DeleteRecipeCommandHandler(unitOfWork).Handle(
                new DeleteRecipeCommand(Guid.NewGuid(), Guid.NewGuid().ToString(), false, 1), CancellationToken.None));

        Assert.Equal(ContentProblemKind.NotFound, exception.Kind);
        Assert.Equal("RECIPE_NOT_FOUND", exception.Code);
        Assert.Equal(0, unitOfWork.SaveCount);
    }

    private static Recipe CreateRecipe(string ownerId) => Recipe.Create(
        Guid.NewGuid(),
        ownerId,
        "sample-recipe",
        "Sample recipe",
        "Description",
        Guid.NewGuid(),
        10,
        10,
        2,
        RecipeDifficulty.Easy,
        null,
        null);

    private sealed class FakeUnitOfWork(Recipe? recipe) : IUnitOfWork
    {
        public IRecipeRepository Recipes { get; } = new FakeRecipeRepository(recipe);

        public int SaveCount { get; private set; }

        public Task<int> SaveChangesAsync(CancellationToken cancellationToken)
        {
            this.SaveCount++;
            return Task.FromResult(1);
        }
    }

    private sealed class FakeRecipeRepository(Recipe? recipe) : IRecipeRepository
    {
        public Task<Recipe?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
            Task.FromResult(recipe is not null && id == recipe.Id ? recipe : null);

        public Task<bool> SlugExistsAsync(string slug, Guid? excludedId, CancellationToken cancellationToken) =>
            Task.FromResult(false);

        public void Add(Recipe entity) => throw new NotSupportedException();
    }
}
