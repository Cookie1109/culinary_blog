using CulinaryBlog.Domain.Recipes;
using Mapster;

namespace CulinaryBlog.Application.Content;

public static class RecipeMappings
{
    private static readonly TypeAdapterConfig NutritionConfig = CreateNutritionConfig();

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
