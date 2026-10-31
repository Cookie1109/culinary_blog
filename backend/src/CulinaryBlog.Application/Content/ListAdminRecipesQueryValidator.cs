// Culinary Blog. Authorship is recorded in the repository history.

using FluentValidation;

namespace CulinaryBlog.Application.Content;

/// <summary>Represents the list admin recipes query validator.</summary>
internal sealed class ListAdminRecipesQueryValidator : AbstractValidator<ListAdminRecipesQuery>
{
    /// <summary>Initializes a new instance of the <see cref="ListAdminRecipesQueryValidator"/> class.</summary>
    public ListAdminRecipesQueryValidator()
    {
        this.RuleFor(request => request.Status).IsInEnum().When(request => request.Status.HasValue);
        this.RuleFor(request => request.AuthorId).NotEmpty().MaximumLength(450).When(request => request.AuthorId is not null);
        this.RuleFor(request => request.Search).MaximumLength(100);
        this.RuleFor(request => request.Page).GreaterThan(0);
        this.RuleFor(request => request.PageSize).InclusiveBetween(1, 50);
    }
}
