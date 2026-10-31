// Culinary Blog. Authorship is recorded in the repository history.

using CulinaryBlog.Application.Abstractions.Caching;
using CulinaryBlog.Application.Abstractions.Persistence;
using CulinaryBlog.Application.Content;
using CulinaryBlog.Domain.Recipes;
using FluentValidation;
using MediatR;

namespace CulinaryBlog.Application.Features.Recipes.Queries;

/// <summary>Carries list published recipes query data.</summary>
/// <param name="CategoryId">The category id supplied to this operation.</param>
/// <param name="Difficulty">The difficulty supplied to this operation.</param>
/// <param name="MaxCookTime">The max cook time supplied to this operation.</param>
/// <param name="MinServings">The min servings supplied to this operation.</param>
/// <param name="MinPrepTime">The min prep time supplied to this operation.</param>
/// <param name="MaxPrepTime">The max prep time supplied to this operation.</param>
/// <param name="Sort">The sort supplied to this operation.</param>
/// <param name="Page">The page supplied to this operation.</param>
/// <param name="PageSize">The page size supplied to this operation.</param>
/// <param name="Category">The category supplied to this operation.</param>
/// <param name="MaxTime">The max time supplied to this operation.</param>
/// <param name="SortBy">The sort by supplied to this operation.</param>
/// <param name="SortOrder">The sort order supplied to this operation.</param>
public sealed record ListPublishedRecipesQuery(
    Guid? CategoryId,
    RecipeDifficulty? Difficulty,
    int? MaxCookTime,
    int? MinServings,
    int? MinPrepTime,
    int? MaxPrepTime,
    string? Sort,
    int Page,
    int PageSize,
    string? Category = null,
    int? MaxTime = null,
    string? SortBy = null,
    string? SortOrder = null) : ICacheableRequest<PageEnvelope<RecipeDto>>
{
    /// <summary>Gets the canonical sort.</summary>
    public string CanonicalSort => RecipeSort.Resolve(this.Sort, this.SortBy, this.SortOrder);

    /// <summary>Gets the cache key.</summary>
    public string CacheKey => PublicCacheKeys.RecipeList(
        this.CategoryId, this.Difficulty, this.MaxCookTime, this.MinServings, this.MinPrepTime, this.MaxPrepTime, this.CanonicalSort, this.Page, this.PageSize, this.Category, this.MaxTime);

    /// <summary>Gets the cache duration.</summary>
    public TimeSpan CacheDuration => TimeSpan.FromMinutes(15);

    /// <summary>Gets the cache tags.</summary>
    public IReadOnlyCollection<string> CacheTags => PublicCacheKeys.RecipeTags;
}
