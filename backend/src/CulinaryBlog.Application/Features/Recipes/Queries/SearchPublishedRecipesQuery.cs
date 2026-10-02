using CulinaryBlog.Application.Abstractions.Persistence;
using CulinaryBlog.Application.Content;
using CulinaryBlog.Domain.Recipes;
using FluentValidation;
using MediatR;

namespace CulinaryBlog.Application.Features.Recipes.Queries;

public sealed record SearchPublishedRecipesQuery(
    string? Search,
    string? Category,
    RecipeDifficulty? Difficulty,
    int? MaxTime,
    string? Sort,
    int Page,
    int PageSize) : IRequest<PageEnvelope<RecipeDto>>;

internal sealed class SearchPublishedRecipesQueryValidator : AbstractValidator<SearchPublishedRecipesQuery>
{
    public SearchPublishedRecipesQueryValidator()
    {
        RuleFor(query => query.Search).MaximumLength(100)
            .Must(value => string.IsNullOrWhiteSpace(value) || value.Trim().Length >= 2);
        RuleFor(query => query.Category).MaximumLength(120);
        RuleFor(query => query.MaxTime).GreaterThan(0).When(query => query.MaxTime.HasValue);
        RuleFor(query => query.Sort)
            .Must(value => value is null or "" or "relevance" or "newest" or "quickest" or "az");
        RuleFor(query => query.Page).GreaterThan(0);
        RuleFor(query => query.PageSize).InclusiveBetween(1, 50);
    }
}

internal sealed class SearchPublishedRecipesQueryHandler(IRecipeSearchRepository repository)
    : IRequestHandler<SearchPublishedRecipesQuery, PageEnvelope<RecipeDto>>
{
    public Task<PageEnvelope<RecipeDto>> Handle(
        SearchPublishedRecipesQuery request,
        CancellationToken cancellationToken) =>
        repository.SearchPublishedRecipesAsync(
            request.Search,
            request.Category,
            request.Difficulty,
            request.MaxTime,
            request.Sort,
            request.Page,
            request.PageSize,
            cancellationToken);
}
