using System.Diagnostics;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using CulinaryBlog.Application.Abstractions.Persistence;
using CulinaryBlog.Application.Content;
using CulinaryBlog.Application.Media;
using CulinaryBlog.Domain.Categories;
using CulinaryBlog.Domain.Common;
using CulinaryBlog.Domain.Recipes;
using CulinaryBlog.Infrastructure.Identity;
using CulinaryBlog.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Npgsql;
using NpgsqlTypes;

namespace CulinaryBlog.Infrastructure.Content;

internal sealed partial class ContentService(
    AppDbContext dbContext,
    IUnitOfWork unitOfWork,
    UserManager<ApplicationUser> userManager,
    IFileStorageService fileStorage,
    TimeProvider timeProvider,
    ILogger<ContentService> logger) : IContentService, IRecipeSearchRepository
{
    private const int MaximumPageSize = 50;

    private static readonly Action<ILogger, string, double, Exception?> SlowDiscoveryQuery =
        LoggerMessage.Define<string, double>(
            LogLevel.Warning,
            new EventId(4004, nameof(SlowDiscoveryQuery)),
            "Discovery query {QueryName} completed in {ElapsedMilliseconds:F1} ms, exceeding the 100 ms budget");

    public async Task<IReadOnlyCollection<CategoryDto>> ListCategoriesAsync(CancellationToken cancellationToken)
    {
        var categories = await dbContext.Categories
            .AsNoTracking()
            .OrderBy(category => category.OrderIndex)
            .ThenBy(category => category.Name)
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
        var categoryImage = category.ImageUrl
            ?? mapped.FirstOrDefault()?.Images.FirstOrDefault(i => i.IsPrimary)?.MediumUrl
            ?? mapped.FirstOrDefault()?.Images.FirstOrDefault()?.OriginalUrl
            ?? mapped.FirstOrDefault()?.PrimaryImageUrl;
        var categoryDto = ToCategoryDto(category, total, categoryImage);
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
            if (await unitOfWork.Recipes.SlugExistsAsync(slug, null, cancellationToken).ConfigureAwait(false))
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
            unitOfWork.Recipes.Add(recipe);
            try
            {
                await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
                ContentMetrics.RecipeCreated.Add(1);
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

    public async Task<RecipeDto> PublishRecipeAsync(
        Guid id,
        Guid userId,
        bool isAdmin,
        long expectedVersion,
        CancellationToken cancellationToken)
    {
        var recipe = await FindRecipeForMutationAsync(id, userId, isAdmin, cancellationToken).ConfigureAwait(false);
        EnsureVersion(recipe, expectedVersion);
        var categoryExists = await dbContext.Categories
            .AnyAsync(category => category.Id == recipe.CategoryId, cancellationToken)
            .ConfigureAwait(false);
        var ingredientCount = await dbContext.RecipeIngredients
            .CountAsync(ingredient => ingredient.RecipeId == recipe.Id, cancellationToken)
            .ConfigureAwait(false);
        var stepNumbers = await dbContext.RecipeSteps
            .Where(step => step.RecipeId == recipe.Id)
            .Select(step => step.StepNumber)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        recipe.Publish(timeProvider.GetUtcNow(), categoryExists, ingredientCount, stepNumbers);
        await SaveRecipeMutationAsync(recipe, cancellationToken).ConfigureAwait(false);
        ContentMetrics.RecipePublished.Add(1);
        return await MapRecipeAsync(recipe, cancellationToken).ConfigureAwait(false);
    }

    public async Task<RecipeDto> UnpublishRecipeAsync(
        Guid id,
        Guid userId,
        bool isAdmin,
        long expectedVersion,
        CancellationToken cancellationToken)
    {
        var recipe = await FindRecipeForMutationAsync(id, userId, isAdmin, cancellationToken).ConfigureAwait(false);
        EnsureVersion(recipe, expectedVersion);
        recipe.Unpublish();
        await SaveRecipeMutationAsync(recipe, cancellationToken).ConfigureAwait(false);
        ContentMetrics.RecipeUnpublished.Add(1);
        return await MapRecipeAsync(recipe, cancellationToken).ConfigureAwait(false);
    }

    public async Task<RecipeDto> ArchiveRecipeAsync(
        Guid id,
        Guid userId,
        bool isAdmin,
        long expectedVersion,
        CancellationToken cancellationToken)
    {
        var recipe = await FindRecipeForMutationAsync(id, userId, isAdmin, cancellationToken).ConfigureAwait(false);
        EnsureVersion(recipe, expectedVersion);
        recipe.Archive();
        await SaveRecipeMutationAsync(recipe, cancellationToken).ConfigureAwait(false);
        return await MapRecipeAsync(recipe, cancellationToken).ConfigureAwait(false);
    }

    public async Task<RecipeDto> UnarchiveRecipeAsync(
        Guid id,
        Guid userId,
        bool isAdmin,
        long expectedVersion,
        CancellationToken cancellationToken)
    {
        var recipe = await FindRecipeForMutationAsync(id, userId, isAdmin, cancellationToken).ConfigureAwait(false);
        EnsureVersion(recipe, expectedVersion);
        recipe.Unarchive();
        await SaveRecipeMutationAsync(recipe, cancellationToken).ConfigureAwait(false);
        return await MapRecipeAsync(recipe, cancellationToken).ConfigureAwait(false);
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

    async Task<PageEnvelope<RecipeDto>> IRecipeSearchRepository.ListPublishedRecipesAsync(
        Guid? categoryId,
        RecipeDifficulty? difficulty,
        int? maxCookTime,
        int? minServings,
        int? minPrepTime,
        int? maxPrepTime,
        string? sort,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        ValidatePage(page, pageSize);
        var stopwatch = Stopwatch.StartNew();
        var query = dbContext.Recipes.AsNoTracking().Where(recipe => recipe.Status == RecipeStatus.Published);

        if (categoryId.HasValue)
        {
            query = query.Where(recipe => recipe.CategoryId == categoryId.Value);
        }

        if (difficulty.HasValue)
        {
            query = query.Where(recipe => recipe.Difficulty == difficulty.Value);
        }

        if (maxCookTime.HasValue)
        {
            query = query.Where(recipe => recipe.CookTime <= maxCookTime.Value);
        }

        if (minServings.HasValue)
        {
            query = query.Where(recipe => recipe.Servings >= minServings.Value);
        }

        if (minPrepTime.HasValue)
        {
            query = query.Where(recipe => recipe.PrepTime >= minPrepTime.Value);
        }

        if (maxPrepTime.HasValue)
        {
            query = query.Where(recipe => recipe.PrepTime <= maxPrepTime.Value);
        }

        var total = await query.CountAsync(cancellationToken).ConfigureAwait(false);
        var ordered = sort switch
        {
            "createdAt" or "oldest" => query.OrderBy(recipe => recipe.CreatedAt).ThenBy(recipe => recipe.Id),
            "title" or "az" => query.OrderBy(recipe => recipe.Title).ThenBy(recipe => recipe.Id),
            "-title" or "za" => query.OrderByDescending(recipe => recipe.Title).ThenBy(recipe => recipe.Id),
            "cookTime" => query.OrderBy(recipe => recipe.CookTime).ThenByDescending(recipe => recipe.PublishedAt),
            "-cookTime" => query.OrderByDescending(recipe => recipe.CookTime).ThenByDescending(recipe => recipe.PublishedAt),
            "prepTime" => query.OrderBy(recipe => recipe.PrepTime).ThenByDescending(recipe => recipe.PublishedAt),
            "-prepTime" => query.OrderByDescending(recipe => recipe.PrepTime).ThenByDescending(recipe => recipe.PublishedAt),
            "servings" => query.OrderBy(recipe => recipe.Servings).ThenByDescending(recipe => recipe.PublishedAt),
            "-servings" => query.OrderByDescending(recipe => recipe.Servings).ThenByDescending(recipe => recipe.PublishedAt),
            _ => query.OrderByDescending(recipe => recipe.CreatedAt).ThenByDescending(recipe => recipe.Id),
        };
        var recipes = await ordered.Skip((page - 1) * pageSize).Take(pageSize)
            .ToListAsync(cancellationToken).ConfigureAwait(false);
        var result = new PageEnvelope<RecipeDto>(
            await MapRecipesAsync(recipes, cancellationToken).ConfigureAwait(false),
            CreateMeta(page, pageSize, total));
        LogSlowDiscoveryQuery(stopwatch, "list");
        return result;
    }

    public async Task<PageEnvelope<RecipeDto>> SearchPublishedRecipesAsync(
        string? search,
        string? category,
        RecipeDifficulty? difficulty,
        int? maxTime,
        string? sort,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        ValidatePage(page, pageSize);
        var stopwatch = Stopwatch.StartNew();
        var query = dbContext.Recipes.AsNoTracking().Where(recipe => recipe.Status == RecipeStatus.Published);
        var terms = Regex.Matches(RemoveAccents(search ?? string.Empty), @"[\p{L}\p{N}]+")
            .Select(match => match.Value)
            .Take(20)
            .ToArray();
        if (!string.IsNullOrWhiteSpace(search) && terms.Length == 0)
        {
            return new PageEnvelope<RecipeDto>([], CreateMeta(page, pageSize, 0));
        }

        var tsQuery = string.Join(" & ", terms.Select(term => term + ":*"));
        var normalizedSearch = string.Join(' ', terms);
        if (terms.Length > 0)
        {
            query = query.Where(recipe =>
                EF.Property<NpgsqlTsVector>(recipe, "SearchVector")
                    .Matches(EF.Functions.ToTsQuery("simple", tsQuery)) ||
                EF.Functions.TrigramsAreSimilar(
                    EF.Property<string>(recipe, "SearchTitle"), normalizedSearch));
        }

        if (!string.IsNullOrWhiteSpace(category))
        {
            query = query.Where(recipe => dbContext.Categories.Any(item =>
                item.Id == recipe.CategoryId && item.Slug == category));
        }

        if (difficulty.HasValue)
        {
            query = query.Where(recipe => recipe.Difficulty == difficulty.Value);
        }

        if (maxTime.HasValue)
        {
            query = query.Where(recipe => recipe.PrepTime + recipe.CookTime <= maxTime.Value);
        }

        var total = await query.CountAsync(cancellationToken).ConfigureAwait(false);
        var ordered = sort switch
        {
            "quickest" => query.OrderBy(recipe => recipe.PrepTime + recipe.CookTime)
                .ThenByDescending(recipe => recipe.PublishedAt),
            "az" => query.OrderBy(recipe => recipe.Title).ThenByDescending(recipe => recipe.PublishedAt),
            "newest" => query.OrderByDescending(recipe => recipe.PublishedAt),
            _ when terms.Length > 0 => query.OrderByDescending(recipe =>
                    EF.Property<NpgsqlTsVector>(recipe, "SearchVector")
                        .Rank(EF.Functions.ToTsQuery("simple", tsQuery)))
                .ThenByDescending(recipe => EF.Functions.TrigramsSimilarity(
                    EF.Property<string>(recipe, "SearchTitle"), normalizedSearch))
                .ThenByDescending(recipe => recipe.PublishedAt),
            _ => query.OrderByDescending(recipe => recipe.PublishedAt),
        };
        var recipes = await ordered.Skip((page - 1) * pageSize).Take(pageSize)
            .ToListAsync(cancellationToken).ConfigureAwait(false);
        var result = new PageEnvelope<RecipeDto>(
            await MapRecipesAsync(recipes, cancellationToken).ConfigureAwait(false),
            CreateMeta(page, pageSize, total));
        LogSlowDiscoveryQuery(stopwatch, "search");
        return result;
    }

    private void LogSlowDiscoveryQuery(Stopwatch stopwatch, string queryName)
    {
        stopwatch.Stop();
        if (stopwatch.ElapsedMilliseconds > 100)
        {
            SlowDiscoveryQuery(logger, queryName, stopwatch.Elapsed.TotalMilliseconds, null);
        }
    }

    private static string RemoveAccents(string value)
    {
        var normalized = value.Normalize(NormalizationForm.FormD);
        var result = new StringBuilder(normalized.Length);
        foreach (var character in normalized)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(character) != UnicodeCategory.NonSpacingMark)
            {
                result.Append(character is 'đ' or 'Đ' ? 'd' : char.ToLowerInvariant(character));
            }
        }

        return result.ToString().Normalize(NormalizationForm.FormC);
    }

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
        var recipe = await unitOfWork.Recipes.GetByIdAsync(id, cancellationToken)
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
            await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (Exception exception) when (IsUniqueViolation(exception, "Slug"))
        {
            throw Conflict("RECIPE_SLUG_EXISTS", "A recipe already owns this slug.");
        }
        catch (Exception exception) when (IsUniqueViolation(exception, "RecipeImages"))
        {
            throw Conflict("IMAGE_PRIMARY_CONFLICT", "Choose another primary image before clearing the current primary.");
        }
        catch (Exception exception) when (IsUniqueViolation(exception, "RecipeSteps") || IsUniqueViolation(exception, "StepNumber"))
        {
            throw Conflict("STEP_NUMBER_CONFLICT", "A step number conflict occurred.");
        }
        catch (Exception exception) when (IsDeadlockOrSerialization(exception))
        {
            throw new ContentProblemException(
                "RECIPE_CONCURRENCY_CONFLICT", "The recipe was changed by another request.",
                ContentProblemKind.Conflict);
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
            if (!await unitOfWork.Recipes.SlugExistsAsync(candidate, recipeId, cancellationToken)
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
        if (recipes.Count == 0)
        {
            return [];
        }

        var recipeIds = recipes.Select(recipe => recipe.Id).ToArray();
        var categoryIds = recipes.Select(recipe => recipe.CategoryId).Distinct().ToArray();
        var authorIds = recipes.Select(recipe => recipe.AuthorId).Distinct().ToArray();
        var categories = await dbContext.Categories.AsNoTracking()
            .Where(category => categoryIds.Contains(category.Id))
            .ToDictionaryAsync(category => category.Id, cancellationToken)
            .ConfigureAwait(false);
        var categoryCounts = await dbContext.Recipes.AsNoTracking()
            .Where(recipe => recipe.Status == RecipeStatus.Published && categoryIds.Contains(recipe.CategoryId))
            .GroupBy(recipe => recipe.CategoryId)
            .Select(group => new { CategoryId = group.Key, Count = group.Count() })
            .ToDictionaryAsync(item => item.CategoryId, item => item.Count, cancellationToken)
            .ConfigureAwait(false);
        var authors = await dbContext.Users.AsNoTracking()
            .Where(author => authorIds.Contains(author.Id))
            .ToDictionaryAsync(author => author.Id, cancellationToken)
            .ConfigureAwait(false);
        var roleRows = await (
                from userRole in dbContext.UserRoles.AsNoTracking()
                join role in dbContext.Roles.AsNoTracking() on userRole.RoleId equals role.Id
                where authorIds.Contains(userRole.UserId)
                select new { userRole.UserId, role.Name })
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
        var rolesByAuthor = roleRows
            .ToLookup(item => item.UserId, item => item.Name ?? string.Empty);
        var ingredientRows = await dbContext.RecipeIngredients.AsNoTracking()
            .Where(item => recipeIds.Contains(item.RecipeId))
            .OrderBy(item => item.OrderIndex)
            .ThenBy(item => item.CreatedAt)
            .Select(item => new
            {
                item.RecipeId,
                Dto = new IngredientDto(item.Id, item.Name, item.Quantity, item.Unit, item.Notes, item.OrderIndex),
            })
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
        var ingredientsByRecipe = ingredientRows.ToLookup(item => item.RecipeId, item => item.Dto);
        var stepRows = await dbContext.RecipeSteps.AsNoTracking()
            .Where(item => recipeIds.Contains(item.RecipeId))
            .OrderBy(item => item.StepNumber)
            .Select(item => new
            {
                item.RecipeId,
                Dto = new StepDto(item.Id, item.StepNumber, item.Title, item.Description, item.TimerMinutes, item.ImageUrl),
            })
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
        var stepsByRecipe = stepRows.ToLookup(item => item.RecipeId, item => item.Dto);
        var imageRows = await dbContext.RecipeImages.AsNoTracking()
            .Where(item => recipeIds.Contains(item.RecipeId))
            .OrderBy(item => item.OrderIndex)
            .ThenBy(item => item.CreatedAt)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
        var imagesByRecipe = imageRows.ToLookup(item => item.RecipeId, item => new RecipeImageDto(
            item.Id,
            MediaUrl(item.Id, "original"),
            item.MediumObjectKey == null ? null : MediaUrl(item.Id, "medium"),
            item.ThumbnailObjectKey == null ? null : MediaUrl(item.Id, "thumbnail"),
            item.AltText,
            item.IsPrimary,
            item.OrderIndex,
            item.ProcessingStatus.ToString().ToLowerInvariant()));

        return recipes.Select(recipe =>
        {
            var category = categories[recipe.CategoryId];
            var author = authors[recipe.AuthorId];
            var images = imagesByRecipe[recipe.Id].ToArray();
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
                images.FirstOrDefault(image => image.IsPrimary)?.ThumbnailUrl,
                ToCategoryDto(category, categoryCounts.GetValueOrDefault(category.Id)),
                new RecipeAuthorDto(
                    author.Id.ToString(),
                    author.Email ?? string.Empty,
                    author.DisplayName,
                    author.AvatarUrl,
                    author.Bio,
                    rolesByAuthor[author.Id].ToArray(),
                    author.EmailConfirmed,
                    author.IsActive,
                    author.CreatedAt),
                recipe.CreatedAt,
                recipe.PublishedAt,
                recipe.Version,
                recipe.Instructions,
                RecipeMappings.ToNutritionDto(recipe.Nutrition),
                ingredientsByRecipe[recipe.Id].ToArray(),
                stepsByRecipe[recipe.Id].ToArray(),
                images);
        }).ToArray();
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
        var ingredients = await dbContext.RecipeIngredients.AsNoTracking()
            .Where(item => item.RecipeId == recipe.Id)
            .OrderBy(item => item.OrderIndex)
            .ThenBy(item => item.CreatedAt)
            .Select(item => new IngredientDto(item.Id, item.Name, item.Quantity, item.Unit, item.Notes, item.OrderIndex))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
        var steps = await dbContext.RecipeSteps.AsNoTracking()
            .Where(item => item.RecipeId == recipe.Id)
            .OrderBy(item => item.StepNumber)
            .Select(item => new StepDto(item.Id, item.StepNumber, item.Title, item.Description, item.TimerMinutes, item.ImageUrl))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
        var imageEntities = await dbContext.RecipeImages.AsNoTracking()
            .Where(item => item.RecipeId == recipe.Id)
            .OrderBy(item => item.OrderIndex)
            .ThenBy(item => item.CreatedAt)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
        var images = imageEntities.Select(item => new RecipeImageDto(
                item.Id,
                MediaUrl(item.Id, "original"),
                item.MediumObjectKey == null ? null : MediaUrl(item.Id, "medium"),
                item.ThumbnailObjectKey == null ? null : MediaUrl(item.Id, "thumbnail"),
                item.AltText,
                item.IsPrimary,
                item.OrderIndex,
                item.ProcessingStatus.ToString().ToLowerInvariant()))
            .ToArray();
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
            images.FirstOrDefault(image => image.IsPrimary)?.ThumbnailUrl,
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
            RecipeMappings.ToNutritionDto(recipe.Nutrition),
            ingredients,
            steps,
            images);
    }

    private static readonly Dictionary<string, string> PreferredCategoryRecipeSlugs = new(StringComparer.OrdinalIgnoreCase)
    {
        ["mon-nuoc"] = "pho-bo-ha-noi",
    };

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

        var categoryImages = await (
            from image in dbContext.RecipeImages.AsNoTracking()
            join recipe in dbContext.Recipes.AsNoTracking() on image.RecipeId equals recipe.Id
            where recipe.Status == RecipeStatus.Published && image.ProcessingStatus == ImageProcessingStatus.Ready
            orderby image.IsPrimary descending, recipe.PublishedAt descending, image.OrderIndex
            select new
            {
                recipe.CategoryId,
                RecipeSlug = recipe.Slug,
                ImageId = image.Id,
                HasMedium = image.MediumObjectKey != null,
            }
        ).ToListAsync(cancellationToken).ConfigureAwait(false);

        var imageByCategory = new Dictionary<Guid, string>();
        foreach (var category in categories)
        {
            var imagesForCategory = categoryImages.Where(item => item.CategoryId == category.Id).ToList();
            if (imagesForCategory.Count == 0) continue;

            if (PreferredCategoryRecipeSlugs.TryGetValue(category.Slug, out var preferredSlug))
            {
                var preferred = imagesForCategory.FirstOrDefault(item => string.Equals(item.RecipeSlug, preferredSlug, StringComparison.OrdinalIgnoreCase));
                if (preferred != null)
                {
                    imageByCategory[category.Id] = MediaUrl(preferred.ImageId, preferred.HasMedium ? "medium" : "original");
                    continue;
                }
            }

            var first = imagesForCategory.First();
            imageByCategory[category.Id] = MediaUrl(first.ImageId, first.HasMedium ? "medium" : "original");
        }

        return categories.Select(category => ToCategoryDto(
            category,
            counts.GetValueOrDefault(category.Id),
            category.ImageUrl ?? imageByCategory.GetValueOrDefault(category.Id))).ToArray();
    }

    private static CategoryDto ToCategoryDto(Category category, int recipeCount, string? resolvedImageUrl = null) =>
        new(category.Id, category.Name, category.Slug, category.Description, resolvedImageUrl ?? category.ImageUrl, category.OrderIndex, recipeCount);

    private static RecipeNutrition? ToNutrition(NutritionDto? nutrition) => nutrition is null
        ? null
        : RecipeNutrition.Create(
            nutrition.Calories,
            nutrition.Protein,
            nutrition.Carbohydrates,
            nutrition.Fat,
            nutrition.Fiber,
            nutrition.Sodium);

    private static string MediaUrl(Guid imageId, string variant) => $"/api/v1/media/{imageId}/{variant}";

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

    private static PostgresException? GetPostgresException(Exception exception)
    {
        for (var current = exception; current is not null; current = current.InnerException)
        {
            if (current is PostgresException pgEx)
            {
                return pgEx;
            }
        }
        return null;
    }

    private static bool IsUniqueViolation(Exception exception, string property)
    {
        var postgresException = GetPostgresException(exception);
        return postgresException is not null &&
            postgresException.SqlState == PostgresErrorCodes.UniqueViolation &&
            (postgresException.ConstraintName?.Contains(property, StringComparison.OrdinalIgnoreCase) ?? false);
    }

    private static bool IsDeadlockOrSerialization(Exception exception)
    {
        var postgresException = GetPostgresException(exception);
        return postgresException is not null &&
            (postgresException.SqlState == PostgresErrorCodes.DeadlockDetected ||
             postgresException.SqlState == PostgresErrorCodes.SerializationFailure);
    }

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
