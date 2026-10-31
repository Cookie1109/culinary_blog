// Culinary Blog. Authorship is recorded in the repository history.

using FluentValidation;

namespace CulinaryBlog.Application.Content;

/// <summary>Represents the ingredient update request validator.</summary>
internal sealed class IngredientUpdateRequestValidator : AbstractValidator<IngredientUpdateRequest>
{
    /// <summary>Initializes a new instance of the <see cref="IngredientUpdateRequestValidator"/> class.</summary>
    public IngredientUpdateRequestValidator()
    {
        this.RuleFor(request => request.Name.Value).NotEmpty().MaximumLength(200)
            .When(request => request.Name.IsSpecified).OverridePropertyName("Name");
        this.RuleFor(request => request.Quantity.Value).GreaterThan(0).LessThanOrEqualTo(9999999.999m).PrecisionScale(10, 3, true)
            .When(request => request.Quantity.IsSpecified).OverridePropertyName("Quantity");
        this.RuleFor(request => request.Unit.Value).MaximumLength(50)
            .When(request => request.Unit.IsSpecified).OverridePropertyName("Unit");
        this.RuleFor(request => request.Notes.Value).MaximumLength(500)
            .When(request => request.Notes.IsSpecified).OverridePropertyName("Notes");
        this.RuleFor(request => request.OrderIndex.Value).NotNull().GreaterThanOrEqualTo(0)
            .When(request => request.OrderIndex.IsSpecified).OverridePropertyName("OrderIndex");
    }
}
