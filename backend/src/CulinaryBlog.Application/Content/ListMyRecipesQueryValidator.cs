// Culinary Blog. Authorship is recorded in the repository history.

using FluentValidation;

namespace CulinaryBlog.Application.Content;

/// <summary>Represents the list my recipes query validator.</summary>
internal sealed class ListMyRecipesQueryValidator : AbstractValidator<ListMyRecipesQuery>
{
    /// <summary>Initializes a new instance of the <see cref="ListMyRecipesQueryValidator"/> class.</summary>
    public ListMyRecipesQueryValidator()
    {
        this.RuleFor(request => request.UserId).NotEmpty();
        this.RuleFor(request => request.Status).IsInEnum().When(request => request.Status.HasValue);
        this.RuleFor(request => request.Search).MaximumLength(100);
        this.RuleFor(request => request.Page).GreaterThan(0);
        this.RuleFor(request => request.PageSize).InclusiveBetween(1, 50);
    }
}
