// Culinary Blog. Authorship is recorded in the repository history.

using CulinaryBlog.Application.Abstractions.Caching;
using CulinaryBlog.Application.Abstractions.Persistence;
using CulinaryBlog.Application.Content;
using CulinaryBlog.Domain.Recipes;
using FluentValidation;
using MediatR;

namespace CulinaryBlog.Application.Features.Recipes.Queries;

/// <summary>Represents the search published recipes query validator.</summary>
internal sealed class SearchPublishedRecipesQueryValidator : AbstractValidator<SearchPublishedRecipesQuery>
{
    /// <summary>Initializes a new instance of the <see cref="SearchPublishedRecipesQueryValidator"/> class.</summary>
    public SearchPublishedRecipesQueryValidator()
    {
        this.RuleFor(query => query).Custom((query, _) => RecipeSort.Resolve(query.Sort, query.SortBy, query.SortOrder, search: true));
        this.RuleFor(query => query.Search).NotEmpty().MaximumLength(100)
            .Must(value => value is not null && value.Trim().Length >= 2);
        this.RuleFor(query => query.Category).MaximumLength(120);
        this.RuleFor(query => query.CategoryId).NotEqual(Guid.Empty).When(query => query.CategoryId.HasValue);
        this.RuleFor(query => query.Difficulty).IsInEnum().When(query => query.Difficulty.HasValue);
        this.RuleFor(query => query.MaxTime).GreaterThan(0).When(query => query.MaxTime.HasValue);
        this.RuleFor(query => query.MaxCookTime).GreaterThanOrEqualTo(0).When(query => query.MaxCookTime.HasValue);
        this.RuleFor(query => query.MinServings).GreaterThan(0).When(query => query.MinServings.HasValue);
        this.RuleFor(query => query.MinPrepTime).GreaterThan(0).When(query => query.MinPrepTime.HasValue);
        this.RuleFor(query => query.MaxPrepTime).GreaterThan(0).When(query => query.MaxPrepTime.HasValue);
        this.RuleFor(query => query.MaxPrepTime).GreaterThanOrEqualTo(query => query.MinPrepTime)
            .When(query => query.MinPrepTime.HasValue && query.MaxPrepTime.HasValue);
        this.RuleFor(query => query.Sort)
            .Must(value => value is null or "" or "relevance" or "newest" or "quickest" or "az" or "za" or "oldest"
                or "createdAt" or "-createdAt" or "title" or "-title" or "cookTime" or "-cookTime"
                or "prepTime" or "-prepTime" or "servings" or "-servings");
        this.RuleFor(query => query.Page).GreaterThan(0);
        this.RuleFor(query => query.PageSize).InclusiveBetween(1, 50);
    }
}
