// Culinary Blog. Authorship is recorded in the repository history.

using CulinaryBlog.Application.Abstractions.Caching;
using CulinaryBlog.Application.Auth;
using CulinaryBlog.Application.Behaviors;
using CulinaryBlog.Application.Content;
using CulinaryBlog.Domain.Recipes;
using FluentValidation;
using MediatR;

namespace CulinaryBlog.Application.Content;

/// <summary>Carries get published recipe query data.</summary>
/// <param name="Slug">The slug supplied to this operation.</param>
public sealed record GetPublishedRecipeQuery(string Slug) : ICacheableRequest<RecipeDto>
{
    /// <summary>Gets the cache key.</summary>
    public string CacheKey => PublicCacheKeys.Recipe(this.Slug);

    /// <summary>Gets the cache duration.</summary>
    public TimeSpan CacheDuration => TimeSpan.FromMinutes(5);

    /// <summary>Gets the cache tags.</summary>
    public IReadOnlyCollection<string> CacheTags => PublicCacheKeys.RecipeTags;
}
