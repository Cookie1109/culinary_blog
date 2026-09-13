using CulinaryBlog.Application.Content;
using CulinaryBlog.Domain.Categories;
using CulinaryBlog.Domain.Common;
using CulinaryBlog.Domain.Recipes;
using CulinaryBlog.Infrastructure.Identity;
using CulinaryBlog.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace CulinaryBlog.Infrastructure.Content;

internal sealed class ContentService(
    AppDbContext dbContext,
    UserManager<ApplicationUser> userManager) : IContentService
{
    private const int MaximumPageSize = 50;

    public async Task<IReadOnlyCollection<CategoryDto>> ListCategoriesAsync(CancellationToken cancellationToken)
    {
        var categories = await dbContext.Categories
            .AsNoTracking()
            .OrderBy(category => category.Name)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
        return await MapCategoriesAsync(categories, cancellationToken).ConfigureAwait(false);
    }

    public async Task<CategoryDetailEnvelope> GetCategoryAsync(
        string slug,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        ValidatePage(page, pageSize);
        var category = await dbContext.Categories.AsNoTracking()
            .SingleOrDefaultAsync(item => item.Slug == slug, cancellationToken)
            .ConfigureAwait(false)
            ?? throw NotFound("CATEGORY_NOT_FOUND", "Category was not found.");

        var query = dbContext.Recipes.AsNoTracking()
            .Where(recipe => recipe.CategoryId == category.Id && recipe.Status == RecipeStatus.Published)
            .OrderByDescending(recipe => recipe.PublishedAt)
            .ThenByDescending(recipe => recipe.CreatedAt);
        var total = await query.CountAsync(cancellationToken).ConfigureAwait(false);
        var recipes = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(cancellationToken)
            .ConfigureAwait(false);
        var mapped = await MapRecipesAsync(recipes, cancellationToken).ConfigureAwait(false);
        var categoryDto = ToCategoryDto(category, total);
        return new CategoryDetailEnvelope(
            new CategoryDetailData(categoryDto, mapped),
            CreateMeta(page, pageSize, total));
    }

    public async Task<CategoryDto> CreateCategoryAsync(
        CategoryWriteRequest request,
        CancellationToken cancellationToken)
    {
        var normalizedName = request.Name.Trim().ToUpperInvariant();
        if (await dbContext.Categories.IgnoreQueryFilters()
            .AnyAsync(category => category.NormalizedName == normalizedName, cancellationToken)
            .ConfigureAwait(false))
        {
            throw Conflict("CATEGORY_NAME_EXISTS", "A category with this name already exists.");
        }

        var slug = Slug.From(request.Name, 120);
        if (await dbContext.Categories.IgnoreQueryFilters()
            .AnyAsync(category => category.Slug == slug, cancellationToken)
            .ConfigureAwait(false))
        {
            throw Conflict("CATEGORY_SLUG_EXISTS", "A category already owns this slug.");
        }

        var category = Category.Create(
            Guid.NewGuid(),
            request.Name,
            slug,
            request.Description,
            request.ImageUrl,
            request.OrderIndex);
        dbContext.Categories.Add(category);
        try
        {
            await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (DbUpdateException exception) when (IsUniqueViolation(exception, "NormalizedName"))
        {
            throw Conflict("CATEGORY_NAME_EXISTS", "A category with this name already exists.");
        }
        catch (DbUpdateException exception) when (IsUniqueViolation(exception, "Slug"))
        {
            throw Conflict("CATEGORY_SLUG_EXISTS", "A category already owns this slug.");
        }

        return ToCategoryDto(category, 0);
    }

    public async Task<CategoryDto> UpdateCategoryAsync(
        Guid id,
        CategoryWriteRequest request,
        CancellationToken cancellationToken)
    {
        var category = await dbContext.Categories.SingleOrDefaultAsync(item => item.Id == id, cancellationToken)
            .ConfigureAwait(false)
            ?? throw NotFound("CATEGORY_NOT_FOUND", "Category was not found.");
        var normalizedName = request.Name.Trim().ToUpperInvariant();
        if (await dbContext.Categories.IgnoreQueryFilters()
            .AnyAsync(item => item.Id != id && item.NormalizedName == normalizedName, cancellationToken)
            .ConfigureAwait(false))
        {
            throw Conflict("CATEGORY_NAME_EXISTS", "A category with this name already exists.");
        }

        category.Update(request.Name, request.Description, request.ImageUrl, request.OrderIndex);
        try
        {
            await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (DbUpdateException exception) when (IsUniqueViolation(exception, "NormalizedName"))
        {
            throw Conflict("CATEGORY_NAME_EXISTS", "A category with this name already exists.");
        }

        var count = await dbContext.Recipes.CountAsync(
            recipe => recipe.CategoryId == id && recipe.Status == RecipeStatus.Published,
            cancellationToken).ConfigureAwait(false);
        return ToCategoryDto(category, count);
    }

    public async Task DeleteCategoryAsync(Guid id, CancellationToken cancellationToken)
    {
        var category = await dbContext.Categories.SingleOrDefaultAsync(item => item.Id == id, cancellationToken)
            .ConfigureAwait(false)
            ?? throw NotFound("CATEGORY_NOT_FOUND", "Category was not found.");
        if (await dbContext.Recipes.AnyAsync(recipe => recipe.CategoryId == id, cancellationToken).ConfigureAwait(false))
        {
            throw Conflict("CATEGORY_DELETE_HAS_RECIPES", "A category containing recipes cannot be deleted.");
        }

        category.Delete();
        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task<RecipeDto> CreateRecipeAsync(
        Guid userId,
        RecipeWriteRequest request,
        CancellationToken cancellationToken)
    {
        await EnsureCategoryExistsAsync(request.CategoryId, cancellationToken).ConfigureAwait(false);
        var baseSlug = Slug.From(request.Title, 220);

        for (var suffix = 1; suffix <= 20; suffix++)
        {
            var slug = WithSuffix(baseSlug, suffix, 220);
            if (await dbContext.Recipes.IgnoreQueryFilters()
                .AnyAsync(recipe => recipe.Slug == slug, cancellationToken).ConfigureAwait(false))
            {
                continue;
            }

            var recipe = Recipe.Create(
                Guid.NewGuid(),
                userId,
                slug,
                request.Title,
                request.Description,
                request.CategoryId,
                request.PrepTime,
                request.CookTime,
                request.Servings,
                request.Difficulty,
                request.Instructions,
                ToNutrition(request.Nutrition));
            dbContext.Recipes.Add(recipe);
            try
            {
                await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
                return await MapRecipeAsync(recipe, cancellationToken).ConfigureAwait(false);
            }
            catch (DbUpdateException exception) when (IsUniqueViolation(exception, "Slug"))
            {
                dbContext.Entry(recipe).State = EntityState.Detached;
            }
        }

        throw Conflict("RECIPE_SLUG_EXISTS", "A unique recipe slug could not be allocated.");
    }

    public async Task<RecipeDto> UpdateRecipeAsync(
        Guid id,
        Guid userId,
        bool isAdmin,
        long expectedVersion,
        RecipeWriteRequest request,
        CancellationToken cancellationToken)
    {
        var recipe = await FindRecipeForMutationAsync(id, userId, isAdmin, cancellationToken).ConfigureAwait(false);
        EnsureVersion(recipe, expectedVersion);
        await EnsureCategoryExistsAsync(request.CategoryId, cancellationToken).ConfigureAwait(false);
        var slug = recipe.PublishedAt is null
            ? await AllocateRecipeSlugAsync(request.Title, recipe.Id, cancellationToken).ConfigureAwait(false)
            : recipe.Slug;
        recipe.Update(
            slug,
            request.Title,
            request.Description,
            request.CategoryId,
            request.PrepTime,
            request.CookTime,
            request.Servings,
            request.Difficulty,
            request.Instructions,
            ToNutrition(request.Nutrition));
        await SaveRecipeMutationAsync(recipe, cancellationToken).ConfigureAwait(false);
        return await MapRecipeAsync(recipe, cancellationToken).ConfigureAwait(false);
    }

    public async Task DeleteRecipeAsync(
        Guid id,
        Guid userId,
        bool isAdmin,
        long expectedVersion,
        CancellationToken cancellationToken)
    {
        var recipe = await FindRecipeForMutationAsync(id, userId, isAdmin, cancellationToken).ConfigureAwait(false);
        EnsureVersion(recipe, expectedVersion);
        recipe.Delete();
        await SaveRecipeMutationAsync(recipe, cancellationToken).ConfigureAwait(false);
    }

    public async Task<RecipeDto> GetPublishedRecipeAsync(string slug, CancellationToken cancellationToken)
    {
        var recipe = await dbContext.Recipes.AsNoTracking()
            .SingleOrDefaultAsync(
                item => item.Slug == slug && item.Status == RecipeStatus.Published,
                cancellationToken)
            .ConfigureAwait(false)
            ?? throw NotFound("RECIPE_NOT_FOUND", "Recipe was not found.");
        return await MapRecipeAsync(recipe, cancellationToken).ConfigureAwait(false);
    }

    public Task<PageEnvelope<RecipeDto>> ListPublishedRecipesAsync(
        int page,
        int pageSize,
        CancellationToken cancellationToken) =>
        ListRecipesAsync(
            dbContext.Recipes.AsNoTracking().Where(recipe => recipe.Status == RecipeStatus.Published),
            page,
            pageSize,
            cancellationToken);

    public async Task<RecipeDto> GetMyRecipeAsync(Guid id, Guid userId, CancellationToken cancellationToken)
    {
        var recipe = await dbContext.Recipes.AsNoTracking().SingleOrDefaultAsync(item => item.Id == id, cancellationToken)
            .ConfigureAwait(false)
            ?? throw NotFound("RECIPE_NOT_FOUND", "Recipe was not found.");
        if (recipe.AuthorId != userId)
        {
            throw Forbidden();
        }

        return await MapRecipeAsync(recipe, cancellationToken).ConfigureAwait(false);
    }

    public Task<PageEnvelope<RecipeDto>> ListMyRecipesAsync(
        Guid userId,
        RecipeStatus? status,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        var query = dbContext.Recipes.AsNoTracking().Where(recipe => recipe.AuthorId == userId);
        if (status.HasValue)
        {
            query = query.Where(recipe => recipe.Status == status.Value);
        }

        return ListRecipesAsync(query, page, pageSize, cancellationToken);
    }

    public async Task<RecipeDto> GetAdminRecipeAsync(Guid id, CancellationToken cancellationToken)
    {
        var recipe = await dbContext.Recipes.AsNoTracking().SingleOrDefaultAsync(item => item.Id == id, cancellationToken)
            .ConfigureAwait(false)
            ?? throw NotFound("RECIPE_NOT_FOUND", "Recipe was not found.");
        return await MapRecipeAsync(recipe, cancellationToken).ConfigureAwait(false);
    }

    public Task<PageEnvelope<RecipeDto>> ListAdminRecipesAsync(
        RecipeStatus? status,
        Guid? authorId,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        var query = dbContext.Recipes.AsNoTracking();
        if (status.HasValue)
        {
            query = query.Where(recipe => recipe.Status == status.Value);
        }

        if (authorId.HasValue)
        {
            query = query.Where(recipe => recipe.AuthorId == authorId.Value);
        }

        return ListRecipesAsync(query, page, pageSize, cancellationToken);
    }

    private async Task<PageEnvelope<RecipeDto>> ListRecipesAsync(
        IQueryable<Recipe> query,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        ValidatePage(page, pageSize);
        var total = await query.CountAsync(cancellationToken).ConfigureAwait(false);
        var recipes = await query.OrderByDescending(recipe => recipe.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
        return new PageEnvelope<RecipeDto>(
            await MapRecipesAsync(recipes, cancellationToken).ConfigureAwait(false),
            CreateMeta(page, pageSize, total));
    }

    private async Task<Recipe> FindRecipeForMutationAsync(
        Guid id,
        Guid userId,
        bool isAdmin,
        CancellationToken cancellationToken)
    {
        var recipe = await dbContext.Recipes.SingleOrDefaultAsync(item => item.Id == id, cancellationToken)
            .ConfigureAwait(false)
            ?? throw NotFound("RECIPE_NOT_FOUND", "Recipe was not found.");
        if (!isAdmin && recipe.AuthorId != userId)
        {
            throw Forbidden();
        }

        return recipe;
    }

    private async Task SaveRecipeMutationAsync(Recipe recipe, CancellationToken cancellationToken)
    {
        try
        {
            await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (DbUpdateConcurrencyException)
        {
            var currentVersion = await dbContext.Recipes.AsNoTracking()
                .Where(item => item.Id == recipe.Id)
                .Select(item => (long?)item.Version)
                .SingleOrDefaultAsync(cancellationToken)
                .ConfigureAwait(false);
            throw new ContentProblemException(
                "RECIPE_CONCURRENCY_CONFLICT",
                "The recipe was changed by another request.",
                ContentProblemKind.Conflict,
                currentVersion);
        }
        catch (DbUpdateException exception) when (IsUniqueViolation(exception, "Slug"))
        {
            throw Conflict("RECIPE_SLUG_EXISTS", "A recipe already owns this slug.");
        }
    }

    private async Task<string> AllocateRecipeSlugAsync(
        string title,
        Guid recipeId,
        CancellationToken cancellationToken)
    {
        var baseSlug = Slug.From(title, 220);
        for (var suffix = 1; suffix <= 20; suffix++)
        {
            var candidate = WithSuffix(baseSlug, suffix, 220);
            if (!await dbContext.Recipes.IgnoreQueryFilters()
                .AnyAsync(recipe => recipe.Id != recipeId && recipe.Slug == candidate, cancellationToken)
                .ConfigureAwait(false))
            {
                return candidate;
            }
        }

        throw Conflict("RECIPE_SLUG_EXISTS", "A unique recipe slug could not be allocated.");
    }

    private async Task EnsureCategoryExistsAsync(Guid categoryId, CancellationToken cancellationToken)
    {
        if (!await dbContext.Categories.AnyAsync(category => category.Id == categoryId, cancellationToken).ConfigureAwait(false))
        {
            throw NotFound("CATEGORY_NOT_FOUND", "Category was not found.");
        }
    }

    private async Task<IReadOnlyCollection<RecipeDto>> MapRecipesAsync(
        List<Recipe> recipes,
        CancellationToken cancellationToken)
    {
        var result = new List<RecipeDto>(recipes.Count);
        foreach (var recipe in recipes)
        {
            result.Add(await MapRecipeAsync(recipe, cancellationToken).ConfigureAwait(false));
        }

        return result;
    }

    private async Task<RecipeDto> MapRecipeAsync(Recipe recipe, CancellationToken cancellationToken)
    {
        var category = await dbContext.Categories.AsNoTracking()
            .SingleAsync(item => item.Id == recipe.CategoryId, cancellationToken)
            .ConfigureAwait(false);
        var categoryCount = await dbContext.Recipes.AsNoTracking().CountAsync(
            item => item.CategoryId == category.Id && item.Status == RecipeStatus.Published,
            cancellationToken).ConfigureAwait(false);
        var author = await userManager.FindByIdAsync(recipe.AuthorId.ToString()).ConfigureAwait(false)
            ?? throw NotFound("USER_NOT_FOUND", "Recipe author was not found.");
        var roles = await userManager.GetRolesAsync(author).ConfigureAwait(false);
        return new RecipeDto(
            recipe.Id,
            recipe.Title,
            recipe.Slug,
            recipe.Description,
            recipe.PrepTime,
            recipe.CookTime,
            recipe.Servings,
            recipe.Difficulty.ToString().ToLowerInvariant(),
            recipe.Status.ToString().ToLowerInvariant(),
            null,
            ToCategoryDto(category, categoryCount),
            new RecipeAuthorDto(
                author.Id.ToString(),
                author.Email ?? string.Empty,
                author.DisplayName,
                author.AvatarUrl,
                author.Bio,
                roles.ToArray(),
                author.EmailConfirmed,
                author.IsActive,
                author.CreatedAt),
            recipe.CreatedAt,
            recipe.PublishedAt,
            recipe.Version,
            recipe.Instructions,
            ToNutritionDto(recipe.Nutrition),
            [],
            [],
            []);
    }

    private async Task<IReadOnlyCollection<CategoryDto>> MapCategoriesAsync(
        IReadOnlyCollection<Category> categories,
        CancellationToken cancellationToken)
    {
        var counts = await dbContext.Recipes.AsNoTracking()
            .Where(recipe => recipe.Status == RecipeStatus.Published)
            .GroupBy(recipe => recipe.CategoryId)
            .Select(group => new { CategoryId = group.Key, Count = group.Count() })
            .ToDictionaryAsync(item => item.CategoryId, item => item.Count, cancellationToken)
            .ConfigureAwait(false);
        return categories.Select(category => ToCategoryDto(category, counts.GetValueOrDefault(category.Id))).ToArray();
    }

    private static CategoryDto ToCategoryDto(Category category, int recipeCount) =>
        new(category.Id, category.Name, category.Slug, category.Description, category.ImageUrl, category.OrderIndex, recipeCount);

    private static RecipeNutrition? ToNutrition(NutritionDto? nutrition) => nutrition is null
        ? null
        : RecipeNutrition.Create(
            nutrition.Calories,
            nutrition.Protein,
            nutrition.Carbohydrates,
            nutrition.Fat,
            nutrition.Fiber,
            nutrition.Sodium);

    private static NutritionDto? ToNutritionDto(RecipeNutrition? nutrition) => nutrition is null ||
        (nutrition.Calories is null && nutrition.Protein is null && nutrition.Carbohydrates is null &&
         nutrition.Fat is null && nutrition.Fiber is null && nutrition.Sodium is null)
        ? null
        : new NutritionDto(
            nutrition.Calories,
            nutrition.Protein,
            nutrition.Carbohydrates,
            nutrition.Fat,
            nutrition.Fiber,
            nutrition.Sodium);

    private static void EnsureVersion(Recipe recipe, long expectedVersion)
    {
        if (expectedVersion != recipe.Version)
        {
            throw new ContentProblemException(
                "RECIPE_CONCURRENCY_CONFLICT",
                "The recipe version is stale.",
                ContentProblemKind.Conflict,
                recipe.Version);
        }
    }

    private static void ValidatePage(int page, int pageSize)
    {
        if (page < 1 || pageSize is < 1 or > MaximumPageSize)
        {
            throw new ContentProblemException(
                "VALIDATION_ERROR",
                "page must be at least 1 and pageSize must be between 1 and 50.",
                ContentProblemKind.BadRequest);
        }
    }

    private static string WithSuffix(string baseSlug, int suffix, int maximumLength)
    {
        if (suffix == 1)
        {
            return baseSlug;
        }

        var ending = $"-{suffix}";
        var prefix = baseSlug.Length + ending.Length <= maximumLength
            ? baseSlug
            : baseSlug[..(maximumLength - ending.Length)].TrimEnd('-');
        return prefix + ending;
    }

    private static bool IsUniqueViolation(DbUpdateException exception, string property) =>
        exception.InnerException is PostgresException postgresException &&
        postgresException.SqlState == PostgresErrorCodes.UniqueViolation &&
        (postgresException.ConstraintName?.Contains(property, StringComparison.OrdinalIgnoreCase) ?? false);

    private static PageMeta CreateMeta(int page, int pageSize, int total)
    {
        var totalPages = total == 0 ? 0 : (int)Math.Ceiling(total / (double)pageSize);
        return new PageMeta(page, pageSize, total, totalPages, page < totalPages, page > 1 && totalPages > 0);
    }

    private static ContentProblemException NotFound(string code, string message) =>
        new(code, message, ContentProblemKind.NotFound);

    private static ContentProblemException Conflict(string code, string message) =>
        new(code, message, ContentProblemKind.Conflict);

    private static ContentProblemException Forbidden() =>
        new("RECIPE_FORBIDDEN", "You do not have permission to access this recipe.", ContentProblemKind.Forbidden);
}
