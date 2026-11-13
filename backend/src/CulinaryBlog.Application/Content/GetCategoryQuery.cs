// Culinary Blog. Authorship is recorded in the repository history.

using CulinaryBlog.Application.Abstractions.Caching;
using CulinaryBlog.Application.Behaviors;
using CulinaryBlog.Application.Content;
using CulinaryBlog.Domain.Recipes;
using FluentValidation;
using MediatR;

namespace CulinaryBlog.Application.Content;

/// <summary>Carries get category query data.</summary>
/// <param name="Slug">The slug supplied to this operation.</param>
/// <param name="Page">The page supplied to this operation.</param>
/// <param name="PageSize">The page size supplied to this operation.</param>
/// <param name="Sort">The sort supplied to this operation.</param>
/// <param name="SortBy">The sort by supplied to this operation.</param>
/// <param name="SortOrder">The sort order supplied to this operation.</param>
public sealed record GetCategoryQuery(string Slug, int Page, int PageSize, string? Sort = null, string? SortBy = null, string? SortOrder = null) : ICacheableRequest<CategoryDetailEnvelope>
{
    /// <summary>Gets the canonical sort.</summary>
    public string? CanonicalSort => this.Sort is null && this.SortBy is null && this.SortOrder is null ? null : RecipeSort.Resolve(this.Sort, this.SortBy, this.SortOrder);

    /// <summary>Gets the cache key.</summary>
    public string CacheKey => PublicCacheKeys.Category(this.Slug, this.Page, this.PageSize, this.CanonicalSort);

    /// <summary>Gets the cache duration.</summary>
    public TimeSpan CacheDuration => TimeSpan.FromMinutes(30);

    /// <summary>Gets the cache tags.</summary>
    public IReadOnlyCollection<string> CacheTags => PublicCacheKeys.CategoryTags;
}
