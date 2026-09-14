using CulinaryBlog.Domain.Common;

namespace CulinaryBlog.Domain.Recipes;

public sealed class RecipeStep : BaseEntity
{
    private RecipeStep()
    {
    }

    public Guid RecipeId { get; private set; }

    public int StepNumber { get; private set; }

    public string Title { get; private set; } = string.Empty;

    public string Description { get; private set; } = string.Empty;

    public int? TimerMinutes { get; private set; }

    public string? ImageUrl { get; private set; }

    public static RecipeStep Create(
        Guid id,
        Guid recipeId,
        int stepNumber,
        string title,
        string description,
        int? timerMinutes,
        string? imageUrl)
    {
        var step = new RecipeStep { Id = id, RecipeId = recipeId };
        step.Update(stepNumber, title, description, timerMinutes, imageUrl);
        return step;
    }

    public void Update(int stepNumber, string title, string description, int? timerMinutes, string? imageUrl)
    {
        var trimmedTitle = title.Trim();
        var trimmedDescription = description.Trim();
        if (stepNumber < 1 || trimmedTitle.Length is < 1 or > 200 || trimmedDescription.Length is < 1 or > 2000 || timerMinutes < 0)
        {
            throw new DomainException("RECIPE_DATA_INVALID", "Recipe step data is invalid.");
        }

        if (imageUrl?.Trim().Length > 500)
        {
            throw new DomainException("RECIPE_DATA_INVALID", "Recipe step image URL is too long.");
        }

        StepNumber = stepNumber;
        Title = trimmedTitle;
        Description = trimmedDescription;
        TimerMinutes = timerMinutes;
        ImageUrl = string.IsNullOrWhiteSpace(imageUrl) ? null : imageUrl.Trim();
    }

    public void MoveTo(int stepNumber) => StepNumber = stepNumber;

    public void Delete() => SoftDelete();
}
