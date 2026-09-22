using CulinaryBlog.Domain.Recipes;

namespace CulinaryBlog.Application.Content;

public sealed record CategoryWriteRequest(
    string Name,
    string? Description,
    string? ImageUrl,
    int OrderIndex = 0);

public sealed record NutritionDto(
    decimal? Calories,
    decimal? Protein,
    decimal? Carbohydrates,
    decimal? Fat,
    decimal? Fiber,
    decimal? Sodium);

public sealed record IngredientWriteRequest(
    string Name,
    decimal? Quantity,
    string? Unit,
    string? Notes,
    int OrderIndex = 0);

public sealed record IngredientDto(
    Guid Id,
    string Name,
    decimal? Quantity,
    string? Unit,
    string? Notes,
    int OrderIndex);

public sealed record StepWriteRequest(
    string Title,
    string Description,
    int? TimerMinutes,
    string? ImageUrl,
    int? StepNumber = null);

public sealed record StepDto(
    Guid Id,
    int StepNumber,
    string Title,
    string Description,
    int? TimerMinutes,
    string? ImageUrl);

public sealed record ImageMetadataRequest(string? AltText, bool? IsPrimary, int? OrderIndex);

public sealed record RecipeImageDto(
    Guid Id,
    string OriginalUrl,
    string? MediumUrl,
    string? ThumbnailUrl,
    string? AltText,
    bool IsPrimary,
    int OrderIndex,
    string ProcessingStatus);

public sealed record ChildMutationDto<T>(T Resource, long RecipeVersion);

public sealed record MediaFileDto(Stream Content, string ContentType);

public sealed record RecipeWriteRequest(
    string Title,
    string Description,
    Guid CategoryId,
    int PrepTime,
    int CookTime,
    int Servings,
    RecipeDifficulty Difficulty,
    string? Instructions,
    NutritionDto? Nutrition);

public sealed record CategoryDto(
    Guid Id,
    string Name,
    string Slug,
    string? Description,
    string? ImageUrl,
    int OrderIndex,
    int RecipeCount);

public sealed record RecipeAuthorDto(
    string Id,
    string Email,
    string DisplayName,
    string? AvatarUrl,
    string? Bio,
    IReadOnlyCollection<string> Roles,
    bool EmailConfirmed,
    bool IsActive,
    DateTimeOffset CreatedAt);

public sealed record RecipeDto(
    Guid Id,
    string Title,
    string Slug,
    string Description,
    int PrepTime,
    int CookTime,
    int Servings,
    string Difficulty,
    string Status,
    string? PrimaryImageUrl,
    CategoryDto Category,
    RecipeAuthorDto Author,
    DateTimeOffset CreatedAt,
    DateTimeOffset? PublishedAt,
    long Version,
    string? Instructions,
    NutritionDto? Nutrition,
    IReadOnlyCollection<IngredientDto> Ingredients,
    IReadOnlyCollection<StepDto> Steps,
    IReadOnlyCollection<RecipeImageDto> Images);

public sealed record PageMeta(
    int Page,
    int PageSize,
    int Total,
    int TotalPages,
    bool HasNextPage,
    bool HasPreviousPage);

public sealed record PageEnvelope<T>(IReadOnlyCollection<T> Data, PageMeta Meta);

public sealed record CategoryDetailData(CategoryDto Category, IReadOnlyCollection<RecipeDto> Recipes);

public sealed record CategoryDetailEnvelope(CategoryDetailData Data, PageMeta Meta);

public interface IContentService
{
    Task<IReadOnlyCollection<CategoryDto>> ListCategoriesAsync(CancellationToken cancellationToken);

    Task<CategoryDetailEnvelope> GetCategoryAsync(string slug, int page, int pageSize, CancellationToken cancellationToken);

    Task<CategoryDto> CreateCategoryAsync(CategoryWriteRequest request, CancellationToken cancellationToken);

    Task<CategoryDto> UpdateCategoryAsync(Guid id, CategoryWriteRequest request, CancellationToken cancellationToken);

    Task DeleteCategoryAsync(Guid id, CancellationToken cancellationToken);

    Task<RecipeDto> CreateRecipeAsync(Guid userId, RecipeWriteRequest request, CancellationToken cancellationToken);

    Task<RecipeDto> UpdateRecipeAsync(
        Guid id,
        Guid userId,
        bool isAdmin,
        long expectedVersion,
        RecipeWriteRequest request,
        CancellationToken cancellationToken);

    Task<RecipeDto> GetPublishedRecipeAsync(string slug, CancellationToken cancellationToken);

    Task<PageEnvelope<RecipeDto>> ListPublishedRecipesAsync(int page, int pageSize, CancellationToken cancellationToken);

    Task<RecipeDto> GetMyRecipeAsync(Guid id, Guid userId, CancellationToken cancellationToken);

    Task<PageEnvelope<RecipeDto>> ListMyRecipesAsync(
        Guid userId,
        RecipeStatus? status,
        int page,
        int pageSize,
        CancellationToken cancellationToken);

    Task<RecipeDto> GetAdminRecipeAsync(Guid id, CancellationToken cancellationToken);

    Task<PageEnvelope<RecipeDto>> ListAdminRecipesAsync(
        RecipeStatus? status,
        Guid? authorId,
        int page,
        int pageSize,
        CancellationToken cancellationToken);

    Task<ChildMutationDto<IngredientDto>> CreateIngredientAsync(
        Guid recipeId, Guid userId, bool isAdmin, long expectedVersion, IngredientWriteRequest request, CancellationToken cancellationToken);

    Task<ChildMutationDto<IngredientDto>> UpdateIngredientAsync(
        Guid recipeId, Guid ingredientId, Guid userId, bool isAdmin, long expectedVersion, IngredientWriteRequest request, CancellationToken cancellationToken);

    Task<long> DeleteIngredientAsync(
        Guid recipeId, Guid ingredientId, Guid userId, bool isAdmin, long expectedVersion, CancellationToken cancellationToken);

    Task<ChildMutationDto<StepDto>> CreateStepAsync(
        Guid recipeId, Guid userId, bool isAdmin, long expectedVersion, StepWriteRequest request, CancellationToken cancellationToken);

    Task<ChildMutationDto<StepDto>> UpdateStepAsync(
        Guid recipeId, Guid stepId, Guid userId, bool isAdmin, long expectedVersion, StepWriteRequest request, CancellationToken cancellationToken);

    Task<long> DeleteStepAsync(
        Guid recipeId, Guid stepId, Guid userId, bool isAdmin, long expectedVersion, CancellationToken cancellationToken);

    Task<ChildMutationDto<RecipeImageDto>> UploadImageAsync(
        Guid recipeId, Guid userId, bool isAdmin, long expectedVersion, Stream content, long length, string contentType,
        string? altText, bool isPrimary, CancellationToken cancellationToken);

    Task<ChildMutationDto<RecipeImageDto>> UpdateImageAsync(
        Guid recipeId, Guid imageId, Guid userId, bool isAdmin, long expectedVersion, ImageMetadataRequest request, CancellationToken cancellationToken);

    Task<long> DeleteImageAsync(
        Guid recipeId, Guid imageId, Guid userId, bool isAdmin, long expectedVersion, CancellationToken cancellationToken);

    Task<MediaFileDto> OpenMediaAsync(
        Guid imageId, string variant, Guid? userId, bool isAdmin, CancellationToken cancellationToken);
}
