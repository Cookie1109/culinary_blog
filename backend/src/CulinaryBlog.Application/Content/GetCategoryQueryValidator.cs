// Culinary Blog. Authorship is recorded in the repository history.

using CulinaryBlog.Application.Abstractions.Caching;
using CulinaryBlog.Application.Behaviors;
using CulinaryBlog.Application.Content;
using CulinaryBlog.Domain.Recipes;
using FluentValidation;
using MediatR;

namespace CulinaryBlog.Application.Content;

/// <summary>Represents the get category query validator.</summary>
internal sealed class GetCategoryQueryValidator : AbstractValidator<GetCategoryQuery>
{
    /// <summary>Initializes a new instance of the <see cref="GetCategoryQueryValidator"/> class.</summary>
    public GetCategoryQueryValidator()
    {
        this.RuleFor(query => query).Custom((query, _) => RecipeSort.Resolve(query.Sort, query.SortBy, query.SortOrder));
        this.RuleFor(request => request.Slug).NotEmpty().MaximumLength(120);
        this.RuleFor(request => request.Page).GreaterThan(0);
        this.RuleFor(request => request.PageSize).InclusiveBetween(1, 50);
        this.RuleFor(request => request.Sort).Must(value => value is null or "createdAt" or "-createdAt" or "title" or "-title"
            or "cookTime" or "-cookTime" or "prepTime" or "-prepTime" or "servings" or "-servings" or "quickest");
    }
}
