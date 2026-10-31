// Culinary Blog. Authorship is recorded in the repository history.

using CulinaryBlog.Application.Abstractions.Caching;
using CulinaryBlog.Application.Abstractions.Persistence;
using CulinaryBlog.Application.Content;
using CulinaryBlog.Domain.Recipes;
using FluentValidation;
using MediatR;

namespace CulinaryBlog.Application.Features.Recipes.Queries;

/// <summary>Represents the search published recipes query handler.</summary>
/// <param name="repository">The repository supplied to this operation.</param>
internal sealed class SearchPublishedRecipesQueryHandler(IRecipeSearchRepository repository)
    : IRequestHandler<SearchPublishedRecipesQuery, PageEnvelope<RecipeDto>>
{
    /// <summary>Handles the search published recipes query handler.</summary>
    /// <param name="request">The request supplied to this operation.</param>
    /// <param name="cancellationToken">Cancels the operation when requested by the caller.</param>
    /// <returns>A task representing the operation and its result.</returns>
    public Task<PageEnvelope<RecipeDto>> Handle(
        SearchPublishedRecipesQuery request,
        CancellationToken cancellationToken) =>
        repository.SearchPublishedRecipesAsync(
            request.Search,
            request.Category,
            request.Difficulty,
            request.MaxTime,
            request.CanonicalSort,
            request.Page,
            request.PageSize,
            request.CategoryId,
            request.MaxCookTime,
            request.MinServings,
            request.MinPrepTime,
            request.MaxPrepTime,
            cancellationToken);
}
