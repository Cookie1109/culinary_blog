// Culinary Blog. Authorship is recorded in the repository history.

namespace CulinaryBlog.Domain.Recipes;

/// <summary>Represents the recipe nutrition.</summary>
public sealed class RecipeNutrition
{
    private RecipeNutrition()
    {
    }

    /// <summary>Gets the calories.</summary>
    public decimal? Calories { get; private set; }

    /// <summary>Gets the protein.</summary>
    public decimal? Protein { get; private set; }

    /// <summary>Gets the carbohydrates.</summary>
    public decimal? Carbohydrates { get; private set; }

    /// <summary>Gets the fat.</summary>
    public decimal? Fat { get; private set; }

    /// <summary>Gets the fiber.</summary>
    public decimal? Fiber { get; private set; }

    /// <summary>Gets the sodium.</summary>
    public decimal? Sodium { get; private set; }

    /// <summary>Creates the recipe nutrition.</summary>
    /// <param name="calories">The calories supplied to this operation.</param>
    /// <param name="protein">The protein supplied to this operation.</param>
    /// <param name="carbohydrates">The carbohydrates supplied to this operation.</param>
    /// <param name="fat">The fat supplied to this operation.</param>
    /// <param name="fiber">The fiber supplied to this operation.</param>
    /// <param name="sodium">The sodium supplied to this operation.</param>
    /// <returns>The create result.</returns>
    public static RecipeNutrition? Create(
        decimal? calories,
        decimal? protein,
        decimal? carbohydrates,
        decimal? fat,
        decimal? fiber,
        decimal? sodium)
    {
        if (calories is null && protein is null && carbohydrates is null && fat is null && fiber is null && sodium is null)
        {
            return null;
        }

        EnsureNonNegative(calories, nameof(calories));
        EnsureNonNegative(protein, nameof(protein));
        EnsureNonNegative(carbohydrates, nameof(carbohydrates));
        EnsureNonNegative(fat, nameof(fat));
        EnsureNonNegative(fiber, nameof(fiber));
        EnsureNonNegative(sodium, nameof(sodium));

        return new RecipeNutrition
        {
            Calories = calories,
            Protein = protein,
            Carbohydrates = carbohydrates,
            Fat = fat,
            Fiber = fiber,
            Sodium = sodium,
        };
    }

    private static void EnsureNonNegative(decimal? value, string name)
    {
        if (value < 0)
        {
            throw new ArgumentOutOfRangeException(name, "Nutrition values cannot be negative.");
        }
    }
}
