using System.Globalization;
using System.Text;
using CulinaryBlog.Domain.Recipes;

namespace CulinaryBlog.Application.Abstractions.Caching;

public static class PublicCacheKeys
{
    public const string RecipesTag = "public:recipes";
    public const string CategoriesTag = "public:categories";

    public static readonly IReadOnlyCollection<string> RecipeTags = [RecipesTag, CategoriesTag];
    public static readonly IReadOnlyCollection<string> CategoryTags = [CategoriesTag, RecipesTag];

    public static string Categories() => "cb:v1:categories:locale=default";

    public static string Category(string slug, int page, int pageSize) =>
        $"cb:v1:category:slug={Normalize(slug)}&page={page.ToString(CultureInfo.InvariantCulture)}&pageSize={pageSize.ToString(CultureInfo.InvariantCulture)}&locale=default";

    public static string Recipe(string slug) =>
        $"cb:v1:recipe:slug={Normalize(slug)}&locale=default";

    public static string RecipeList(
        Guid? categoryId,
        RecipeDifficulty? difficulty,
        int? maxCookTime,
        int? minServings,
        int? minPrepTime,
        int? maxPrepTime,
        string? sort,
        int page,
        int pageSize) =>
        "cb:v1:recipe-list:" + JoinParameters(
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

    public static string Search(
        string? search,
        string? category,
        RecipeDifficulty? difficulty,
        int? maxTime,
        string? sort,
        int page,
        int pageSize) =>
        "cb:v1:search:" + JoinParameters(
            ("q", search),
            ("category", category),
            ("difficulty", difficulty?.ToString()),
            ("maxTime", Format(maxTime)),
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
