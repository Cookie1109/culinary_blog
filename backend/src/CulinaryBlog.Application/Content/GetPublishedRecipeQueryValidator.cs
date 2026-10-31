// Culinary Blog. Authorship is recorded in the repository history.

using FluentValidation;

namespace CulinaryBlog.Application.Content;

/// <summary>Represents the get published recipe query validator.</summary>
internal sealed class GetPublishedRecipeQueryValidator : AbstractValidator<GetPublishedRecipeQuery>
{
    /// <summary>Initializes a new instance of the <see cref="GetPublishedRecipeQueryValidator"/> class.</summary>
    public GetPublishedRecipeQueryValidator() => this.RuleFor(request => request.Slug).NotEmpty().MaximumLength(220);
}
