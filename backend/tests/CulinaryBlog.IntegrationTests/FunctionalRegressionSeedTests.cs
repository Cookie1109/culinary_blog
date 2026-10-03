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
    public async Task Phase8SeedIsSyntheticCompleteAndIdempotent()
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

        Assert.Equal(5, authors.Count);
        Assert.All(authors, author =>
        {
            Assert.EndsWith("@example.test", author.Email, StringComparison.Ordinal);
            Assert.Null(author.PasswordHash);
        });
        Assert.Equal(20, await dbContext.Categories.IgnoreQueryFilters().CountAsync());
        Assert.Equal(50, recipes.Count);
        Assert.All(recipes, recipe => Assert.Contains(recipe.AuthorId, authorIds));
        Assert.Contains(recipes, recipe => recipe.Status == RecipeStatus.Draft);
        Assert.Contains(recipes, recipe => recipe.Status == RecipeStatus.Published);
        Assert.Contains(recipes, recipe => recipe.Status == RecipeStatus.Archived);
    }
}
