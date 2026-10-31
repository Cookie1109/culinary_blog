// Culinary Blog. Authorship is recorded in the repository history.

using FluentValidation;

namespace CulinaryBlog.Application.Content;

/// <summary>Represents the ingredient write request validator.</summary>
internal sealed class IngredientWriteRequestValidator : AbstractValidator<IngredientWriteRequest>
{
    /// <summary>Initializes a new instance of the <see cref="IngredientWriteRequestValidator"/> class.</summary>
    public IngredientWriteRequestValidator()
    {
        this.RuleFor(request => request.Name).NotEmpty().MaximumLength(100);
        this.RuleFor(request => request.Quantity).GreaterThan(0).LessThanOrEqualTo(9999999.999m).PrecisionScale(10, 3, true).When(request => request.Quantity.HasValue);
        this.RuleFor(request => request.Unit).MaximumLength(50);
        this.RuleFor(request => request.Notes).MaximumLength(500);
        this.RuleFor(request => request.OrderIndex).GreaterThanOrEqualTo(0);
    }
}
