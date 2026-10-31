// Culinary Blog. Authorship is recorded in the repository history.

using CulinaryBlog.Application.Abstractions.Caching;
using CulinaryBlog.Application.Abstractions.Persistence;
using CulinaryBlog.Application.Content;
using CulinaryBlog.Domain.Recipes;
using FluentValidation;
using MediatR;

namespace CulinaryBlog.Application.Features.Recipes.Queries;

/// <summary>Represents the list published recipes query validator.</summary>
internal sealed class ListPublishedRecipesQueryValidator : AbstractValidator<ListPublishedRecipesQuery>
{
    private static readonly string[] AllowedSorts =
    [
        "-createdAt", "createdAt", "newest", "oldest",
        "title", "-title", "az", "za", "quickest",
        "cookTime", "-cookTime", "prepTime", "-prepTime", "servings", "-servings",
    ];

    /// <summary>Initializes a new instance of the <see cref="ListPublishedRecipesQueryValidator"/> class.</summary>
    public ListPublishedRecipesQueryValidator()
    {
        this.RuleFor(query => query).Custom((query, _) => RecipeSort.Resolve(query.Sort, query.SortBy, query.SortOrder));
        this.RuleFor(query => query.CategoryId).NotEqual(Guid.Empty).When(query => query.CategoryId.HasValue);
        this.RuleFor(query => query.Category).MaximumLength(120);
        this.RuleFor(query => query.MaxTime).GreaterThan(0).When(query => query.MaxTime.HasValue);
        this.RuleFor(query => query.Difficulty).IsInEnum().When(query => query.Difficulty.HasValue);
        this.RuleFor(query => query.MaxCookTime).GreaterThanOrEqualTo(0).When(query => query.MaxCookTime.HasValue);
        this.RuleFor(query => query.MinServings).GreaterThan(0).When(query => query.MinServings.HasValue);
        this.RuleFor(query => query.MinPrepTime).GreaterThan(0).When(query => query.MinPrepTime.HasValue);
        this.RuleFor(query => query.MaxPrepTime).GreaterThan(0).When(query => query.MaxPrepTime.HasValue);
        this.RuleFor(query => query.MaxPrepTime)
            .GreaterThanOrEqualTo(query => query.MinPrepTime)
            .When(query => query.MinPrepTime.HasValue && query.MaxPrepTime.HasValue);
        this.RuleFor(query => query.Sort)
            .Must(value => string.IsNullOrEmpty(value) || AllowedSorts.Contains(value, StringComparer.Ordinal));
        this.RuleFor(query => query.Page).GreaterThan(0);
        this.RuleFor(query => query.PageSize).InclusiveBetween(1, 50);
    }
}
