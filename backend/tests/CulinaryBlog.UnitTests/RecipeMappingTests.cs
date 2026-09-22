using CulinaryBlog.Application.Content;
using CulinaryBlog.Domain.Recipes;

namespace CulinaryBlog.UnitTests;

public sealed class RecipeMappingTests
{
    [Fact]
    public void NutritionMappingPreservesValuesAndNulls()
    {
        Assert.Null(RecipeMappings.ToNutritionDto(null));

        var nutrition = RecipeNutrition.Create(250, 10, null, 8, null, 120);
        var dto = RecipeMappings.ToNutritionDto(nutrition);

        Assert.NotNull(dto);
        Assert.Equal(250, dto.Calories);
        Assert.Equal(10, dto.Protein);
        Assert.Null(dto.Carbohydrates);
        Assert.Equal(8, dto.Fat);
        Assert.Null(dto.Fiber);
        Assert.Equal(120, dto.Sodium);
    }
}
