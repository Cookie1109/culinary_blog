// Culinary Blog. Authorship is recorded in the repository history.

namespace CulinaryBlog.Domain.Recipes;

/// <summary>The recipe status.</summary>
public enum RecipeStatus
{
    /// <summary>The draft value.</summary>
    Draft = 0,

    /// <summary>The published value.</summary>
    Published = 1,

    /// <summary>The archived value.</summary>
    Archived = 2,
}
