// Culinary Blog. Authorship is recorded in the repository history.

using CulinaryBlog.Domain.Common;

namespace CulinaryBlog.Domain.Recipes;

/// <summary>Represents the recipe.</summary>
public sealed class Recipe : BaseEntity
{
    private Recipe()
    {
    }

    /// <summary>Gets the title.</summary>
    public string Title { get; private set; } = string.Empty;

    /// <summary>Gets the slug.</summary>
    public string Slug { get; private set; } = string.Empty;

    /// <summary>Gets the description.</summary>
    public string Description { get; private set; } = string.Empty;

    /// <summary>Gets the instructions.</summary>
    public string Instructions { get; private set; } = string.Empty;

    /// <summary>Gets the prep time.</summary>
    public int PrepTime { get; private set; }

    /// <summary>Gets the cook time.</summary>
    public int CookTime { get; private set; }

    /// <summary>Gets the servings.</summary>
    public int Servings { get; private set; }

    /// <summary>Gets the difficulty.</summary>
    public RecipeDifficulty Difficulty { get; private set; }

    /// <summary>Gets the status.</summary>
    public RecipeStatus Status { get; private set; }

    /// <summary>Gets the category id.</summary>
    public Guid CategoryId { get; private set; }

    /// <summary>Gets the author id.</summary>
    public string AuthorId { get; private set; } = string.Empty;

    /// <summary>Gets the published at.</summary>
    public DateTimeOffset? PublishedAt { get; private set; }

    /// <summary>Gets the nutrition.</summary>
    public RecipeNutrition? Nutrition { get; private set; }

    /// <summary>Creates a draft recipe with the supplied identity, author and content.</summary>
    /// <param name="id">The identifier of the requested resource.</param>
    /// <param name="authorId">The identifier of the user performing or owning the operation.</param>
    /// <param name="slug">The slug supplied to this operation.</param>
    /// <param name="title">The title supplied to this operation.</param>
    /// <param name="description">The description supplied to this operation.</param>
    /// <param name="categoryId">The category id supplied to this operation.</param>
    /// <param name="prepTime">The preparation time in minutes.</param>
    /// <param name="cookTime">The cooking time in minutes.</param>
    /// <param name="servings">The servings supplied to this operation.</param>
    /// <param name="difficulty">The difficulty supplied to this operation.</param>
    /// <param name="instructions">The instructions supplied to this operation.</param>
    /// <param name="nutrition">The nutrition supplied to this operation.</param>
    /// <returns>The create result.</returns>
    public static Recipe Create(
        Guid id,
        string authorId,
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

    /// <summary>Advances the recipe version after a composition change.</summary>
    public void MarkCompositionChanged()
    {
        if (this.Status == RecipeStatus.Archived)
        {
            throw new DomainException("RECIPE_INVALID_TRANSITION", "An archived recipe must be unarchived before editing.");
        }

        this.Version++;
    }

    /// <summary>Updates the recipe.</summary>
    /// <param name="slug">The slug supplied to this operation.</param>
    /// <param name="title">The title supplied to this operation.</param>
    /// <param name="description">The description supplied to this operation.</param>
    /// <param name="categoryId">The category id supplied to this operation.</param>
    /// <param name="prepTime">The preparation time in minutes.</param>
    /// <param name="cookTime">The cooking time in minutes.</param>
    /// <param name="servings">The servings supplied to this operation.</param>
    /// <param name="difficulty">The difficulty supplied to this operation.</param>
    /// <param name="instructions">The instructions supplied to this operation.</param>
    /// <param name="nutrition">The nutrition supplied to this operation.</param>
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
        if (this.Status == RecipeStatus.Archived)
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

        this.Title = trimmedTitle;
        this.Description = trimmedDescription;
        this.CategoryId = categoryId;
        this.PrepTime = prepTime;
        this.CookTime = cookTime;
        this.Servings = servings;
        this.Difficulty = difficulty;
        this.Instructions = instructions?.Trim() ?? string.Empty;
        this.Nutrition = nutrition;

        if (this.PublishedAt is null)
        {
            this.Slug = slug;
        }
    }

    /// <summary>Publishes a draft only when it contains ingredients and steps.</summary>
    /// <param name="publishedAt">The optional UTC publication time.</param>
    /// <param name="categoryExists">The category exists supplied to this operation.</param>
    /// <param name="ingredientCount">The ingredient count supplied to this operation.</param>
    /// <param name="stepNumbers">The step numbers supplied to this operation.</param>
    public void Publish(
        DateTimeOffset publishedAt,
        bool categoryExists,
        int ingredientCount,
        IReadOnlyCollection<int> stepNumbers)
    {
        if (this.Status != RecipeStatus.Draft)
        {
            throw new DomainException("RECIPE_INVALID_TRANSITION", "Only a draft recipe can be published.");
        }

        var hasContinuousSteps = stepNumbers.Count > 0 &&
            stepNumbers.Order().SequenceEqual(Enumerable.Range(1, stepNumbers.Count));
        if (!categoryExists ||
            this.Title.Length is < 5 or > 200 ||
            this.Description.Length is < 1 or > 2000 ||
            this.CategoryId == Guid.Empty ||
            this.PrepTime < 1 ||
            this.CookTime < 0 ||
            this.Servings < 1 ||
            !Enum.IsDefined(this.Difficulty) ||
            ingredientCount < 1 ||
            !hasContinuousSteps)
        {
            throw new DomainException(
                "RECIPE_PUBLISH_INCOMPLETE",
                "Recipe requires valid core data, a category, at least one ingredient, and continuously numbered steps before publishing.");
        }

        this.Status = RecipeStatus.Published;
        this.PublishedAt ??= publishedAt;
    }

    /// <summary>Returns a published recipe to Draft so public reads no longer expose it.</summary>
    public void Unpublish()
    {
        if (this.Status != RecipeStatus.Published)
        {
            throw new DomainException("RECIPE_INVALID_TRANSITION", "Only a published recipe can be unpublished.");
        }

        this.Status = RecipeStatus.Draft;
    }

    /// <summary>Archives a recipe so it cannot be edited or publicly discovered.</summary>
    public void Archive()
    {
        if (this.Status == RecipeStatus.Archived)
        {
            throw new DomainException("RECIPE_INVALID_TRANSITION", "The recipe is already archived.");
        }

        this.Status = RecipeStatus.Archived;
    }

    /// <summary>Returns an archived recipe to Draft.</summary>
    public void Unarchive()
    {
        if (this.Status != RecipeStatus.Archived)
        {
            throw new DomainException("RECIPE_INVALID_TRANSITION", "Only an archived recipe can be unarchived.");
        }

        this.Status = RecipeStatus.Draft;
    }

    /// <summary>Deletes the recipe.</summary>
    public void Delete() => this.SoftDelete();
}
