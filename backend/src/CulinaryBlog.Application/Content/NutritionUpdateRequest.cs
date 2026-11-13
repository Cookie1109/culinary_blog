// Culinary Blog. Authorship is recorded in the repository history.

using CulinaryBlog.Domain.Recipes;

namespace CulinaryBlog.Application.Content;

/// <summary>Carries nutrition update request data.</summary>
public sealed record NutritionUpdateRequest
{
    /// <summary>Gets or initializes the calories.</summary>
    public InputField<decimal?> Calories { get; init; }

    /// <summary>Gets or initializes the protein.</summary>
    public InputField<decimal?> Protein { get; init; }

    /// <summary>Gets or initializes the carbohydrates.</summary>
    public InputField<decimal?> Carbohydrates { get; init; }

    /// <summary>Gets or initializes the fat.</summary>
    public InputField<decimal?> Fat { get; init; }

    /// <summary>Gets or initializes the fiber.</summary>
    public InputField<decimal?> Fiber { get; init; }

    /// <summary>Gets or initializes the sodium.</summary>
    public InputField<decimal?> Sodium { get; init; }
}
