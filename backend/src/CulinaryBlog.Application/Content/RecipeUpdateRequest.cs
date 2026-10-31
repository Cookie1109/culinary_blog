// Culinary Blog. Authorship is recorded in the repository history.

using CulinaryBlog.Domain.Recipes;

namespace CulinaryBlog.Application.Content;

/// <summary>Updates only supplied recipe fields, using the aggregate If-Match version.</summary>
/// <remarks>Omission retains the current value. Nutrition merges supplied fields; null clears nullable content. Instructions null becomes empty text.</remarks>
public sealed record RecipeUpdateRequest
{
    /// <summary>Gets or initializes the title.</summary>
    public InputField<string?> Title { get; init; }

    /// <summary>Gets or initializes the description.</summary>
    public InputField<string?> Description { get; init; }

    /// <summary>Gets or initializes the category id.</summary>
    public InputField<Guid?> CategoryId { get; init; }

    /// <summary>Gets or initializes the prep time.</summary>
    public InputField<int?> PrepTime { get; init; }

    /// <summary>Gets or initializes the cook time.</summary>
    public InputField<int?> CookTime { get; init; }

    /// <summary>Gets or initializes the servings.</summary>
    public InputField<int?> Servings { get; init; }

    /// <summary>Gets or initializes the difficulty.</summary>
    public InputField<RecipeDifficulty?> Difficulty { get; init; }

    /// <summary>Gets or initializes the instructions.</summary>
    public InputField<string?> Instructions { get; init; }

    /// <summary>Gets or initializes the nutrition.</summary>
    public InputField<NutritionUpdateRequest?> Nutrition { get; init; }
}
