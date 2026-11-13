// Culinary Blog. Authorship is recorded in the repository history.

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

/// <summary>Represents the content service.</summary>
/// <param name="dbContext">The db context supplied to this operation.</param>
/// <param name="unitOfWork">The unit of work supplied to this operation.</param>
/// <param name="userManager">The user manager supplied to this operation.</param>
/// <param name="fileStorage">The file storage supplied to this operation.</param>
/// <param name="timeProvider">The time provider supplied to this operation.</param>
/// <param name="logger">The logger supplied to this operation.</param>
internal sealed partial class ContentService(
    AppDbContext dbContext,
    IUnitOfWork unitOfWork,
    UserManager<ApplicationUser> userManager,
    IFileStorageService fileStorage,
    TimeProvider timeProvider,
    ILogger<ContentService> logger) : IContentService, IRecipeSearchRepository
{
    private const int MaximumPageSize = 50;
    private const string EmptySearchMessage = "Không tìm thấy công thức. Hãy thử từ khóa khác hoặc giảm bộ lọc.";

    private static readonly Action<ILogger, string, double, Exception?> SlowDiscoveryQuery =
        LoggerMessage.Define<string, double>(
            LogLevel.Warning,
            new EventId(4004, nameof(SlowDiscoveryQuery)),
            "Discovery query {QueryName} completed in {ElapsedMilliseconds:F1} ms, exceeding the 100 ms budget");

    private static readonly Dictionary<string, string> PreferredCategoryRecipeSlugs = new(StringComparer.OrdinalIgnoreCase)
    {
        ["mon-nuoc"] = "pho-bo-ha-noi",
    };

    /// <summary>Lists categories.</summary>
    /// <param name="cancellationToken">Cancels the operation when requested by the caller.</param>
    /// <returns>A task representing the operation and its result.</returns>
    public async Task<IReadOnlyCollection<CategoryDto>> ListCategoriesAsync(CancellationToken cancellationToken)
    {
        var categories = await dbContext.Categories
            .AsNoTracking()
            .OrderBy(category => category.Name)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
        return await this.MapCategoriesAsync(categories, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Gets category.</summary>
    /// <param name="slug">The slug supplied to this operation.</param>
    /// <param name="page">The page supplied to this operation.</param>
    /// <param name="pageSize">The page size supplied to this operation.</param>
    /// <param name="cancellationToken">Cancels the operation when requested by the caller.</param>
    /// <param name="sort">The sort supplied to this operation.</param>
    /// <returns>A task representing the operation and its result.</returns>
    public async Task<CategoryDetailEnvelope> GetCategoryAsync(
        string slug,
        int page,
        int pageSize,
        CancellationToken cancellationToken,
        string? sort = null)
    {
        ValidatePage(page, pageSize);
        var category = await dbContext.Categories.AsNoTracking()
            .SingleOrDefaultAsync(item => item.Slug == slug, cancellationToken)
            .ConfigureAwait(false)
            ?? throw NotFound("CATEGORY_NOT_FOUND", "Category was not found.");

        var filtered = dbContext.Recipes.AsNoTracking()
            .Where(recipe => recipe.CategoryId == category.Id && recipe.Status == RecipeStatus.Published);
        var query = sort is null
            ? filtered.OrderByDescending(recipe => recipe.PublishedAt).ThenByDescending(recipe => recipe.CreatedAt).ThenBy(recipe => recipe.Id)
            : OrderPublicRecipes(filtered, sort);
        var total = await query.CountAsync(cancellationToken).ConfigureAwait(false);
        var recipes = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(cancellationToken)
            .ConfigureAwait(false);
        var mapped = await this.MapRecipesAsync(recipes, cancellationToken).ConfigureAwait(false);
        var categoryImage = category.ImageUrl
            ?? mapped.FirstOrDefault()?.Images.FirstOrDefault(i => i.IsPrimary)?.MediumUrl
            ?? mapped.FirstOrDefault()?.Images.FirstOrDefault()?.OriginalUrl
            ?? mapped.FirstOrDefault()?.PrimaryImageUrl;
        var categoryDto = ToCategoryDto(category, total, categoryImage);
        return new CategoryDetailEnvelope(
            new CategoryDetailData(categoryDto, mapped),
            CreateMeta(page, pageSize, total));
    }

    /// <summary>Creates category.</summary>
    /// <param name="request">The request supplied to this operation.</param>
    /// <param name="cancellationToken">Cancels the operation when requested by the caller.</param>
    /// <returns>A task representing the operation and its result.</returns>
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

    /// <summary>Updates category.</summary>
    /// <param name="id">The identifier of the requested resource.</param>
    /// <param name="request">The request supplied to this operation.</param>
    /// <param name="cancellationToken">Cancels the operation when requested by the caller.</param>
    /// <returns>A task representing the operation and its result.</returns>
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

    /// <summary>Deletes category.</summary>
    /// <param name="id">The identifier of the requested resource.</param>
    /// <param name="cancellationToken">Cancels the operation when requested by the caller.</param>
    /// <returns>A task representing the operation and its result.</returns>
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

    /// <summary>Creates recipe.</summary>
    /// <param name="userId">The identifier of the user performing or owning the operation.</param>
    /// <param name="request">The request supplied to this operation.</param>
    /// <param name="cancellationToken">Cancels the operation when requested by the caller.</param>
    /// <returns>A task representing the operation and its result.</returns>
    public async Task<RecipeDto> CreateRecipeAsync(
        string userId,
        RecipeCreateRequest request,
        CancellationToken cancellationToken)
    {
        await this.EnsureCategoryExistsAsync(request.CategoryId, cancellationToken).ConfigureAwait(false);
        var baseSlug = Slug.From(request.Title, 220);

        for (var suffix = 1; suffix <= 20; suffix++)
        {
            var slug = WithSuffix(baseSlug, suffix, 220);
            if (await unitOfWork.Recipes.SlugExistsAsync(slug, null, cancellationToken).ConfigureAwait(false) ||
                await dbContext.RecipeSlugs.AnyAsync(item => item.Slug == slug, cancellationToken).ConfigureAwait(false))
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
            var reservation = RecipeSlug.Reserve(slug, recipe.Id);
            dbContext.RecipeSlugs.Add(reservation);
            var ingredients = (request.Ingredients ?? []).Select(item => RecipeIngredient.Create(
                Guid.NewGuid(), recipe.Id, item.Name, item.Quantity, item.Unit, item.Notes, item.OrderIndex)).ToArray();
            var steps = (request.Steps ?? []).Select((item, index) => RecipeStep.Create(
                Guid.NewGuid(), recipe.Id, index + 1, item.Title, item.Description, item.TimerMinutes, item.ImageUrl)).ToArray();
            dbContext.RecipeIngredients.AddRange(ingredients);
            dbContext.RecipeSteps.AddRange(steps);
            try
            {
                await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
                ContentMetrics.RecipeCreated.Add(1);
                return await this.MapRecipeAsync(recipe, cancellationToken).ConfigureAwait(false);
            }
            catch (DbUpdateException exception) when (IsUniqueViolation(exception, "Slug"))
            {
                dbContext.Entry(recipe).State = EntityState.Detached;
                dbContext.Entry(reservation).State = EntityState.Detached;
                foreach (var ingredient in ingredients)
                {
                    dbContext.Entry(ingredient).State = EntityState.Detached;
                }

                foreach (var step in steps)
                {
                    dbContext.Entry(step).State = EntityState.Detached;
                }
            }
        }

        throw Conflict("RECIPE_SLUG_EXISTS", "A unique recipe slug could not be allocated.");
    }

    /// <summary>Updates recipe.</summary>
    /// <param name="id">The identifier of the requested resource.</param>
    /// <param name="userId">The identifier of the user performing or owning the operation.</param>
    /// <param name="isAdmin">The is admin supplied to this operation.</param>
    /// <param name="expectedVersion">The recipe version used to reject concurrent changes.</param>
    /// <param name="request">The request supplied to this operation.</param>
    /// <param name="cancellationToken">Cancels the operation when requested by the caller.</param>
    /// <returns>A task representing the operation and its result.</returns>
    public async Task<RecipeDto> UpdateRecipeAsync(
        Guid id,
        string userId,
        bool isAdmin,
        long expectedVersion,
        RecipeUpdateRequest request,
        CancellationToken cancellationToken)
    {
        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken).ConfigureAwait(false);
        await dbContext.Database.ExecuteSqlInterpolatedAsync($"SELECT 1 FROM \"Recipes\" WHERE \"Id\" = {id} FOR UPDATE", cancellationToken).ConfigureAwait(false);

        // Authorization may have loaded an earlier snapshot. Reload both the root and its table-shared owned data.
        if (dbContext.Recipes.Local.SingleOrDefault(item => item.Id == id) is { } previous)
        {
            var nutrition = dbContext.Entry(previous).Reference(item => item.Nutrition).TargetEntry;
            if (nutrition is not null)
            {
                nutrition.State = EntityState.Detached;
            }

            dbContext.Entry(previous).State = EntityState.Detached;
        }

        var recipe = await this.FindRecipeForMutationAsync(id, userId, isAdmin, cancellationToken).ConfigureAwait(false);
        EnsureVersion(recipe, expectedVersion);
        if (request == new RecipeUpdateRequest())
        {
            return await this.MapRecipeAsync(recipe, cancellationToken).ConfigureAwait(false);
        }

        var categoryId = request.CategoryId.Or(recipe.CategoryId)!.Value;
        var title = request.Title.Or(recipe.Title)!;
        await this.EnsureCategoryExistsAsync(categoryId, cancellationToken).ConfigureAwait(false);
        var slug = recipe.PublishedAt is null
            ? await this.AllocateRecipeSlugAsync(title, recipe.Id, cancellationToken).ConfigureAwait(false)
            : recipe.Slug;
        if (slug != recipe.Slug && !await dbContext.RecipeSlugs.AnyAsync(item => item.Slug == recipe.Slug, cancellationToken).ConfigureAwait(false))
        {
            dbContext.RecipeSlugs.Add(RecipeSlug.Reserve(recipe.Slug, recipe.Id));
        }

        if (slug != recipe.Slug && !await dbContext.RecipeSlugs.AnyAsync(item => item.Slug == slug, cancellationToken).ConfigureAwait(false))
        {
            dbContext.RecipeSlugs.Add(RecipeSlug.Reserve(slug, recipe.Id));
        }

        recipe.Update(
            slug,
            title,
            request.Description.Or(recipe.Description)!,
            categoryId,
            request.PrepTime.Or(recipe.PrepTime)!.Value,
            request.CookTime.Or(recipe.CookTime)!.Value,
            request.Servings.Or(recipe.Servings)!.Value,
            request.Difficulty.Or(recipe.Difficulty)!.Value,
            request.Instructions.Or(recipe.Instructions),
            request.Nutrition.IsSpecified ? MergeNutrition(recipe.Nutrition, request.Nutrition.Value) : recipe.Nutrition);
        recipe.MarkCompositionChanged();
        await this.SaveRecipeMutationAsync(cancellationToken).ConfigureAwait(false);
        await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
        return await this.MapRecipeAsync(recipe, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Publishes recipe.</summary>
    /// <param name="id">The identifier of the requested resource.</param>
    /// <param name="userId">The identifier of the user performing or owning the operation.</param>
    /// <param name="isAdmin">The is admin supplied to this operation.</param>
    /// <param name="expectedVersion">The recipe version used to reject concurrent changes.</param>
    /// <param name="cancellationToken">Cancels the operation when requested by the caller.</param>
    /// <returns>A task representing the operation and its result.</returns>
    public async Task<RecipeDto> PublishRecipeAsync(
        Guid id,
        string userId,
        bool isAdmin,
        long expectedVersion,
        CancellationToken cancellationToken)
    {
        var recipe = await this.FindRecipeForMutationAsync(id, userId, isAdmin, cancellationToken).ConfigureAwait(false);
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
        await this.SaveRecipeMutationAsync(cancellationToken).ConfigureAwait(false);
        ContentMetrics.RecipePublished.Add(1);
        return await this.MapRecipeAsync(recipe, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Unpublishes recipe.</summary>
    /// <param name="id">The identifier of the requested resource.</param>
    /// <param name="userId">The identifier of the user performing or owning the operation.</param>
    /// <param name="isAdmin">The is admin supplied to this operation.</param>
    /// <param name="expectedVersion">The recipe version used to reject concurrent changes.</param>
    /// <param name="cancellationToken">Cancels the operation when requested by the caller.</param>
    /// <returns>A task representing the operation and its result.</returns>
    public async Task<RecipeDto> UnpublishRecipeAsync(
        Guid id,
        string userId,
        bool isAdmin,
        long expectedVersion,
        CancellationToken cancellationToken)
    {
        var recipe = await this.FindRecipeForMutationAsync(id, userId, isAdmin, cancellationToken).ConfigureAwait(false);
        EnsureVersion(recipe, expectedVersion);
        recipe.Unpublish();
        await this.SaveRecipeMutationAsync(cancellationToken).ConfigureAwait(false);
        ContentMetrics.RecipeUnpublished.Add(1);
        return await this.MapRecipeAsync(recipe, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Archives recipe.</summary>
    /// <param name="id">The identifier of the requested resource.</param>
    /// <param name="userId">The identifier of the user performing or owning the operation.</param>
    /// <param name="isAdmin">The is admin supplied to this operation.</param>
    /// <param name="expectedVersion">The recipe version used to reject concurrent changes.</param>
    /// <param name="cancellationToken">Cancels the operation when requested by the caller.</param>
    /// <returns>A task representing the operation and its result.</returns>
    public async Task<RecipeDto> ArchiveRecipeAsync(
        Guid id,
        string userId,
        bool isAdmin,
        long expectedVersion,
        CancellationToken cancellationToken)
    {
        var recipe = await this.FindRecipeForMutationAsync(id, userId, isAdmin, cancellationToken).ConfigureAwait(false);
        EnsureVersion(recipe, expectedVersion);
        recipe.Archive();
        await this.SaveRecipeMutationAsync(cancellationToken).ConfigureAwait(false);
        return await this.MapRecipeAsync(recipe, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Unarchives recipe.</summary>
    /// <param name="id">The identifier of the requested resource.</param>
    /// <param name="userId">The identifier of the user performing or owning the operation.</param>
    /// <param name="isAdmin">The is admin supplied to this operation.</param>
    /// <param name="expectedVersion">The recipe version used to reject concurrent changes.</param>
    /// <param name="cancellationToken">Cancels the operation when requested by the caller.</param>
    /// <returns>A task representing the operation and its result.</returns>
    public async Task<RecipeDto> UnarchiveRecipeAsync(
        Guid id,
        string userId,
        bool isAdmin,
        long expectedVersion,
        CancellationToken cancellationToken)
    {
        var recipe = await this.FindRecipeForMutationAsync(id, userId, isAdmin, cancellationToken).ConfigureAwait(false);
        EnsureVersion(recipe, expectedVersion);
        recipe.Unarchive();
        await this.SaveRecipeMutationAsync(cancellationToken).ConfigureAwait(false);
        return await this.MapRecipeAsync(recipe, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Gets published recipe.</summary>
    /// <param name="slug">The slug supplied to this operation.</param>
    /// <param name="cancellationToken">Cancels the operation when requested by the caller.</param>
    /// <returns>A task representing the operation and its result.</returns>
    public async Task<RecipeDto> GetPublishedRecipeAsync(string slug, CancellationToken cancellationToken)
    {
        var recipe = await dbContext.Recipes.AsNoTracking()
            .SingleOrDefaultAsync(
                item => item.Status == RecipeStatus.Published &&
                    (item.Slug == slug || dbContext.RecipeSlugs.Any(alias => alias.Slug == slug && alias.RecipeId == item.Id)),
                cancellationToken)
            .ConfigureAwait(false)
            ?? throw NotFound("RECIPE_NOT_FOUND", "Recipe was not found.");
        return await this.MapRecipeAsync(recipe, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Lists published recipes.</summary>
    /// <param name="page">The page supplied to this operation.</param>
    /// <param name="pageSize">The page size supplied to this operation.</param>
    /// <param name="cancellationToken">Cancels the operation when requested by the caller.</param>
    /// <returns>A task representing the operation and its result.</returns>
    public Task<PageEnvelope<RecipeDto>> ListPublishedRecipesAsync(
        int page,
        int pageSize,
        CancellationToken cancellationToken) =>
        this.ListRecipesAsync(
            dbContext.Recipes.AsNoTracking().Where(recipe => recipe.Status == RecipeStatus.Published),
            page,
            pageSize,
            cancellationToken);

    /// <summary>Lists published recipes.</summary>
    /// <param name="categoryId">The category id supplied to this operation.</param>
    /// <param name="difficulty">The difficulty supplied to this operation.</param>
    /// <param name="maxCookTime">The max cook time supplied to this operation.</param>
    /// <param name="minServings">The min servings supplied to this operation.</param>
    /// <param name="minPrepTime">The min prep time supplied to this operation.</param>
    /// <param name="maxPrepTime">The max prep time supplied to this operation.</param>
    /// <param name="sort">The sort supplied to this operation.</param>
    /// <param name="page">The page supplied to this operation.</param>
    /// <param name="pageSize">The page size supplied to this operation.</param>
    /// <param name="category">The category supplied to this operation.</param>
    /// <param name="maxTime">The max time supplied to this operation.</param>
    /// <param name="cancellationToken">Cancels the operation when requested by the caller.</param>
    /// <returns>A task representing the operation and its result.</returns>
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
        string? category,
        int? maxTime,
        CancellationToken cancellationToken)
    {
        ValidatePage(page, pageSize);
        var stopwatch = Stopwatch.StartNew();
        var query = dbContext.Recipes.AsNoTracking().Where(recipe => recipe.Status == RecipeStatus.Published);

        category = category?.Normalize(NormalizationForm.FormKC).Trim().ToLowerInvariant();
        if (!string.IsNullOrEmpty(category))
        {
            query = query.Where(recipe => dbContext.Categories.Any(item => item.Id == recipe.CategoryId && item.Slug == category));
        }

        if (maxTime.HasValue)
        {
            query = query.Where(recipe => recipe.PrepTime + recipe.CookTime <= maxTime.Value);
        }

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
        var ordered = OrderPublicRecipes(query, sort);
        var recipes = await ordered.Skip((page - 1) * pageSize).Take(pageSize)
            .ToListAsync(cancellationToken).ConfigureAwait(false);
        var result = new PageEnvelope<RecipeDto>(
            await this.MapRecipesAsync(recipes, cancellationToken).ConfigureAwait(false),
            CreateMeta(page, pageSize, total));
        this.LogSlowDiscoveryQuery(stopwatch, "list");
        return result;
    }

    /// <summary>Searches published recipes.</summary>
    /// <param name="search">The search supplied to this operation.</param>
    /// <param name="category">The category supplied to this operation.</param>
    /// <param name="difficulty">The difficulty supplied to this operation.</param>
    /// <param name="maxTime">The max time supplied to this operation.</param>
    /// <param name="sort">The sort supplied to this operation.</param>
    /// <param name="page">The page supplied to this operation.</param>
    /// <param name="pageSize">The page size supplied to this operation.</param>
    /// <param name="categoryId">The category id supplied to this operation.</param>
    /// <param name="maxCookTime">The max cook time supplied to this operation.</param>
    /// <param name="minServings">The min servings supplied to this operation.</param>
    /// <param name="minPrepTime">The min prep time supplied to this operation.</param>
    /// <param name="maxPrepTime">The max prep time supplied to this operation.</param>
    /// <param name="cancellationToken">Cancels the operation when requested by the caller.</param>
    /// <returns>A task representing the operation and its result.</returns>
    public async Task<PageEnvelope<RecipeDto>> SearchPublishedRecipesAsync(
        string? search,
        string? category,
        RecipeDifficulty? difficulty,
        int? maxTime,
        string? sort,
        int page,
        int pageSize,
        Guid? categoryId,
        int? maxCookTime,
        int? minServings,
        int? minPrepTime,
        int? maxPrepTime,
        CancellationToken cancellationToken)
    {
        ValidatePage(page, pageSize);
        var stopwatch = Stopwatch.StartNew();
        category = category?.Normalize(NormalizationForm.FormKC).Trim().ToLowerInvariant();
        search = search?.Normalize(NormalizationForm.FormKC);
        var query = dbContext.Recipes.AsNoTracking().Where(recipe => recipe.Status == RecipeStatus.Published);
        var rawTerms = Regex.Matches((search ?? string.Empty).Normalize(NormalizationForm.FormKC), @"[\p{L}\p{N}]+", RegexOptions.None, TimeSpan.FromSeconds(1))
            .Select(match => match.Value).Take(20).ToArray();
        var terms = rawTerms.Select(RemoveAccents).ToArray();
        if (!string.IsNullOrWhiteSpace(search) && terms.Length == 0)
        {
            return new PageEnvelope<RecipeDto>([], CreateMeta(page, pageSize, 0), EmptySearchMessage);
        }

        var normalizedSearch = string.Join(' ', terms);
        var tsQuery = string.Join(" & ", terms.Select(term => term + ":*A"));

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

        if (categoryId.HasValue)
        {
            query = query.Where(recipe => recipe.CategoryId == categoryId.Value);
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

        if (terms.Length > 0)
        {
            // SearchVector weight A contains title lexemes; description lexemes have weight B.
            var exactQuery = string.Join(" & ", terms.Select(term => term + ":A"));
            var exactMatches = query.Where(recipe => EF.Property<NpgsqlTsVector>(recipe, "SearchVector")
                .Matches(EF.Functions.ToTsQuery("simple", exactQuery)));
            var accentedTerms = rawTerms.Where(term => !string.Equals(RemoveAccents(term), term, StringComparison.OrdinalIgnoreCase)).ToArray();
            var accentedMatches = exactMatches;
            foreach (var term in accentedTerms)
            {
                accentedMatches = accentedMatches.Where(BuildAccentWordPredicate(term));
            }

            if (accentedTerms.Length > 0 && await accentedMatches.AnyAsync(cancellationToken).ConfigureAwait(false))
            {
                query = accentedMatches;
                tsQuery = exactQuery;
            }
            else if (await exactMatches.AnyAsync(cancellationToken).ConfigureAwait(false))
            {
                query = exactMatches;
                tsQuery = exactQuery;
            }
            else
            {
                var prefixMatches = query.Where(recipe => EF.Property<NpgsqlTsVector>(recipe, "SearchVector")
                    .Matches(EF.Functions.ToTsQuery("simple", tsQuery)));
                var knownShortTerms = new HashSet<string>(StringComparer.Ordinal);
                foreach (var term in terms.Where(term => term.Length <= 2).Distinct())
                {
                    if (await dbContext.Recipes.AnyAsync(
                        recipe => recipe.Status == RecipeStatus.Published &&
                            EF.Property<NpgsqlTsVector>(recipe, "SearchVector")
                                .Matches(EF.Functions.ToTsQuery("simple", term + ":A")),
                        cancellationToken).ConfigureAwait(false))
                    {
                        knownShortTerms.Add(term);
                        prefixMatches = prefixMatches.Where(recipe => EF.Property<NpgsqlTsVector>(recipe, "SearchVector")
                            .Matches(EF.Functions.ToTsQuery("simple", term + ":A")));
                    }
                }

                if (await prefixMatches.AnyAsync(cancellationToken).ConfigureAwait(false))
                {
                    query = prefixMatches;
                }
                else
                {
                    // Approximate names are a fallback, and every query word must still match.
                    foreach (var term in terms)
                    {
                        var termQuery = knownShortTerms.Contains(term) ? term + ":A" : term + ":*A";
                        var allowTypo = term.Length >= 4;
                        query = query.Where(recipe => EF.Property<NpgsqlTsVector>(recipe, "SearchVector")
                            .Matches(EF.Functions.ToTsQuery("simple", termQuery)) ||
                            (allowTypo && EF.Functions.TrigramsAreWordSimilar(
                                term,
                                EF.Property<string>(recipe, "SearchTitle"))));
                    }
                }
            }
        }

        var total = await query.CountAsync(cancellationToken).ConfigureAwait(false);
        var ordered = terms.Length > 0 && (string.IsNullOrEmpty(sort) || sort == "relevance")
            ? query.OrderByDescending(recipe => EF.Property<NpgsqlTsVector>(recipe, "SearchVector")
                    .Rank(EF.Functions.ToTsQuery("simple", tsQuery)))
                .ThenByDescending(recipe => EF.Functions.TrigramsSimilarity(EF.Property<string>(recipe, "SearchTitle"), normalizedSearch))
                .ThenByDescending(recipe => recipe.PublishedAt).ThenBy(recipe => recipe.Id)
            : OrderPublicRecipes(query, sort);
        var ranked = await ordered.Skip((page - 1) * pageSize).Take(pageSize)
            .Select(recipe => new
            {
                Recipe = recipe,
                Score = terms.Length > 0 ? (float?)EF.Property<NpgsqlTsVector>(recipe, "SearchVector").Rank(EF.Functions.ToTsQuery("simple", tsQuery)) : null,
            })
            .ToListAsync(cancellationToken).ConfigureAwait(false);
        var scores = ranked.ToDictionary(item => item.Recipe.Id, item => item.Score);
        var mapped = await this.MapRecipesAsync(ranked.Select(item => item.Recipe).ToList(), cancellationToken).ConfigureAwait(false);
        var result = new PageEnvelope<RecipeDto>(
            mapped.Select(recipe => recipe with { RelevanceScore = scores[recipe.Id] }).ToArray(),
            CreateMeta(page, pageSize, total),
            mapped.Count == 0 ? EmptySearchMessage : null);
        this.LogSlowDiscoveryQuery(stopwatch, "search");
        return result;
    }

    /// <summary>Gets my recipe.</summary>
    /// <param name="id">The identifier of the requested resource.</param>
    /// <param name="userId">The identifier of the user performing or owning the operation.</param>
    /// <param name="cancellationToken">Cancels the operation when requested by the caller.</param>
    /// <returns>A task representing the operation and its result.</returns>
    public async Task<RecipeDto> GetMyRecipeAsync(Guid id, string userId, CancellationToken cancellationToken)
    {
        var recipe = await dbContext.Recipes.AsNoTracking().SingleOrDefaultAsync(item => item.Id == id, cancellationToken)
            .ConfigureAwait(false)
            ?? throw NotFound("RECIPE_NOT_FOUND", "Recipe was not found.");
        if (recipe.AuthorId != userId)
        {
            throw Forbidden();
        }

        return await this.MapRecipeAsync(recipe, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Lists my recipes.</summary>
    /// <param name="userId">The identifier of the user performing or owning the operation.</param>
    /// <param name="status">The status supplied to this operation.</param>
    /// <param name="q">The q supplied to this operation.</param>
    /// <param name="page">The page supplied to this operation.</param>
    /// <param name="pageSize">The page size supplied to this operation.</param>
    /// <param name="cancellationToken">Cancels the operation when requested by the caller.</param>
    /// <returns>A task representing the operation and its result.</returns>
    public Task<PageEnvelope<RecipeDto>> ListMyRecipesAsync(
        string userId,
        RecipeStatus? status,
        string? q,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        var query = dbContext.Recipes.AsNoTracking().Where(recipe => recipe.AuthorId == userId);
        if (status.HasValue)
        {
            query = query.Where(recipe => recipe.Status == status.Value);
        }

        return this.ListRecipesAsync(this.FilterManagedRecipes(query, q, includeAuthor: false), page, pageSize, cancellationToken);
    }

    /// <summary>Gets admin recipe.</summary>
    /// <param name="id">The identifier of the requested resource.</param>
    /// <param name="cancellationToken">Cancels the operation when requested by the caller.</param>
    /// <returns>A task representing the operation and its result.</returns>
    public async Task<RecipeDto> GetAdminRecipeAsync(Guid id, CancellationToken cancellationToken)
    {
        var recipe = await dbContext.Recipes.AsNoTracking().SingleOrDefaultAsync(item => item.Id == id, cancellationToken)
            .ConfigureAwait(false)
            ?? throw NotFound("RECIPE_NOT_FOUND", "Recipe was not found.");
        return await this.MapRecipeAsync(recipe, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Lists admin recipes.</summary>
    /// <param name="status">The status supplied to this operation.</param>
    /// <param name="authorId">The identifier of the user performing or owning the operation.</param>
    /// <param name="q">The q supplied to this operation.</param>
    /// <param name="page">The page supplied to this operation.</param>
    /// <param name="pageSize">The page size supplied to this operation.</param>
    /// <param name="cancellationToken">Cancels the operation when requested by the caller.</param>
    /// <returns>A task representing the operation and its result.</returns>
    public Task<PageEnvelope<RecipeDto>> ListAdminRecipesAsync(
        RecipeStatus? status,
        string? authorId,
        string? q,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        var query = dbContext.Recipes.AsNoTracking();
        if (status.HasValue)
        {
            query = query.Where(recipe => recipe.Status == status.Value);
        }

        if (authorId is not null)
        {
            query = query.Where(recipe => recipe.AuthorId == authorId);
        }

        return this.ListRecipesAsync(this.FilterManagedRecipes(query, q, includeAuthor: true), page, pageSize, cancellationToken);
    }

    private static IOrderedQueryable<Recipe> OrderPublicRecipes(IQueryable<Recipe> query, string? sort)
    {
        return sort switch
        {
            "quickest" => query.OrderBy(recipe => recipe.PrepTime + recipe.CookTime).ThenByDescending(recipe => recipe.PublishedAt).ThenBy(recipe => recipe.Id),
            "createdAt" or "oldest" => query.OrderBy(recipe => recipe.CreatedAt).ThenBy(recipe => recipe.Id),
            "title" or "az" => query.OrderBy(recipe => recipe.Title).ThenBy(recipe => recipe.Id),
            "-title" or "za" => query.OrderByDescending(recipe => recipe.Title).ThenBy(recipe => recipe.Id),
            "cookTime" => query.OrderBy(recipe => recipe.CookTime).ThenByDescending(recipe => recipe.PublishedAt).ThenBy(recipe => recipe.Id),
            "-cookTime" => query.OrderByDescending(recipe => recipe.CookTime).ThenByDescending(recipe => recipe.PublishedAt).ThenBy(recipe => recipe.Id),
            "prepTime" => query.OrderBy(recipe => recipe.PrepTime).ThenByDescending(recipe => recipe.PublishedAt).ThenBy(recipe => recipe.Id),
            "-prepTime" => query.OrderByDescending(recipe => recipe.PrepTime).ThenByDescending(recipe => recipe.PublishedAt).ThenBy(recipe => recipe.Id),
            "servings" => query.OrderBy(recipe => recipe.Servings).ThenByDescending(recipe => recipe.PublishedAt).ThenBy(recipe => recipe.Id),
            "-servings" => query.OrderByDescending(recipe => recipe.Servings).ThenByDescending(recipe => recipe.PublishedAt).ThenBy(recipe => recipe.Id),
            _ => query.OrderByDescending(recipe => recipe.CreatedAt).ThenByDescending(recipe => recipe.Id),
        };
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

    private static RecipeNutrition? MergeNutrition(RecipeNutrition? current, NutritionUpdateRequest? update) => update is null
        ? null
        : RecipeNutrition.Create(
            update.Calories.Or(current?.Calories),
            update.Protein.Or(current?.Protein),
            update.Carbohydrates.Or(current?.Carbohydrates),
            update.Fat.Or(current?.Fat),
            update.Fiber.Or(current?.Fiber),
            update.Sodium.Or(current?.Sodium));

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
                ContentProblemKind.Validation);
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

    private void LogSlowDiscoveryQuery(Stopwatch stopwatch, string queryName)
    {
        stopwatch.Stop();
        if (stopwatch.ElapsedMilliseconds > 100)
        {
            SlowDiscoveryQuery(logger, queryName, stopwatch.Elapsed.TotalMilliseconds, null);
        }
    }

    private IQueryable<Recipe> FilterManagedRecipes(IQueryable<Recipe> query, string? q, bool includeAuthor)
    {
        var term = q?.Trim();
        if (string.IsNullOrEmpty(term))
        {
            return query;
        }

        if (term.Length > 100)
        {
            throw new ContentProblemException("VALIDATION_ERROR", "q must not exceed 100 characters.", ContentProblemKind.Validation);
        }

        var escaped = term.Replace("\\", "\\\\", StringComparison.Ordinal)
            .Replace("%", "\\%", StringComparison.Ordinal).Replace("_", "\\_", StringComparison.Ordinal);
        var pattern = $"%{escaped}%";
        var categoryIds = dbContext.Categories.Where(category => EF.Functions.ILike(category.Name, pattern, "\\")).Select(category => category.Id);
        var authorIds = dbContext.Users.Where(user => EF.Functions.ILike(user.DisplayName, pattern, "\\")).Select(user => user.Id);
        return query.Where(recipe => EF.Functions.ILike(recipe.Title, pattern, "\\")
            || categoryIds.Contains(recipe.CategoryId)
            || (includeAuthor && authorIds.Contains(recipe.AuthorId)));
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
            await this.MapRecipesAsync(recipes, cancellationToken).ConfigureAwait(false),
            CreateMeta(page, pageSize, total));
    }

    private async Task<Recipe> FindRecipeForMutationAsync(
        Guid id,
        string userId,
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

    private async Task SaveRecipeMutationAsync(CancellationToken cancellationToken)
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
                "RECIPE_CONCURRENCY_CONFLICT",
                "The recipe was changed by another request.",
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
            if (!await unitOfWork.Recipes.SlugExistsAsync(candidate, recipeId, cancellationToken).ConfigureAwait(false) &&
                !await dbContext.RecipeSlugs.AnyAsync(item => item.Slug == candidate && item.RecipeId != recipeId, cancellationToken).ConfigureAwait(false))
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
            throw new FluentValidation.ValidationException([new FluentValidation.Results.ValidationFailure("CategoryId", "Category is invalid.")]);
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
            item.OriginalUrl,
            item.MediumUrl,
            item.ThumbnailUrl,
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
                images.FirstOrDefault(image => image.IsPrimary) is { } primary ? primary.ThumbnailUrl ?? primary.MediumUrl ?? primary.OriginalUrl : null,
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
                item.OriginalUrl,
                item.MediumUrl,
                item.ThumbnailUrl,
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
            images.FirstOrDefault(image => image.IsPrimary) is { } primary ? primary.ThumbnailUrl ?? primary.MediumUrl ?? primary.OriginalUrl : null,
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
            where recipe.Status == RecipeStatus.Published
            orderby image.IsPrimary descending, recipe.PublishedAt descending, image.OrderIndex
            select new
            {
                recipe.CategoryId,
                RecipeSlug = recipe.Slug,
                ImageId = image.Id,
                HasMedium = image.MediumObjectKey != null,
            })
        .ToListAsync(cancellationToken).ConfigureAwait(false);

        var imageByCategory = new Dictionary<Guid, string>();
        foreach (var category in categories)
        {
            var imagesForCategory = categoryImages.Where(item => item.CategoryId == category.Id).ToList();
            if (imagesForCategory.Count == 0)
            {
                continue;
            }

            if (PreferredCategoryRecipeSlugs.TryGetValue(category.Slug, out var preferredSlug))
            {
                var preferred = imagesForCategory.FirstOrDefault(item => string.Equals(item.RecipeSlug, preferredSlug, StringComparison.OrdinalIgnoreCase));
                if (preferred != null)
                {
                    imageByCategory[category.Id] = MediaUrl(preferred.ImageId, preferred.HasMedium ? "medium" : "original");
                    continue;
                }
            }

            var first = imagesForCategory[0];
            imageByCategory[category.Id] = MediaUrl(first.ImageId, first.HasMedium ? "medium" : "original");
        }

        return categories.Select(category => ToCategoryDto(
            category,
            counts.GetValueOrDefault(category.Id),
            category.ImageUrl ?? imageByCategory.GetValueOrDefault(category.Id))).ToArray();
    }
}
