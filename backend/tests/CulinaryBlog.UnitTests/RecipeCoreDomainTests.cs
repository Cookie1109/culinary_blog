using CulinaryBlog.Domain.Categories;
using CulinaryBlog.Domain.Common;
using CulinaryBlog.Domain.Recipes;

namespace CulinaryBlog.UnitTests;

public sealed class RecipeCoreDomainTests
{
    [Theory]
    [InlineData("Bánh mì Việt Nam", "banh-mi-viet-nam")]
    [InlineData("  Wild   Mushroom Risotto! ", "wild-mushroom-risotto")]
    public void SlugIsAsciiAndDeterministic(string value, string expected)
    {
        Assert.Equal(expected, Slug.From(value, 220));
    }

    [Fact]
    public void CategoryRenameKeepsStableSlugAndSoftDeleteIsIdempotent()
    {
        var category = Category.Create(Guid.NewGuid(), "Món chính", "mon-chinh", null, null, 0);

        category.Update("Bữa tối", "Món ăn buổi tối", null, 1);
        category.Delete();
        category.Delete();

        Assert.Equal("mon-chinh", category.Slug);
        Assert.Equal("Bữa tối", category.Name);
        Assert.True(category.IsDeleted);
    }

    [Fact]
    public void RecipeStartsAsDraftAndArchiveTransitionsAreGuarded()
    {
        var recipe = CreateRecipe();

        Assert.Equal(RecipeStatus.Draft, recipe.Status);
        recipe.Archive();
        Assert.Equal(RecipeStatus.Archived, recipe.Status);
        Assert.Throws<DomainException>(() => recipe.Archive());
        recipe.Unarchive();
        Assert.Equal(RecipeStatus.Draft, recipe.Status);
    }

    [Fact]
    public void ArchivedRecipeCannotBeEdited()
    {
        var recipe = CreateRecipe();
        recipe.Archive();

        var exception = Assert.Throws<DomainException>(() => recipe.Update(
            "updated",
            "Updated recipe",
            "Description",
            Guid.NewGuid(),
            10,
            0,
            2,
            RecipeDifficulty.Easy,
            null,
            null));

        Assert.Equal("RECIPE_INVALID_TRANSITION", exception.Code);
    }

    [Fact]
    public void NutritionRejectsNegativeValues()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => RecipeNutrition.Create(-1, null, null, null, null, null));
    }

    private static Recipe CreateRecipe() => Recipe.Create(
        Guid.NewGuid(),
        Guid.NewGuid(),
        "draft-recipe",
        "Draft recipe",
        "Description",
        Guid.NewGuid(),
        10,
        0,
        2,
        RecipeDifficulty.Medium,
        null,
        null);
}
