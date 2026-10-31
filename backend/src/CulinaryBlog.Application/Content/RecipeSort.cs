// Culinary Blog. Authorship is recorded in the repository history.

namespace CulinaryBlog.Application.Content;

/// <summary>Represents the recipe sort.</summary>
public static class RecipeSort
{
    /// <summary>Performs the resolve operation.</summary>
    /// <param name="sort">The sort supplied to this operation.</param>
    /// <param name="sortBy">The sort by supplied to this operation.</param>
    /// <param name="sortOrder">The sort order supplied to this operation.</param>
    /// <param name="search">The search supplied to this operation.</param>
    /// <returns>The resolve result.</returns>
    public static string Resolve(string? sort, string? sortBy, string? sortOrder, bool search = false)
    {
        var explicitSort = sort switch
        {
            null or "" => null,
            "newest" => "-createdAt",
            "oldest" => "createdAt",
            "az" => "title",
            "za" => "-title",
            _ => sort,
        };
        if (sortBy is null && sortOrder is null)
        {
            return explicitSort ?? (search ? "relevance" : "-createdAt");
        }

        var field = sortBy ?? "createdAt";
        if (field is not ("createdAt" or "title" or "cookTime" or "prepTime" or "servings" or "relevance"))
        {
            throw new ContentProblemException("VALIDATION_ERROR", "sortBy is invalid.", ContentProblemKind.Validation);
        }

        var direction = sortOrder?.ToLowerInvariant() ?? (sortBy is null || field == "relevance" ? "desc" : "asc");
        if (direction is not ("asc" or "desc") || (field == "relevance" && (!search || direction != "desc")))
        {
            throw new ContentProblemException("VALIDATION_ERROR", "sortOrder is invalid for sortBy.", ContentProblemKind.Validation);
        }

        var prefix = direction == "desc" ? "-" : string.Empty;
        var alias = field == "relevance" ? field : prefix + field;
        if (explicitSort is not null && explicitSort != alias)
        {
            throw new ContentProblemException("VALIDATION_ERROR", "sort conflicts with sortBy/sortOrder.", ContentProblemKind.BadRequest);
        }

        return alias;
    }
}
