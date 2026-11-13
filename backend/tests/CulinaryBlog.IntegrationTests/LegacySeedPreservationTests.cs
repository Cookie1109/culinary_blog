// Culinary Blog. Authorship is recorded in the repository history.

using CulinaryBlog.Domain.Categories;
using CulinaryBlog.Domain.Recipes;
using CulinaryBlog.Infrastructure.Identity;
using CulinaryBlog.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace CulinaryBlog.IntegrationTests;

/// <summary>Contains regression checks for legacy seed preservation tests.</summary>
/// <param name="factory">The factory supplied to this operation.</param>
public sealed class LegacySeedPreservationTests(AuthApiFactory factory) : IClassFixture<AuthApiFactory>
{
    /// <summary>Verifies that restart retains earlier hundred recipe installation and its original ids.</summary>
    /// <returns>A task representing the operation and its result.</returns>
    [Fact]
    public async Task RestartRetainsEarlierHundredRecipeInstallationAndItsOriginalIds()
    {
        await using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var initializer = scope.ServiceProvider.GetRequiredService<DatabaseInitializer>();
        await initializer.InitializeAsync(CancellationToken.None);
        var existing = await db.Recipes.Select(recipe => recipe.Slug).ToListAsync();
        var authorId = await db.Users.Select(user => user.Id).FirstAsync();
        var categories = await db.Categories.ToDictionaryAsync(category => category.Slug, category => category.Id);
        foreach (var seed in VietnameseSeedData.Recipes.Where(recipe => !existing.Contains(recipe.Slug)))
        {
            var digest = System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes($"culinary-blog-seed:recipe:{seed.Slug}"));
            var recipe = Recipe.Create(
                new Guid(digest.AsSpan(0, 16)),
                authorId,
                seed.Slug,
                seed.Title,
                seed.Description,
                categories[seed.CategorySlug],
                seed.PrepTime,
                seed.CookTime,
                seed.Servings,
                seed.Difficulty,
                "Retained legacy instructions",
                null);
            db.Recipes.Add(recipe);
        }

        await db.SaveChangesAsync();
        var original = await db.Recipes.ToDictionaryAsync(recipe => recipe.Id, recipe => recipe.Title);
        Assert.Equal(100, original.Count);
        await initializer.InitializeAsync(CancellationToken.None);
        db.ChangeTracker.Clear();
        var retained = await db.Recipes.ToDictionaryAsync(recipe => recipe.Id, recipe => recipe.Title);
        Assert.Equal(original.OrderBy(item => item.Key), retained.OrderBy(item => item.Key));
    }
}
