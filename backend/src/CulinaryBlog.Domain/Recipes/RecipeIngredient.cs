using CulinaryBlog.Domain.Common;

namespace CulinaryBlog.Domain.Recipes;

public sealed class RecipeIngredient : BaseEntity
{
    private RecipeIngredient()
    {
    }

    public Guid RecipeId { get; private set; }

    public string Name { get; private set; } = string.Empty;

    public decimal? Quantity { get; private set; }

    public string? Unit { get; private set; }

    public string? Notes { get; private set; }

    public int OrderIndex { get; private set; }

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

    public void Update(string name, decimal? quantity, string? unit, string? notes, int orderIndex)
    {
        var trimmedName = name.Trim();
        if (trimmedName.Length is < 1 or > 200 || quantity <= 0 || orderIndex < 0)
        {
            throw new DomainException("RECIPE_DATA_INVALID", "Ingredient data is invalid.");
        }

        if (unit?.Trim().Length > 50 || notes?.Trim().Length > 500)
        {
            throw new DomainException("RECIPE_DATA_INVALID", "Ingredient unit or notes are too long.");
        }

        Name = trimmedName;
        Quantity = quantity;
        Unit = string.IsNullOrWhiteSpace(unit) ? null : unit.Trim();
        Notes = string.IsNullOrWhiteSpace(notes) ? null : notes.Trim();
        OrderIndex = orderIndex;
    }

    public void MoveTo(int orderIndex) => OrderIndex = orderIndex;

    public void Delete() => SoftDelete();
}
