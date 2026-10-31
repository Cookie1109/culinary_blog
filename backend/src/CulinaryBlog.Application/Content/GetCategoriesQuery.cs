// Culinary Blog. Authorship is recorded in the repository history.

using CulinaryBlog.Application.Abstractions.Caching;
using CulinaryBlog.Application.Behaviors;
using CulinaryBlog.Application.Content;
using CulinaryBlog.Domain.Recipes;
using FluentValidation;
using MediatR;

namespace CulinaryBlog.Application.Content;

/// <summary>Carries get categories query data.</summary>
public sealed record GetCategoriesQuery : ICacheableRequest<IReadOnlyCollection<CategoryDto>>
{
    /// <summary>Gets the cache key.</summary>
    public string CacheKey => PublicCacheKeys.Categories;

    /// <summary>Gets the cache duration.</summary>
    public TimeSpan CacheDuration => TimeSpan.FromMinutes(30);

    /// <summary>Gets the cache tags.</summary>
    public IReadOnlyCollection<string> CacheTags => PublicCacheKeys.CategoryTags;
}
