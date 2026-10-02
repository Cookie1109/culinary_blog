using CulinaryBlog.Application.Abstractions.Caching;
using CulinaryBlog.Application.Abstractions.Persistence;
using CulinaryBlog.Application.Content;
using CulinaryBlog.Domain.Recipes;
using FluentValidation;
using MediatR;

namespace CulinaryBlog.Application.Features.Recipes.Queries;

public sealed record ListPublishedRecipesQuery(
    Guid? CategoryId,
    RecipeDifficulty? Difficulty,
    int? MaxCookTime,
    int? MinServings,
    int? MinPrepTime,
    int? MaxPrepTime,
    string? Sort,
    int Page,
    int PageSize) : ICacheableRequest<PageEnvelope<RecipeDto>>
{
    public string CacheKey => PublicCacheKeys.RecipeList(
        CategoryId, Difficulty, MaxCookTime, MinServings, MinPrepTime, MaxPrepTime, Sort, Page, PageSize);

    public TimeSpan CacheDuration => TimeSpan.FromMinutes(15);

    public IReadOnlyCollection<string> CacheTags => PublicCacheKeys.RecipeTags;
}

internal sealed class ListPublishedRecipesQueryValidator : AbstractValidator<ListPublishedRecipesQuery>
{
    private static readonly string[] AllowedSorts =
    [
        "-createdAt", "createdAt", "newest", "oldest",
        "title", "-title", "az", "za",
        "cookTime", "-cookTime", "prepTime", "-prepTime", "servings", "-servings",
    ];

    public ListPublishedRecipesQueryValidator()
    {
        RuleFor(query => query.CategoryId).NotEqual(Guid.Empty).When(query => query.CategoryId.HasValue);
        RuleFor(query => query.MaxCookTime).GreaterThanOrEqualTo(0).When(query => query.MaxCookTime.HasValue);
        RuleFor(query => query.MinServings).GreaterThan(0).When(query => query.MinServings.HasValue);
        RuleFor(query => query.MinPrepTime).GreaterThan(0).When(query => query.MinPrepTime.HasValue);
        RuleFor(query => query.MaxPrepTime).GreaterThan(0).When(query => query.MaxPrepTime.HasValue);
        RuleFor(query => query.MaxPrepTime)
            .GreaterThanOrEqualTo(query => query.MinPrepTime)
            .When(query => query.MinPrepTime.HasValue && query.MaxPrepTime.HasValue);
        RuleFor(query => query.Sort)
            .Must(value => string.IsNullOrEmpty(value) || AllowedSorts.Contains(value, StringComparer.Ordinal));
        RuleFor(query => query.Page).GreaterThan(0);
        RuleFor(query => query.PageSize).InclusiveBetween(1, 50);
    }
}

internal sealed class ListPublishedRecipesQueryHandler(IRecipeSearchRepository repository)
    : IRequestHandler<ListPublishedRecipesQuery, PageEnvelope<RecipeDto>>
{
    public Task<PageEnvelope<RecipeDto>> Handle(
        ListPublishedRecipesQuery request,
        CancellationToken cancellationToken) =>
        repository.ListPublishedRecipesAsync(
            request.CategoryId,
            request.Difficulty,
            request.MaxCookTime,
            request.MinServings,
            request.MinPrepTime,
            request.MaxPrepTime,
            request.Sort,
            request.Page,
            request.PageSize,
            cancellationToken);
}
