// Culinary Blog. Authorship is recorded in the repository history.

using CulinaryBlog.Domain.Recipes;
using Mapster;

namespace CulinaryBlog.Application.Content;

/// <summary>Represents the recipe mappings.</summary>
public static class RecipeMappings
{
    private static readonly TypeAdapterConfig NutritionConfig = CreateNutritionConfig();

    /// <summary>Performs the to nutrition dto operation.</summary>
    /// <param name="nutrition">The nutrition supplied to this operation.</param>
    /// <returns>The to nutrition dto result.</returns>
    public static NutritionDto? ToNutritionDto(RecipeNutrition? nutrition) =>
        nutrition is null ||
        (nutrition.Calories is null && nutrition.Protein is null && nutrition.Carbohydrates is null &&
         nutrition.Fat is null && nutrition.Fiber is null && nutrition.Sodium is null)
            ? null
            : nutrition.Adapt<RecipeNutrition, NutritionDto>(NutritionConfig);

    private static TypeAdapterConfig CreateNutritionConfig()
    {
        var config = new TypeAdapterConfig();
        config.NewConfig<RecipeNutrition, NutritionDto>().MapToConstructor(true);
        return config;
    }
}
