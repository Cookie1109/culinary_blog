// Culinary Blog. Authorship is recorded in the repository history.

using CulinaryBlog.Domain.Common;

namespace CulinaryBlog.Domain.Recipes;

/// <summary>Represents the recipe ingredient.</summary>
public sealed class RecipeIngredient : BaseEntity
{
    private RecipeIngredient()
    {
    }

    /// <summary>Gets the recipe id.</summary>
    public Guid RecipeId { get; private set; }

    /// <summary>Gets the name.</summary>
    public string Name { get; private set; } = string.Empty;

    /// <summary>Gets the quantity.</summary>
    public decimal? Quantity { get; private set; }

    /// <summary>Gets the unit.</summary>
    public string? Unit { get; private set; }

    /// <summary>Gets the notes.</summary>
    public string? Notes { get; private set; }

    /// <summary>Gets the order index.</summary>
    public int OrderIndex { get; private set; }

    /// <summary>Creates the recipe ingredient.</summary>
    /// <param name="id">The identifier of the requested resource.</param>
    /// <param name="recipeId">The identifier of the recipe being accessed.</param>
    /// <param name="name">The name supplied to this operation.</param>
    /// <param name="quantity">The quantity supplied to this operation.</param>
    /// <param name="unit">The unit supplied to this operation.</param>
    /// <param name="notes">The notes supplied to this operation.</param>
    /// <param name="orderIndex">The order index supplied to this operation.</param>
    /// <returns>The create result.</returns>
    public static RecipeIngredient Create(
        Guid id,
        Guid recipeId,
        string name,
        decimal? quantity,
        string? unit,
        string? notes,
        int orderIndex)
    {
        var ingredient = new RecipeIngredient { Id = id, RecipeId = recipeId };
        ingredient.Update(name, quantity, unit, notes, orderIndex);
        return ingredient;
    }

    /// <summary>Updates the recipe ingredient.</summary>
    /// <param name="name">The name supplied to this operation.</param>
    /// <param name="quantity">The quantity supplied to this operation.</param>
    /// <param name="unit">The unit supplied to this operation.</param>
    /// <param name="notes">The notes supplied to this operation.</param>
    /// <param name="orderIndex">The order index supplied to this operation.</param>
    public void Update(string name, decimal? quantity, string? unit, string? notes, int orderIndex)
    {
        var trimmedName = name.Trim();
        if (trimmedName.Length is < 1 or > 100 || quantity <= 0 || orderIndex < 0)
        {
            throw new DomainException("RECIPE_DATA_INVALID", "Ingredient data is invalid.");
        }

        if (unit?.Trim().Length > 50 || notes?.Trim().Length > 500)
        {
            throw new DomainException("RECIPE_DATA_INVALID", "Ingredient unit or notes are too long.");
        }

        this.Name = trimmedName;
        this.Quantity = quantity;
        this.Unit = string.IsNullOrWhiteSpace(unit) ? null : unit.Trim();
        this.Notes = string.IsNullOrWhiteSpace(notes) ? null : notes.Trim();
        this.OrderIndex = orderIndex;
    }

    /// <summary>Performs the move to operation.</summary>
    /// <param name="orderIndex">The order index supplied to this operation.</param>
    public void MoveTo(int orderIndex) => this.OrderIndex = orderIndex;

    /// <summary>Deletes the recipe ingredient.</summary>
    public void Delete() => this.SoftDelete();
}
