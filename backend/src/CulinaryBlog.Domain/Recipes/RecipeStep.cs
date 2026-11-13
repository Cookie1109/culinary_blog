// Culinary Blog. Authorship is recorded in the repository history.

using CulinaryBlog.Domain.Common;

namespace CulinaryBlog.Domain.Recipes;

/// <summary>Represents the recipe step.</summary>
public sealed class RecipeStep : BaseEntity
{
    private RecipeStep()
    {
    }

    /// <summary>Gets the recipe id.</summary>
    public Guid RecipeId { get; private set; }

    /// <summary>Gets the step number.</summary>
    public int StepNumber { get; private set; }

    /// <summary>Gets the title.</summary>
    public string Title { get; private set; } = string.Empty;

    /// <summary>Gets the description.</summary>
    public string Description { get; private set; } = string.Empty;

    /// <summary>Gets the timer minutes.</summary>
    public double? TimerMinutes { get; private set; }

    /// <summary>Gets the image url.</summary>
    public string? ImageUrl { get; private set; }

    /// <summary>Creates the recipe step.</summary>
    /// <param name="id">The identifier of the requested resource.</param>
    /// <param name="recipeId">The identifier of the recipe being accessed.</param>
    /// <param name="stepNumber">The step number supplied to this operation.</param>
    /// <param name="title">The title supplied to this operation.</param>
    /// <param name="description">The description supplied to this operation.</param>
    /// <param name="timerMinutes">The timer minutes supplied to this operation.</param>
    /// <param name="imageUrl">The image url supplied to this operation.</param>
    /// <returns>The create result.</returns>
    public static RecipeStep Create(
        Guid id,
        Guid recipeId,
        int stepNumber,
        string title,
        string description,
        double? timerMinutes,
        string? imageUrl)
    {
        var step = new RecipeStep { Id = id, RecipeId = recipeId };
        step.Update(stepNumber, title, description, timerMinutes, imageUrl);
        return step;
    }

    /// <summary>Updates the recipe step.</summary>
    /// <param name="stepNumber">The step number supplied to this operation.</param>
    /// <param name="title">The title supplied to this operation.</param>
    /// <param name="description">The description supplied to this operation.</param>
    /// <param name="timerMinutes">The timer minutes supplied to this operation.</param>
    /// <param name="imageUrl">The image url supplied to this operation.</param>
    public void Update(int stepNumber, string title, string description, double? timerMinutes, string? imageUrl)
    {
        var trimmedTitle = title.Trim();
        var trimmedDescription = description.Trim();
        if (stepNumber < 1 || trimmedTitle.Length is < 1 or > 200 || trimmedDescription.Length is < 1 or > 2000 || timerMinutes < 0 ||
            (timerMinutes.HasValue && !double.IsFinite(timerMinutes.Value)))
        {
            throw new DomainException("RECIPE_DATA_INVALID", "Recipe step data is invalid.");
        }

        if (imageUrl?.Trim().Length > 500)
        {
            throw new DomainException("RECIPE_DATA_INVALID", "Recipe step image URL is too long.");
        }

        this.StepNumber = stepNumber;
        this.Title = trimmedTitle;
        this.Description = trimmedDescription;
        this.TimerMinutes = timerMinutes;
        this.ImageUrl = string.IsNullOrWhiteSpace(imageUrl) ? null : imageUrl.Trim();
    }

    /// <summary>Performs the move to operation.</summary>
    /// <param name="stepNumber">The step number supplied to this operation.</param>
    public void MoveTo(int stepNumber) => this.StepNumber = stepNumber;

    /// <summary>Deletes the recipe step.</summary>
    public void Delete() => this.SoftDelete();
}
