// Culinary Blog. Authorship is recorded in the repository history.

using System.Globalization;
using System.Text;
using CulinaryBlog.Domain.Recipes;

namespace CulinaryBlog.Application.Abstractions.Caching;

/// <summary>Represents the public cache keys.</summary>
public static class PublicCacheKeys
{
    /// <summary>The recipes tag.</summary>
    public const string RecipesTag = "public:recipes";

    /// <summary>The categories tag.</summary>
    public const string CategoriesTag = "public:categories";

    /// <summary>Performs the categories operation.</summary>
    /// <returns>The categories result.</returns>
    public const string Categories = "cb:v1:categories:locale=default";

    /// <summary>The recipe tags.</summary>
    public static readonly IReadOnlyCollection<string> RecipeTags = [RecipesTag, CategoriesTag];

    /// <summary>The category tags.</summary>
    public static readonly IReadOnlyCollection<string> CategoryTags = [CategoriesTag, RecipesTag];

    /// <summary>Performs the category operation.</summary>
    /// <param name="slug">The slug supplied to this operation.</param>
    /// <param name="page">The page supplied to this operation.</param>
    /// <param name="pageSize">The page size supplied to this operation.</param>
    /// <param name="sort">The sort supplied to this operation.</param>
    /// <returns>The category result.</returns>
    public static string Category(string slug, int page, int pageSize, string? sort = null) =>
        $"cb:v1:category:slug={Normalize(slug)}&page={page.ToString(CultureInfo.InvariantCulture)}&pageSize={pageSize.ToString(CultureInfo.InvariantCulture)}&locale=default" +
        (sort is null ? string.Empty : $"&sort={Normalize(sort)}");

    /// <summary>Performs the recipe operation.</summary>
    /// <param name="slug">The slug supplied to this operation.</param>
    /// <returns>The recipe result.</returns>
    public static string Recipe(string slug) =>
        $"cb:v1:recipe:slug={Normalize(slug)}&locale=default";

    /// <summary>Performs the recipe list operation.</summary>
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
    /// <returns>The recipe list result.</returns>
    public static string RecipeList(
        Guid? categoryId,
        RecipeDifficulty? difficulty,
        int? maxCookTime,
        int? minServings,
        int? minPrepTime,
        int? maxPrepTime,
        string? sort,
        int page,
        int pageSize,
        string? category = null,
        int? maxTime = null) =>
        "cb:v1:recipe-list:" + JoinParameters(
            ("category", category),
            ("maxTime", Format(maxTime)),
            ("categoryId", categoryId?.ToString("N", CultureInfo.InvariantCulture)),
            ("difficulty", difficulty?.ToString()),
            ("maxCookTime", Format(maxCookTime)),
            ("minServings", Format(minServings)),
            ("minPrepTime", Format(minPrepTime)),
            ("maxPrepTime", Format(maxPrepTime)),
            ("sort", sort),
            ("page", Format(page)),
            ("pageSize", Format(pageSize)),
            ("locale", "default"));

    /// <summary>Searches the public cache keys.</summary>
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
    /// <returns>The search result.</returns>
    public static string Search(
        string? search,
        string? category,
        RecipeDifficulty? difficulty,
        int? maxTime,
        string? sort,
        int page,
        int pageSize,
        Guid? categoryId = null,
        int? maxCookTime = null,
        int? minServings = null,
        int? minPrepTime = null,
        int? maxPrepTime = null) =>
        "cb:v3:search:" + JoinParameters(
            ("q", search),
            ("category", category),
            ("difficulty", difficulty?.ToString()),
            ("maxTime", Format(maxTime)),
            ("categoryId", categoryId?.ToString("N", CultureInfo.InvariantCulture)),
            ("maxCookTime", Format(maxCookTime)),
            ("minServings", Format(minServings)),
            ("minPrepTime", Format(minPrepTime)),
            ("maxPrepTime", Format(maxPrepTime)),
            ("sort", sort),
            ("page", Format(page)),
            ("pageSize", Format(pageSize)),
            ("locale", "default"));

    private static string JoinParameters(params (string Name, string? Value)[] parameters) =>
        string.Join('&', parameters.Select(parameter => $"{parameter.Name}={Normalize(parameter.Value)}"));

    private static string Format(int? value) => value?.ToString(CultureInfo.InvariantCulture) ?? string.Empty;

    private static string Normalize(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        var normalized = value.Normalize(NormalizationForm.FormKC).Trim().ToLowerInvariant();
        var collapsed = string.Join(' ', normalized.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));
        return Uri.EscapeDataString(collapsed);
    }
}
