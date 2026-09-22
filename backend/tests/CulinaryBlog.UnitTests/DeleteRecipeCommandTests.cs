using CulinaryBlog.Application.Abstractions.Persistence;
using CulinaryBlog.Application.Content;
using CulinaryBlog.Application.Features.Recipes.Commands;
using CulinaryBlog.Domain.Recipes;

namespace CulinaryBlog.UnitTests;

public sealed class DeleteRecipeCommandTests
{
    [Fact]
    public async Task OwnerCanSoftDeleteRecipe()
    {
        var ownerId = Guid.NewGuid();
        var recipe = CreateRecipe(ownerId);
        var unitOfWork = new FakeUnitOfWork(recipe);

        await new DeleteRecipeCommandHandler(unitOfWork).Handle(
            new DeleteRecipeCommand(recipe.Id, ownerId, false, recipe.Version), CancellationToken.None);

        Assert.True(recipe.IsDeleted);
        Assert.Equal(1, unitOfWork.SaveCount);
    }

    [Fact]
    public async Task OtherAuthorCannotDeleteRecipe()
    {
        var recipe = CreateRecipe(Guid.NewGuid());
        var unitOfWork = new FakeUnitOfWork(recipe);

        var exception = await Assert.ThrowsAsync<ContentProblemException>(() =>
            new DeleteRecipeCommandHandler(unitOfWork).Handle(
                new DeleteRecipeCommand(recipe.Id, Guid.NewGuid(), false, recipe.Version), CancellationToken.None));

        Assert.Equal(ContentProblemKind.Forbidden, exception.Kind);
        Assert.False(recipe.IsDeleted);
        Assert.Equal(0, unitOfWork.SaveCount);
    }

    [Fact]
    public async Task StaleVersionDoesNotDeleteRecipe()
    {
        var ownerId = Guid.NewGuid();
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

    private static Recipe CreateRecipe(Guid ownerId) => Recipe.Create(
        Guid.NewGuid(), ownerId, "sample-recipe", "Sample recipe", "Description",
        Guid.NewGuid(), 10, 10, 2, RecipeDifficulty.Easy, null, null);

    private sealed class FakeUnitOfWork(Recipe recipe) : IUnitOfWork
    {
        public IRecipeRepository Recipes { get; } = new FakeRecipeRepository(recipe);

        public int SaveCount { get; private set; }

        public Task<int> SaveChangesAsync(CancellationToken cancellationToken)
        {
            SaveCount++;
            return Task.FromResult(1);
        }
    }

    private sealed class FakeRecipeRepository(Recipe recipe) : IRecipeRepository
    {
        public Task<Recipe?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
            Task.FromResult<Recipe?>(id == recipe.Id ? recipe : null);

        public Task<bool> SlugExistsAsync(string slug, Guid? excludedId, CancellationToken cancellationToken) =>
            Task.FromResult(false);

        public void Add(Recipe entity) => throw new NotSupportedException();
    }
}
