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

    [Fact]
    public void NutritionNormalizesEmptyInputAndPreservesAllValues()
    {
        Assert.Null(RecipeNutrition.Create(null, null, null, null, null, null));

        var nutrition = RecipeNutrition.Create(100, 10, 20, 5, 2, 50);

        Assert.NotNull(nutrition);
        Assert.Equal(100, nutrition.Calories);
        Assert.Equal(10, nutrition.Protein);
        Assert.Equal(20, nutrition.Carbohydrates);
        Assert.Equal(5, nutrition.Fat);
        Assert.Equal(2, nutrition.Fiber);
        Assert.Equal(50, nutrition.Sodium);
    }

    [Fact]
    public void NutritionRejectsEveryNegativeField()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => RecipeNutrition.Create(null, -1m, null, null, null, null));
        Assert.Throws<ArgumentOutOfRangeException>(() => RecipeNutrition.Create(null, null, -1m, null, null, null));
        Assert.Throws<ArgumentOutOfRangeException>(() => RecipeNutrition.Create(null, null, null, -1m, null, null));
        Assert.Throws<ArgumentOutOfRangeException>(() => RecipeNutrition.Create(null, null, null, null, -1m, null));
        Assert.Throws<ArgumentOutOfRangeException>(() => RecipeNutrition.Create(null, null, null, null, null, -1m));
    }

    [Fact]
    public void RecipeRejectsInvalidDataAndTransitions()
    {
        var recipe = CreateRecipe();

        Assert.Throws<DomainException>(() => recipe.Unarchive());
        Assert.Throws<DomainException>(() => recipe.Update(
            "bad",
            "x",
            string.Empty,
            Guid.Empty,
            0,
            -1,
            0,
            (RecipeDifficulty)999,
            null,
            null));
        Assert.Throws<DomainException>(() => recipe.Update(
            "valid-slug",
            "Valid title",
            "Description",
            Guid.NewGuid(),
            10,
            0,
            2,
            RecipeDifficulty.Easy,
            new string('x', 10_001),
            null));
    }

    [Fact]
    public void CategoryRejectsInvalidDataAndNormalizesOptionalFields()
    {
        Assert.Throws<DomainException>(() => Category.Create(Guid.NewGuid(), "x", "x", null, null, 0));
        Assert.Throws<DomainException>(() => Category.Create(Guid.NewGuid(), "Valid", "valid", null, null, -1));
        Assert.Throws<DomainException>(() =>
            Category.Create(Guid.NewGuid(), "Valid", "valid", new string('x', 2001), null, 0));

        var category = Category.Create(Guid.NewGuid(), "  Món chay  ", "mon-chay", "  Tươi ngon  ", "  https://example.com/image.png  ", 2);

        Assert.Equal("Món chay", category.Name);
        Assert.Equal("MÓN CHAY", category.NormalizedName);
        Assert.Equal("Tươi ngon", category.Description);
        Assert.Equal("https://example.com/image.png", category.ImageUrl);
        Assert.Equal(2, category.OrderIndex);
    }

    [Fact]
    public void SlugHandlesEmptyAsciiOutputRepeatedSeparatorsAndMaximumLength()
    {
        Assert.Equal("item", Slug.From("你好", 20));
        Assert.Equal("a-b", Slug.From("A---B", 20));
        Assert.Equal("very-long", Slug.From("Very long recipe title", 9));
        Assert.Throws<ArgumentException>(() => Slug.From(" ", 20));
    }

    [Fact]
    public void IngredientSupportsNullableQuantityAndRejectsInvalidQuantity()
    {
        var ingredient = RecipeIngredient.Create(
            Guid.NewGuid(), Guid.NewGuid(), "  Muối  ", null, null, "  Vừa ăn  ", 0);

        Assert.Equal("Muối", ingredient.Name);
        Assert.Null(ingredient.Quantity);
        Assert.Equal("Vừa ăn", ingredient.Notes);
        Assert.Throws<DomainException>(() => RecipeIngredient.Create(
            Guid.NewGuid(), Guid.NewGuid(), "Đường", 0, "g", null, 0));
    }

    [Fact]
    public void StepAndImageValidateCompositionMetadata()
    {
        var step = RecipeStep.Create(
            Guid.NewGuid(), Guid.NewGuid(), 1, "  Sơ chế  ", "  Rửa sạch nguyên liệu.  ", 5, null);
        var image = RecipeImage.Create(
            Guid.NewGuid(), Guid.NewGuid(), "recipes/key/original.jpg", "image/jpeg", "  Thành phẩm  ", true, 0);

        Assert.Equal("Sơ chế", step.Title);
        Assert.Equal(1, step.StepNumber);
        Assert.Equal("Thành phẩm", image.AltText);
        Assert.Equal(ImageProcessingStatus.Pending, image.ProcessingStatus);
        image.MarkReady("recipes/key/medium.webp", "recipes/key/thumbnail.webp");
        Assert.Equal(ImageProcessingStatus.Ready, image.ProcessingStatus);
        Assert.Throws<DomainException>(() => RecipeStep.Create(
            Guid.NewGuid(), Guid.NewGuid(), 0, "Bước", "Mô tả", null, null));
    }

    [Fact]
    public void ArchivedRecipeRejectsCompositionMutation()
    {
        var recipe = CreateRecipe();
        recipe.Archive();

        var exception = Assert.Throws<DomainException>(recipe.MarkCompositionChanged);

        Assert.Equal("RECIPE_INVALID_TRANSITION", exception.Code);
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
