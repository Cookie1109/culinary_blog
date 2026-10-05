using CulinaryBlog.Domain.Categories;
using CulinaryBlog.Domain.Recipes;
using CulinaryBlog.Infrastructure.Identity;
using CulinaryBlog.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace CulinaryBlog.IntegrationTests;

public sealed class FunctionalRegressionSeedTests(AuthApiFactory factory) : IClassFixture<AuthApiFactory>
{
    [Fact]
    public async Task VietnameseSeedIsCompleteMeaningfulAndIdempotent()
    {
        await using var scope = factory.Services.CreateAsyncScope();
        var services = scope.ServiceProvider;
        var dbContext = services.GetRequiredService<AppDbContext>();
        var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();

        await services.GetRequiredService<DatabaseInitializer>().InitializeAsync(CancellationToken.None);

        var authors = (await userManager.GetUsersInRoleAsync("Author"))
            .Where(user => user.Email?.StartsWith("phase8-author-", StringComparison.Ordinal) == true)
            .OrderBy(user => user.Email)
            .ToList();
        var authorIds = authors.Select(user => user.Id).ToHashSet();
        var recipes = await dbContext.Recipes.IgnoreQueryFilters().ToListAsync();
        var ingredients = await dbContext.RecipeIngredients.IgnoreQueryFilters().ToListAsync();
        var steps = await dbContext.RecipeSteps.IgnoreQueryFilters().ToListAsync();
        var images = await dbContext.RecipeImages.IgnoreQueryFilters().ToListAsync();

        Assert.Equal(5, authors.Count);
        Assert.All(authors, author =>
        {
            Assert.EndsWith("@example.test", author.Email, StringComparison.Ordinal);
            Assert.Null(author.PasswordHash);
            Assert.False(string.IsNullOrWhiteSpace(author.Bio));
        });
        Assert.Equal(20, await dbContext.Categories.IgnoreQueryFilters().CountAsync());
        Assert.Equal(100, recipes.Count);
        Assert.All(recipes, recipe => Assert.Contains(recipe.AuthorId, authorIds));
        Assert.All(recipes, recipe => Assert.Equal(RecipeStatus.Published, recipe.Status));
        Assert.Equal(100, recipes.Select(recipe => recipe.Slug).Distinct(StringComparer.Ordinal).Count());
        Assert.All(recipes, recipe => Assert.True(ingredients.Count(item => item.RecipeId == recipe.Id) >= 10));
        Assert.All(recipes, recipe => Assert.True(steps.Count(item => item.RecipeId == recipe.Id) >= 5));
        Assert.Equal(100, images.Count);
        Assert.All(images, image =>
        {
            Assert.True(image.IsPrimary);
            Assert.EndsWith("/original.jpg", image.ObjectKey, StringComparison.Ordinal);
        });

        var legacyCategory = Category.Create(
            Guid.NewGuid(),
            "Món chính",
            "mon-chinh",
            null,
            null,
            20);
        dbContext.Categories.Add(legacyCategory);
        dbContext.Recipes.Add(Recipe.Create(
            Guid.NewGuid(),
            authors[0].Id,
            "mon-mau-ngau-nhien",
            "Món mẫu ngẫu nhiên",
            "Nội dung mẫu cũ cần được loại bỏ.",
            legacyCategory.Id,
            10,
            10,
            2,
            RecipeDifficulty.Easy,
            null,
            null));
        await dbContext.SaveChangesAsync();

        await services.GetRequiredService<DatabaseInitializer>().InitializeAsync(CancellationToken.None);

        Assert.Equal(20, await dbContext.Categories.IgnoreQueryFilters().CountAsync());
        Assert.Equal(100, await dbContext.Recipes.IgnoreQueryFilters().CountAsync());
        Assert.Equal(100, await dbContext.RecipeImages.IgnoreQueryFilters().CountAsync());
    }
}
