using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
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

public sealed class DatabaseInitializer(
    AppDbContext dbContext,
    RoleManager<IdentityRole<Guid>> roleManager,
    UserManager<ApplicationUser> userManager,
    IFileStorageService fileStorage,
    IApplicationCache cache,
    IOptions<AdminSeedOptions> adminOptions)
{
    private static readonly string[] Roles = ["Author", "Admin"];

    public async Task InitializeAsync(CancellationToken cancellationToken)
    {
        await dbContext.Database.MigrateAsync(cancellationToken).ConfigureAwait(false);

        foreach (var role in Roles)
        {
            if (!await roleManager.RoleExistsAsync(role).ConfigureAwait(false))
            {
                EnsureSucceeded(
                    await roleManager.CreateAsync(new IdentityRole<Guid>(role)).ConfigureAwait(false),
                    $"create the {role} role");
            }
        }

        var authorIds = await SeedAuthorsAsync().ConfigureAwait(false);
        await RemoveNonSeedDataAsync(cancellationToken).ConfigureAwait(false);
        var categoryIds = await SeedCategoriesAsync(cancellationToken).ConfigureAwait(false);
        await SeedRecipesAsync(categoryIds, authorIds, cancellationToken).ConfigureAwait(false);
        await cache.RemoveByTagAsync(PublicCacheKeys.RecipesTag, cancellationToken).ConfigureAwait(false);
        await cache.RemoveByTagAsync(PublicCacheKeys.CategoriesTag, cancellationToken).ConfigureAwait(false);
        await SeedAdminAsync().ConfigureAwait(false);
    }

    private async Task<List<Guid>> SeedAuthorsAsync()
    {
        var authorIds = new List<Guid>(VietnameseSeedData.Authors.Count);
        foreach (var seed in VietnameseSeedData.Authors)
        {
            var author = await userManager.FindByEmailAsync(seed.Email).ConfigureAwait(false);
            if (author is null)
            {
                author = new ApplicationUser
                {
                    Id = StableGuid($"author:{seed.Email}"),
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
            else
            {
                author.DisplayName = seed.DisplayName;
                author.Bio = seed.Bio;
                author.EmailConfirmed = true;
                author.IsActive = true;
                EnsureSucceeded(
                    await userManager.UpdateAsync(author).ConfigureAwait(false),
                    $"update seed author {seed.Email}");
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

    private async Task RemoveNonSeedDataAsync(CancellationToken cancellationToken)
    {
        var seedSlugs = VietnameseSeedData.Recipes.Select(recipe => recipe.Slug)
            .ToHashSet(StringComparer.Ordinal);
        var recipes = await dbContext.Recipes.IgnoreQueryFilters()
            .Where(recipe => !seedSlugs.Contains(recipe.Slug))
            .Select(recipe => recipe.Id)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
        var nonSeedRecipeIds = recipes.ToArray();

        var legacyImages = await dbContext.RecipeImages.IgnoreQueryFilters()
            .Where(image => nonSeedRecipeIds.Contains(image.RecipeId))
            .Select(image => new { image.ObjectKey, image.MediumObjectKey, image.ThumbnailObjectKey })
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
        var objectKeys = legacyImages
            .SelectMany(image => new[] { image.ObjectKey, image.MediumObjectKey, image.ThumbnailObjectKey })
            .OfType<string>();

        await dbContext.RecipeImages.IgnoreQueryFilters().Where(image => nonSeedRecipeIds.Contains(image.RecipeId))
            .ExecuteDeleteAsync(cancellationToken).ConfigureAwait(false);
        await dbContext.RecipeIngredients.IgnoreQueryFilters().Where(item => nonSeedRecipeIds.Contains(item.RecipeId))
            .ExecuteDeleteAsync(cancellationToken).ConfigureAwait(false);
        await dbContext.RecipeSteps.IgnoreQueryFilters().Where(item => nonSeedRecipeIds.Contains(item.RecipeId))
            .ExecuteDeleteAsync(cancellationToken).ConfigureAwait(false);
        await dbContext.Recipes.IgnoreQueryFilters().Where(recipe => nonSeedRecipeIds.Contains(recipe.Id))
            .ExecuteDeleteAsync(cancellationToken).ConfigureAwait(false);
        var seedCategorySlugs = VietnameseSeedData.Categories.Select(category => category.Slug).ToArray();
        await dbContext.Categories.IgnoreQueryFilters()
            .Where(category => !seedCategorySlugs.Contains(category.Slug))
            .ExecuteDeleteAsync(cancellationToken)
            .ConfigureAwait(false);

        foreach (var objectKey in objectKeys.Distinct(StringComparer.Ordinal))
        {
            if (await fileStorage.ExistsAsync(objectKey, cancellationToken).ConfigureAwait(false))
            {
                await fileStorage.DeleteAsync(objectKey, cancellationToken).ConfigureAwait(false);
            }
        }

        dbContext.ChangeTracker.Clear();
    }

    private async Task<IReadOnlyDictionary<string, Guid>> SeedCategoriesAsync(CancellationToken cancellationToken)
    {
        var existing = await dbContext.Categories.IgnoreQueryFilters()
            .Where(category => !category.IsDeleted)
            .ToDictionaryAsync(category => category.Slug, StringComparer.Ordinal, cancellationToken)
            .ConfigureAwait(false);

        for (var index = 0; index < VietnameseSeedData.Categories.Count; index++)
        {
            var seed = VietnameseSeedData.Categories[index];
            if (existing.TryGetValue(seed.Slug, out var category))
            {
                category.Update(seed.Name, seed.Description, null, index);
                continue;
            }

            category = Category.Create(
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
            .Where(pair => VietnameseSeedData.Categories.Any(seed => seed.Slug == pair.Key))
            .ToDictionary(pair => pair.Key, pair => pair.Value.Id, StringComparer.Ordinal);
    }

    private async Task SeedRecipesAsync(
        IReadOnlyDictionary<string, Guid> categoryIds,
        List<Guid> authorIds,
        CancellationToken cancellationToken)
    {
        var seedSlugs = VietnameseSeedData.Recipes.Select(recipe => recipe.Slug).ToArray();
        var existingRecipeIds = await dbContext.Recipes.IgnoreQueryFilters()
            .Where(recipe => seedSlugs.Contains(recipe.Slug))
            .Select(recipe => recipe.Id)
            .ToArrayAsync(cancellationToken)
            .ConfigureAwait(false);
        await dbContext.RecipeIngredients.IgnoreQueryFilters()
            .Where(item => existingRecipeIds.Contains(item.RecipeId))
            .ExecuteDeleteAsync(cancellationToken)
            .ConfigureAwait(false);
        await dbContext.RecipeSteps.IgnoreQueryFilters()
            .Where(item => existingRecipeIds.Contains(item.RecipeId))
            .ExecuteDeleteAsync(cancellationToken)
            .ConfigureAwait(false);
        dbContext.ChangeTracker.Clear();
        var existingRecipes = await dbContext.Recipes.IgnoreQueryFilters()
            .Where(recipe => seedSlugs.Contains(recipe.Slug))
            .ToDictionaryAsync(recipe => recipe.Slug, StringComparer.Ordinal, cancellationToken)
            .ConfigureAwait(false);
        var publishedAt = DateTimeOffset.UtcNow.AddDays(-VietnameseSeedData.Recipes.Count);

        for (var recipeIndex = 0; recipeIndex < VietnameseSeedData.Recipes.Count; recipeIndex++)
        {
            var seed = VietnameseSeedData.Recipes[recipeIndex];
            var nutrition = RecipeNutrition.Create(
                seed.Nutrition.Calories,
                seed.Nutrition.Protein,
                seed.Nutrition.Carbohydrates,
                seed.Nutrition.Fat,
                seed.Nutrition.Fiber,
                seed.Nutrition.Sodium);
            var instructions = string.Join(
                "\n\n",
                seed.Steps.Select((step, index) => $"{index + 1}. {step.Title}: {step.Description}"));
            var isNew = !existingRecipes.TryGetValue(seed.Slug, out var recipe);
            if (isNew)
            {
                recipe = Recipe.Create(
                    StableGuid($"recipe:{seed.Slug}"),
                    authorIds[recipeIndex % authorIds.Count],
                    seed.Slug,
                    seed.Title,
                    seed.Description,
                    categoryIds[seed.CategorySlug],
                    seed.PrepTime,
                    seed.CookTime,
                    seed.Servings,
                    seed.Difficulty,
                    instructions,
                    nutrition);
                recipe.Publish(
                    publishedAt.AddDays(recipeIndex),
                    true,
                    seed.Ingredients.Count,
                    Enumerable.Range(1, seed.Steps.Count).ToArray());
                dbContext.Recipes.Add(recipe);
            }
            else
            {
                recipe!.Update(
                    seed.Slug,
                    seed.Title,
                    seed.Description,
                    categoryIds[seed.CategorySlug],
                    seed.PrepTime,
                    seed.CookTime,
                    seed.Servings,
                    seed.Difficulty,
                    instructions,
                    nutrition);
            }

            var recipeId = recipe!.Id;

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

            await EnsureRecipeImageAsync(recipeId, seed, cancellationToken).ConfigureAwait(false);
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
                Id = Guid.NewGuid(),
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

    private static Guid StableGuid(string value)
    {
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes($"culinary-blog-seed:{value}"));
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
}
