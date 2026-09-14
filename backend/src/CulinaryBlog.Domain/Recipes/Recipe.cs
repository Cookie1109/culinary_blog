using CulinaryBlog.Domain.Common;

namespace CulinaryBlog.Domain.Recipes;

public sealed class Recipe : BaseEntity
{
    private Recipe()
    {
    }

    public string Title { get; private set; } = string.Empty;

    public string Slug { get; private set; } = string.Empty;

    public string Description { get; private set; } = string.Empty;

    public string? Instructions { get; private set; }

    public int PrepTime { get; private set; }

    public int CookTime { get; private set; }

    public int Servings { get; private set; }

    public RecipeDifficulty Difficulty { get; private set; }

    public RecipeStatus Status { get; private set; }

    public Guid CategoryId { get; private set; }

    public Guid AuthorId { get; private set; }

    public DateTimeOffset? PublishedAt { get; private set; }

    public RecipeNutrition? Nutrition { get; private set; }

    public void MarkCompositionChanged()
    {
        if (Status == RecipeStatus.Archived)
        {
            throw new DomainException("RECIPE_INVALID_TRANSITION", "An archived recipe must be unarchived before editing.");
        }

        Version++;
    }

    public static Recipe Create(
        Guid id,
        Guid authorId,
        string slug,
        string title,
        string description,
        Guid categoryId,
        int prepTime,
        int cookTime,
        int servings,
        RecipeDifficulty difficulty,
        string? instructions,
        RecipeNutrition? nutrition)
    {
        var recipe = new Recipe
        {
            Id = id,
            AuthorId = authorId,
            Slug = slug,
            Status = RecipeStatus.Draft,
        };
        recipe.Update(slug, title, description, categoryId, prepTime, cookTime, servings, difficulty, instructions, nutrition);
        return recipe;
    }

    public void Update(
        string slug,
        string title,
        string description,
        Guid categoryId,
        int prepTime,
        int cookTime,
        int servings,
        RecipeDifficulty difficulty,
        string? instructions,
        RecipeNutrition? nutrition)
    {
        if (Status == RecipeStatus.Archived)
        {
            throw new DomainException("RECIPE_INVALID_TRANSITION", "An archived recipe must be unarchived before editing.");
        }

        var trimmedTitle = title.Trim();
        var trimmedDescription = description.Trim();
        if (trimmedTitle.Length is < 5 or > 200 || trimmedDescription.Length is < 1 or > 2000)
        {
            throw new DomainException("RECIPE_DATA_INVALID", "Recipe title or description is invalid.");
        }

        if (categoryId == Guid.Empty || prepTime < 1 || cookTime < 0 || servings < 1 || !Enum.IsDefined(difficulty))
        {
            throw new DomainException("RECIPE_DATA_INVALID", "Recipe category, time, servings or difficulty is invalid.");
        }

        if (instructions?.Trim().Length > 10000)
        {
            throw new DomainException("RECIPE_DATA_INVALID", "Recipe instructions are too long.");
        }

        Title = trimmedTitle;
        Description = trimmedDescription;
        CategoryId = categoryId;
        PrepTime = prepTime;
        CookTime = cookTime;
        Servings = servings;
        Difficulty = difficulty;
        Instructions = string.IsNullOrWhiteSpace(instructions) ? null : instructions.Trim();
        Nutrition = nutrition;

        if (PublishedAt is null)
        {
            Slug = slug;
        }
    }

    public void Archive()
    {
        if (Status == RecipeStatus.Archived)
        {
            throw new DomainException("RECIPE_INVALID_TRANSITION", "The recipe is already archived.");
        }

        Status = RecipeStatus.Archived;
    }

    public void Unarchive()
    {
        if (Status != RecipeStatus.Archived)
        {
            throw new DomainException("RECIPE_INVALID_TRANSITION", "Only an archived recipe can be unarchived.");
        }

        Status = RecipeStatus.Draft;
    }

    public void Delete() => SoftDelete();
}
