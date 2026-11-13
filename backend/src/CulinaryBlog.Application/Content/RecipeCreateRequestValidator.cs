// Culinary Blog. Authorship is recorded in the repository history.

using FluentValidation;

namespace CulinaryBlog.Application.Content;

/// <summary>Represents the recipe create request validator.</summary>
internal sealed class RecipeCreateRequestValidator : AbstractValidator<RecipeCreateRequest>
{
    /// <summary>Initializes a new instance of the <see cref="RecipeCreateRequestValidator"/> class.</summary>
    public RecipeCreateRequestValidator()
    {
        this.RuleFor(request => (RecipeWriteRequest)request).SetValidator(new RecipeWriteRequestValidator());
        this.RuleForEach(request => request.Ingredients).NotNull().SetValidator(new IngredientWriteRequestValidator());
        this.RuleForEach(request => request.Steps).NotNull().SetValidator(new StepWriteRequestValidator());
    }
}
