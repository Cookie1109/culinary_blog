// Culinary Blog. Authorship is recorded in the repository history.

using FluentValidation;

namespace CulinaryBlog.Application.Content;

/// <summary>Represents the nutrition update request validator.</summary>
internal sealed class NutritionUpdateRequestValidator : AbstractValidator<NutritionUpdateRequest>
{
    /// <summary>Initializes a new instance of the <see cref="NutritionUpdateRequestValidator"/> class.</summary>
    public NutritionUpdateRequestValidator()
    {
        this.RuleFor(request => request.Calories.Value).GreaterThanOrEqualTo(0).LessThanOrEqualTo(999999.99m).PrecisionScale(8, 2, true)
            .When(request => request.Calories.IsSpecified).OverridePropertyName("Calories");
        this.RuleFor(request => request.Protein.Value).GreaterThanOrEqualTo(0).LessThanOrEqualTo(999999.99m).PrecisionScale(8, 2, true)
            .When(request => request.Protein.IsSpecified).OverridePropertyName("Protein");
        this.RuleFor(request => request.Carbohydrates.Value).GreaterThanOrEqualTo(0).LessThanOrEqualTo(999999.99m).PrecisionScale(8, 2, true)
            .When(request => request.Carbohydrates.IsSpecified).OverridePropertyName("Carbohydrates");
        this.RuleFor(request => request.Fat.Value).GreaterThanOrEqualTo(0).LessThanOrEqualTo(999999.99m).PrecisionScale(8, 2, true)
            .When(request => request.Fat.IsSpecified).OverridePropertyName("Fat");
        this.RuleFor(request => request.Fiber.Value).GreaterThanOrEqualTo(0).LessThanOrEqualTo(999999.99m).PrecisionScale(8, 2, true)
            .When(request => request.Fiber.IsSpecified).OverridePropertyName("Fiber");
        this.RuleFor(request => request.Sodium.Value).GreaterThanOrEqualTo(0).LessThanOrEqualTo(999999.99m).PrecisionScale(8, 2, true)
            .When(request => request.Sodium.IsSpecified).OverridePropertyName("Sodium");
    }
}
