namespace CulinaryBlog.Domain.Recipes;

public sealed class RecipeNutrition
{
    private RecipeNutrition()
    {
    }

    public decimal? Calories { get; private set; }

    public decimal? Protein { get; private set; }

    public decimal? Carbohydrates { get; private set; }

    public decimal? Fat { get; private set; }

    public decimal? Fiber { get; private set; }

    public decimal? Sodium { get; private set; }

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
