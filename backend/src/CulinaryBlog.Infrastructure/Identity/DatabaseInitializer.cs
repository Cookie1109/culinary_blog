// Culinary Blog. Authorship is recorded in the repository history.

using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Bogus;
using CulinaryBlog.Application.Abstractions.Caching;
using CulinaryBlog.Application.Media;
using CulinaryBlog.Domain.Categories;
using CulinaryBlog.Domain.Recipes;
using CulinaryBlog.Infrastructure.Jobs;
using CulinaryBlog.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace CulinaryBlog.Infrastructure.Identity;

/// <summary>Represents the database initializer.</summary>
/// <param name="dbContext">The db context supplied to this operation.</param>
/// <param name="roleManager">The role manager supplied to this operation.</param>
/// <param name="userManager">The user manager supplied to this operation.</param>
/// <param name="fileStorage">The file storage supplied to this operation.</param>
/// <param name="cache">The cache supplied to this operation.</param>
/// <param name="adminOptions">The admin options supplied to this operation.</param>
public sealed class DatabaseInitializer(
    AppDbContext dbContext,
    RoleManager<IdentityRole> roleManager,
    UserManager<ApplicationUser> userManager,
    IFileStorageService fileStorage,
    IApplicationCache cache,
    IOptions<AdminSeedOptions> adminOptions)
{
    private static readonly string[] Roles = ["Author", "Admin"];

    /// <summary>Performs the initialize operation.</summary>
    /// <param name="cancellationToken">Cancels the operation when requested by the caller.</param>
    /// <returns>A task representing the operation and its result.</returns>
    public async Task InitializeAsync(CancellationToken cancellationToken)
    {
        await dbContext.Database.MigrateAsync(cancellationToken).ConfigureAwait(false);

        foreach (var role in Roles)
        {
            if (!await roleManager.RoleExistsAsync(role).ConfigureAwait(false))
            {
                EnsureSucceeded(
                    await roleManager.CreateAsync(new IdentityRole(role)).ConfigureAwait(false),
                    $"create the {role} role");
            }
        }

        var authorIds = await this.SeedAuthorsAsync().ConfigureAwait(false);
        var categoryIds = await this.SeedCategoriesAsync(cancellationToken).ConfigureAwait(false);
        await this.SeedRecipesAsync(categoryIds, authorIds, cancellationToken).ConfigureAwait(false);
        await cache.RemoveByTagAsync(PublicCacheKeys.RecipesTag, cancellationToken).ConfigureAwait(false);
        await cache.RemoveByTagAsync(PublicCacheKeys.CategoriesTag, cancellationToken).ConfigureAwait(false);
        await this.SeedAdminAsync().ConfigureAwait(false);
    }

    private static Guid StableGuid(string value)
    {
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes($"culinary-blog-seed:{value}"));
        hash[7] = (byte)((hash[7] & 0x0f) | 0x40);
        hash[8] = (byte)((hash[8] & 0x3f) | 0x80);
        return new Guid(hash.AsSpan(0, 16));
    }

    private static void EnsureSucceeded(IdentityResult result, string operation)
    {
        if (!result.Succeeded)
        {
            throw new InvalidOperationException(
                $"Failed to {operation}: {string.Join(", ", result.Errors.Select(error => error.Code))}");
        }
    }

    private async Task<List<string>> SeedAuthorsAsync()
    {
        var authorIds = new List<string>(VietnameseSeedData.Authors.Count);
        foreach (var seed in VietnameseSeedData.Authors)
        {
            var author = await userManager.FindByEmailAsync(seed.Email).ConfigureAwait(false);
            if (author is null)
            {
                author = new ApplicationUser
                {
                    Id = StableGuid($"author:{seed.Email}").ToString(),
                    UserName = seed.Email,
                    Email = seed.Email,
                    EmailConfirmed = true,
                    DisplayName = seed.DisplayName,
                    Bio = seed.Bio,
                    IsActive = true,
                    CreatedAt = DateTimeOffset.UtcNow,
                };
                EnsureSucceeded(
                    await userManager.CreateAsync(author).ConfigureAwait(false),
                    $"create seed author {seed.Email}");
            }

            if (!await userManager.IsInRoleAsync(author, "Author").ConfigureAwait(false))
            {
                EnsureSucceeded(
                    await userManager.AddToRoleAsync(author, "Author").ConfigureAwait(false),
                    $"assign seed author {seed.Email} to the Author role");
            }

            authorIds.Add(author.Id);
        }

        return authorIds;
    }

    private async Task<IReadOnlyDictionary<string, Guid>> SeedCategoriesAsync(CancellationToken cancellationToken)
    {
        var existing = await dbContext.Categories.IgnoreQueryFilters()
            .ToDictionaryAsync(category => category.Slug, StringComparer.Ordinal, cancellationToken)
            .ConfigureAwait(false);

        for (var index = 0; index < VietnameseSeedData.Categories.Count; index++)
        {
            var seed = VietnameseSeedData.Categories[index];
            if (existing.ContainsKey(seed.Slug))
            {
                continue;
            }

            var category = Category.Create(
                StableGuid($"category:{seed.Slug}"),
                seed.Name,
                seed.Slug,
                seed.Description,
                null,
                index);
            dbContext.Categories.Add(category);
            existing.Add(seed.Slug, category);
        }

        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return existing
            .Where(pair => !pair.Value.IsDeleted && VietnameseSeedData.Categories.Any(seed => seed.Slug == pair.Key))
            .ToDictionary(pair => pair.Key, pair => pair.Value.Id, StringComparer.Ordinal);
    }

    private async Task SeedRecipesAsync(
        IReadOnlyDictionary<string, Guid> categoryIds,
        List<string> authorIds,
        CancellationToken cancellationToken)
    {
        // Bogus generates a repeatable 50-recipe fixture from the curated Vietnamese templates.
        // Existing installations, including the earlier 100-recipe fixture, are never pruned or overwritten.
        var seedRecipes = new Randomizer(20261025).Shuffle(VietnameseSeedData.Recipes).Take(50).ToArray();
        var seedSlugs = seedRecipes.Select(recipe => recipe.Slug).ToArray();
        var existingRecipes = await dbContext.Recipes.IgnoreQueryFilters()
            .Where(recipe => seedSlugs.Contains(recipe.Slug))
            .ToDictionaryAsync(recipe => recipe.Slug, StringComparer.Ordinal, cancellationToken)
            .ConfigureAwait(false);
        var publishedAt = DateTimeOffset.UtcNow.AddDays(-seedRecipes.Length);

        for (var recipeIndex = 0; recipeIndex < seedRecipes.Length; recipeIndex++)
        {
            var seed = seedRecipes[recipeIndex];
            var nutrition = RecipeNutrition.Create(
                seed.Nutrition.Calories,
                seed.Nutrition.Protein,
                seed.Nutrition.Carbohydrates,
                seed.Nutrition.Fat,
                seed.Nutrition.Fiber,
                seed.Nutrition.Sodium);
            if (existingRecipes.ContainsKey(seed.Slug) || !categoryIds.TryGetValue(seed.CategorySlug, out var categoryId))
            {
                continue;
            }

            var recipe = Recipe.Create(
                StableGuid($"recipe:{seed.Slug}"),
                authorIds[recipeIndex % authorIds.Count],
                seed.Slug,
                seed.Title,
                seed.Description,
                categoryId,
                seed.PrepTime,
                seed.CookTime,
                seed.Servings,
                seed.Difficulty,
                null,
                nutrition);
            recipe.Publish(
                publishedAt.AddDays(recipeIndex),
                true,
                seed.Ingredients.Count,
                Enumerable.Range(1, seed.Steps.Count).ToArray());
            dbContext.Recipes.Add(recipe);

            var recipeId = recipe.Id;

            for (var ingredientIndex = 0; ingredientIndex < seed.Ingredients.Count; ingredientIndex++)
            {
                var ingredient = seed.Ingredients[ingredientIndex];
                dbContext.RecipeIngredients.Add(RecipeIngredient.Create(
                    StableGuid($"ingredient:{seed.Slug}:{ingredientIndex}"),
                    recipeId,
                    ingredient.Name,
                    ingredient.Quantity,
                    ingredient.Unit,
                    ingredient.Notes,
                    ingredientIndex));
            }

            for (var stepIndex = 0; stepIndex < seed.Steps.Count; stepIndex++)
            {
                var step = seed.Steps[stepIndex];
                dbContext.RecipeSteps.Add(RecipeStep.Create(
                    StableGuid($"step:{seed.Slug}:{stepIndex}"),
                    recipeId,
                    stepIndex + 1,
                    step.Title,
                    step.Description,
                    step.TimerMinutes,
                    null));
            }

            await this.EnsureRecipeImageAsync(recipeId, seed, cancellationToken).ConfigureAwait(false);
        }

        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    private async Task EnsureRecipeImageAsync(Guid recipeId, SeedRecipe seed, CancellationToken cancellationToken)
    {
        var imageId = StableGuid($"image:{seed.Slug}");
        var objectKey = $"recipes/{recipeId:N}/{imageId:N}/original.jpg";
        var imageRecord = await dbContext.RecipeImages.IgnoreQueryFilters()
            .SingleOrDefaultAsync(image => image.RecipeId == recipeId, cancellationToken)
            .ConfigureAwait(false);
        var resourceName = typeof(DatabaseInitializer).Assembly.GetManifestResourceNames()
            .Single(name => name.EndsWith($".{seed.Slug}.jpg", StringComparison.Ordinal));
        await using var imageStream = typeof(DatabaseInitializer).Assembly.GetManifestResourceStream(resourceName)
            ?? throw new InvalidOperationException($"Seed image resource '{resourceName}' was not found.");
        var objectExists = await fileStorage.ExistsAsync(objectKey, cancellationToken).ConfigureAwait(false);
        var sourceChanged = !objectExists;
        if (objectExists)
        {
            await using var storedImage = await fileStorage.OpenReadAsync(objectKey, cancellationToken).ConfigureAwait(false);
            sourceChanged = storedImage.Length != imageStream.Length;
        }

        if (sourceChanged)
        {
            await fileStorage.UploadAsync(
                objectKey,
                imageStream,
                imageStream.Length,
                "image/jpeg",
                cancellationToken).ConfigureAwait(false);
        }

        if (imageRecord is null)
        {
            imageRecord = RecipeImage.Create(
                imageId,
                recipeId,
                objectKey,
                "image/jpeg",
                $"{seed.Title} hoàn chỉnh",
                true,
                0);
            dbContext.RecipeImages.Add(imageRecord);
        }
        else if (sourceChanged)
        {
            imageRecord.MarkPending();
        }
        else
        {
            return;
        }

        dbContext.MediaOutbox.Add(new MediaOutboxMessage
        {
            Id = Guid.NewGuid(),
            Type = MediaOutboxTypes.ResizeImage,
            Payload = JsonSerializer.Serialize(new ResizeImagePayload(imageId)),
            CreatedAt = DateTimeOffset.UtcNow,
            NextAttemptAt = DateTimeOffset.UtcNow,
        });
    }

    private async Task SeedAdminAsync()
    {
        var options = adminOptions.Value;
        if (!options.Enabled)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(options.Email) || string.IsNullOrWhiteSpace(options.Password))
        {
            throw new InvalidOperationException("AdminSeed Email and Password are required when admin seeding is enabled.");
        }

        var admin = await userManager.FindByEmailAsync(options.Email).ConfigureAwait(false);
        if (admin is null)
        {
            admin = new ApplicationUser
            {
                Id = Guid.NewGuid().ToString(),
                UserName = options.Email,
                Email = options.Email,
                DisplayName = options.DisplayName,
                IsActive = true,
                CreatedAt = DateTimeOffset.UtcNow,
            };
            EnsureSucceeded(
                await userManager.CreateAsync(admin, options.Password).ConfigureAwait(false),
                "create the admin user");
        }

        if (!await userManager.IsInRoleAsync(admin, "Admin").ConfigureAwait(false))
        {
            EnsureSucceeded(
                await userManager.AddToRoleAsync(admin, "Admin").ConfigureAwait(false),
                "assign the Admin role");
        }
    }
}
