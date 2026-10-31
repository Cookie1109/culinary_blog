// Culinary Blog. Authorship is recorded in the repository history.

namespace CulinaryBlog.Domain.Recipes;

/// <summary>Represents the recipe slug.</summary>
public sealed class RecipeSlug
{
    private RecipeSlug()
    {
    }

    /// <summary>Gets the slug.</summary>
    public string Slug { get; private set; } = string.Empty;

    /// <summary>Gets the recipe id.</summary>
    public Guid RecipeId { get; private set; }

    /// <summary>Performs the reserve operation.</summary>
    /// <param name="slug">The slug supplied to this operation.</param>
    /// <param name="recipeId">The identifier of the recipe being accessed.</param>
    /// <returns>The reserve result.</returns>
    public static RecipeSlug Reserve(string slug, Guid recipeId) => new() { Slug = slug, RecipeId = recipeId };
}
