// Culinary Blog. Authorship is recorded in the repository history.

using CulinaryBlog.Domain.Recipes;

namespace CulinaryBlog.Application.Content;

/// <summary>Carries ingredient update request data.</summary>
public sealed record IngredientUpdateRequest
{
    /// <summary>Gets or initializes the name.</summary>
    public InputField<string?> Name { get; init; }

    /// <summary>Gets or initializes the quantity.</summary>
    public InputField<decimal?> Quantity { get; init; }

    /// <summary>Gets or initializes the unit.</summary>
    public InputField<string?> Unit { get; init; }

    /// <summary>Gets or initializes the notes.</summary>
    public InputField<string?> Notes { get; init; }

    /// <summary>Gets or initializes the order index.</summary>
    public InputField<int?> OrderIndex { get; init; }
}
