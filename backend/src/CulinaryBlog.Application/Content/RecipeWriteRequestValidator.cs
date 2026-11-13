// Culinary Blog. Authorship is recorded in the repository history.

using FluentValidation;

namespace CulinaryBlog.Application.Content;

/// <summary>Represents the recipe write request validator.</summary>
internal sealed class RecipeWriteRequestValidator : AbstractValidator<RecipeWriteRequest>
{
    /// <summary>Initializes a new instance of the <see cref="RecipeWriteRequestValidator"/> class.</summary>
    public RecipeWriteRequestValidator()
    {
        this.RuleFor(request => request.Title).NotEmpty().MinimumLength(5).MaximumLength(200);
        this.RuleFor(request => request.Description).NotEmpty().MaximumLength(2000);
        this.RuleFor(request => request.CategoryId).NotEmpty();
        this.RuleFor(request => request.PrepTime).GreaterThan(0);
        this.RuleFor(request => request.CookTime).GreaterThanOrEqualTo(0);
        this.RuleFor(request => request.Servings).GreaterThan(0);
        this.RuleFor(request => request.Difficulty).IsInEnum();
        this.When(request => request.Nutrition is not null, () =>
        {
            this.RuleFor(request => request.Nutrition!.Calories).GreaterThanOrEqualTo(0).LessThanOrEqualTo(999999.99m).PrecisionScale(8, 2, true).When(request => request.Nutrition!.Calories.HasValue);
            this.RuleFor(request => request.Nutrition!.Protein).GreaterThanOrEqualTo(0).LessThanOrEqualTo(999999.99m).PrecisionScale(8, 2, true).When(request => request.Nutrition!.Protein.HasValue);
            this.RuleFor(request => request.Nutrition!.Carbohydrates).GreaterThanOrEqualTo(0).LessThanOrEqualTo(999999.99m).PrecisionScale(8, 2, true).When(request => request.Nutrition!.Carbohydrates.HasValue);
            this.RuleFor(request => request.Nutrition!.Fat).GreaterThanOrEqualTo(0).LessThanOrEqualTo(999999.99m).PrecisionScale(8, 2, true).When(request => request.Nutrition!.Fat.HasValue);
            this.RuleFor(request => request.Nutrition!.Fiber).GreaterThanOrEqualTo(0).LessThanOrEqualTo(999999.99m).PrecisionScale(8, 2, true).When(request => request.Nutrition!.Fiber.HasValue);
            this.RuleFor(request => request.Nutrition!.Sodium).GreaterThanOrEqualTo(0).LessThanOrEqualTo(999999.99m).PrecisionScale(8, 2, true).When(request => request.Nutrition!.Sodium.HasValue);
        });
    }
}
