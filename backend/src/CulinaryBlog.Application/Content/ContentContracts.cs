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
    IReadOnlyCollection<object> Ingredients,
    IReadOnlyCollection<object> Steps,
    IReadOnlyCollection<object> Images);

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

    Task DeleteRecipeAsync(
        Guid id,
        Guid userId,
        bool isAdmin,
        long expectedVersion,
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
}
