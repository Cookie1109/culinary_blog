// Culinary Blog. Authorship is recorded in the repository history.

using CulinaryBlog.Domain.Categories;
using CulinaryBlog.Domain.Common;
using CulinaryBlog.Domain.Recipes;

namespace CulinaryBlog.UnitTests;

/// <summary>Contains regression checks for recipe core domain tests.</summary>
public sealed class RecipeCoreDomainTests
{
    /// <summary>Verifies that slug is ascii and deterministic.</summary>
    /// <param name="value">The value supplied to this operation.</param>
    /// <param name="expected">The expected supplied to this operation.</param>
    [Theory]
    [InlineData("Bánh mì Việt Nam", "banh-mi-viet-nam")]
    [InlineData("  Wild   Mushroom Risotto! ", "wild-mushroom-risotto")]
    public void SlugIsAsciiAndDeterministic(string value, string expected)
    {
        Assert.Equal(expected, Slug.From(value, 220));
    }

    /// <summary>Verifies that category rename keeps stable slug and soft delete is idempotent.</summary>
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

    /// <summary>Verifies that recipe starts as draft and archive transitions are guarded.</summary>
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

    /// <summary>Verifies that archived recipe cannot be edited.</summary>
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

    /// <summary>Verifies that publish requires complete aggregate and guards transitions.</summary>
    [Fact]
    public void PublishRequiresCompleteAggregateAndGuardsTransitions()
    {
        var recipe = CreateRecipe();
        var incomplete = Assert.Throws<DomainException>(() =>
            recipe.Publish(DateTimeOffset.UtcNow, true, 0, [1]));

        Assert.Equal("RECIPE_PUBLISH_INCOMPLETE", incomplete.Code);
        Assert.Null(recipe.PublishedAt);
        Assert.Throws<DomainException>(() => recipe.Unpublish());

        var publishedAt = new DateTimeOffset(2026, 10, 2, 4, 0, 0, TimeSpan.Zero);
        recipe.Publish(publishedAt, true, 1, [1]);

        Assert.Equal(RecipeStatus.Published, recipe.Status);
        Assert.Equal(publishedAt, recipe.PublishedAt);
        Assert.Throws<DomainException>(() => recipe.Publish(publishedAt, true, 1, [1]));
    }

    /// <summary>Verifies that first publish locks slug and republish keeps published at.</summary>
    [Fact]
    public void FirstPublishLocksSlugAndRepublishKeepsPublishedAt()
    {
        var recipe = CreateRecipe();
        var firstPublishedAt = new DateTimeOffset(2026, 10, 2, 4, 0, 0, TimeSpan.Zero);
        recipe.Publish(firstPublishedAt, true, 1, [1]);
        recipe.Unpublish();

        recipe.Update(
            "changed-slug",
            "Changed recipe title",
            "Changed description",
            Guid.NewGuid(),
            15,
            5,
            4,
            RecipeDifficulty.Easy,
            null,
            null);
        recipe.Publish(firstPublishedAt.AddDays(1), true, 1, [1]);

        Assert.Equal("draft-recipe", recipe.Slug);
        Assert.Equal(firstPublishedAt, recipe.PublishedAt);
        Assert.Equal(RecipeStatus.Published, recipe.Status);
    }

    /// <summary>Verifies that publish rejects every incomplete aggregate.</summary>
    /// <param name="categoryExists">The category exists supplied to this operation.</param>
    /// <param name="ingredientCount">The ingredient count supplied to this operation.</param>
    /// <param name="stepNumbers">The step numbers supplied to this operation.</param>
    [Theory]
    [InlineData(false, 1, new[] { 1 })]
    [InlineData(true, 0, new[] { 1 })]
    [InlineData(true, 1, new int[0])]
    [InlineData(true, 1, new[] { 2 })]
    public void PublishRejectsEveryIncompleteAggregate(
        bool categoryExists,
        int ingredientCount,
        int[] stepNumbers)
    {
        var recipe = CreateRecipe();

        var exception = Assert.Throws<DomainException>(() =>
            recipe.Publish(DateTimeOffset.UtcNow, categoryExists, ingredientCount, stepNumbers));

        Assert.Equal("RECIPE_PUBLISH_INCOMPLETE", exception.Code);
        Assert.Equal(RecipeStatus.Draft, recipe.Status);
    }

    /// <summary>Verifies that nutrition rejects negative values.</summary>
    [Fact]
    public void NutritionRejectsNegativeValues()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => RecipeNutrition.Create(-1, null, null, null, null, null));
    }

    /// <summary>Verifies that nutrition normalizes empty input and preserves all values.</summary>
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

    /// <summary>Verifies that nutrition rejects every negative field.</summary>
    [Fact]
    public void NutritionRejectsEveryNegativeField()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => RecipeNutrition.Create(null, -1m, null, null, null, null));
        Assert.Throws<ArgumentOutOfRangeException>(() => RecipeNutrition.Create(null, null, -1m, null, null, null));
        Assert.Throws<ArgumentOutOfRangeException>(() => RecipeNutrition.Create(null, null, null, -1m, null, null));
        Assert.Throws<ArgumentOutOfRangeException>(() => RecipeNutrition.Create(null, null, null, null, -1m, null));
        Assert.Throws<ArgumentOutOfRangeException>(() => RecipeNutrition.Create(null, null, null, null, null, -1m));
    }

    /// <summary>Verifies that recipe rejects invalid data and transitions.</summary>
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
        recipe.Update(
            "valid-slug",
            "Valid title",
            "Description",
            Guid.NewGuid(),
            10,
            0,
            2,
            RecipeDifficulty.Easy,
            new string('x', 10_001),
            null);
        Assert.Equal(10_001, recipe.Instructions.Length);
    }

    /// <summary>Verifies that category rejects invalid data and normalizes optional fields.</summary>
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

    /// <summary>Verifies that slug handles empty ascii output repeated separators and maximum length.</summary>
    [Fact]
    public void SlugHandlesEmptyAsciiOutputRepeatedSeparatorsAndMaximumLength()
    {
        Assert.Equal("item", Slug.From("你好", 20));
        Assert.Equal("a-b", Slug.From("A---B", 20));
        Assert.Equal("very-long", Slug.From("Very long recipe title", 9));
        Assert.Throws<ArgumentException>(() => Slug.From(" ", 20));
    }

    /// <summary>Verifies that ingredient supports nullable quantity and rejects invalid quantity.</summary>
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

    /// <summary>Verifies that step and image validate composition metadata.</summary>
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
        image.MarkPending();
        Assert.Equal(ImageProcessingStatus.Pending, image.ProcessingStatus);
        Assert.Null(image.MediumObjectKey);
        Assert.Null(image.ThumbnailObjectKey);
        Assert.Throws<DomainException>(() => RecipeStep.Create(
            Guid.NewGuid(), Guid.NewGuid(), 0, "Bước", "Mô tả", null, null));
    }

    /// <summary>Verifies that archived recipe rejects composition mutation.</summary>
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
        Guid.NewGuid().ToString(),
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
