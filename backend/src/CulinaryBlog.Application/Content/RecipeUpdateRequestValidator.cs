// Culinary Blog. Authorship is recorded in the repository history.

using FluentValidation;

namespace CulinaryBlog.Application.Content;

/// <summary>Represents the recipe update request validator.</summary>
internal sealed class RecipeUpdateRequestValidator : AbstractValidator<RecipeUpdateRequest>
{
    /// <summary>Initializes a new instance of the <see cref="RecipeUpdateRequestValidator"/> class.</summary>
    public RecipeUpdateRequestValidator()
    {
        this.RuleFor(request => request.Title.Value).NotEmpty().MinimumLength(5).MaximumLength(200)
            .When(request => request.Title.IsSpecified).OverridePropertyName("Title");
        this.RuleFor(request => request.Description.Value).NotEmpty().MaximumLength(2000)
            .When(request => request.Description.IsSpecified).OverridePropertyName("Description");
        this.RuleFor(request => request.CategoryId.Value).NotNull().NotEqual(Guid.Empty)
            .When(request => request.CategoryId.IsSpecified).OverridePropertyName("CategoryId");
        this.RuleFor(request => request.PrepTime.Value).NotNull().GreaterThan(0)
            .When(request => request.PrepTime.IsSpecified).OverridePropertyName("PrepTime");
        this.RuleFor(request => request.CookTime.Value).NotNull().GreaterThanOrEqualTo(0)
            .When(request => request.CookTime.IsSpecified).OverridePropertyName("CookTime");
        this.RuleFor(request => request.Servings.Value).NotNull().GreaterThan(0)
            .When(request => request.Servings.IsSpecified).OverridePropertyName("Servings");
        this.RuleFor(request => request.Difficulty.Value).NotNull().IsInEnum()
            .When(request => request.Difficulty.IsSpecified).OverridePropertyName("Difficulty");
        this.RuleFor(request => request.Nutrition.Value!).SetValidator(new NutritionUpdateRequestValidator())
            .When(request => request.Nutrition.IsSpecified && request.Nutrition.Value is not null).OverridePropertyName("Nutrition");
    }
}
