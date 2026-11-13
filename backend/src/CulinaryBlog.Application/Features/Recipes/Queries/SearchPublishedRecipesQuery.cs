// Culinary Blog. Authorship is recorded in the repository history.

using CulinaryBlog.Application.Abstractions.Caching;
using CulinaryBlog.Application.Abstractions.Persistence;
using CulinaryBlog.Application.Content;
using CulinaryBlog.Domain.Recipes;
using FluentValidation;
using MediatR;

namespace CulinaryBlog.Application.Features.Recipes.Queries;

/// <summary>Carries search published recipes query data.</summary>
/// <param name="Search">The search supplied to this operation.</param>
/// <param name="Category">The category supplied to this operation.</param>
/// <param name="Difficulty">The difficulty supplied to this operation.</param>
/// <param name="MaxTime">The max time supplied to this operation.</param>
/// <param name="Sort">The sort supplied to this operation.</param>
/// <param name="Page">The page supplied to this operation.</param>
/// <param name="PageSize">The page size supplied to this operation.</param>
/// <param name="CategoryId">The category id supplied to this operation.</param>
/// <param name="MaxCookTime">The max cook time supplied to this operation.</param>
/// <param name="MinServings">The min servings supplied to this operation.</param>
/// <param name="MinPrepTime">The min prep time supplied to this operation.</param>
/// <param name="MaxPrepTime">The max prep time supplied to this operation.</param>
/// <param name="SortBy">The sort by supplied to this operation.</param>
/// <param name="SortOrder">The sort order supplied to this operation.</param>
public sealed record SearchPublishedRecipesQuery(
    string? Search,
    string? Category,
    RecipeDifficulty? Difficulty,
    int? MaxTime,
    string? Sort,
    int Page,
    int PageSize,
    Guid? CategoryId = null,
    int? MaxCookTime = null,
    int? MinServings = null,
    int? MinPrepTime = null,
    int? MaxPrepTime = null,
    string? SortBy = null,
    string? SortOrder = null) : ICacheableRequest<PageEnvelope<RecipeDto>>
{
    /// <summary>Gets the canonical sort.</summary>
    public string CanonicalSort => RecipeSort.Resolve(this.Sort, this.SortBy, this.SortOrder, search: true);

    /// <summary>Gets the cache key.</summary>
    public string CacheKey => PublicCacheKeys.Search(
        this.Search,
        this.Category,
        this.Difficulty,
        this.MaxTime,
        this.CanonicalSort,
        this.Page,
        this.PageSize,
        this.CategoryId,
        this.MaxCookTime,
        this.MinServings,
        this.MinPrepTime,
        this.MaxPrepTime);

    /// <summary>Gets the cache duration.</summary>
    public TimeSpan CacheDuration => TimeSpan.FromMinutes(1);

    /// <summary>Gets the cache tags.</summary>
    public IReadOnlyCollection<string> CacheTags => PublicCacheKeys.RecipeTags;
}
