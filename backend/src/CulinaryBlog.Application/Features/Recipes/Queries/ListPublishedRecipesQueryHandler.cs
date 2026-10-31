// Culinary Blog. Authorship is recorded in the repository history.

using CulinaryBlog.Application.Abstractions.Caching;
using CulinaryBlog.Application.Abstractions.Persistence;
using CulinaryBlog.Application.Content;
using CulinaryBlog.Domain.Recipes;
using FluentValidation;
using MediatR;

namespace CulinaryBlog.Application.Features.Recipes.Queries;

/// <summary>Represents the list published recipes query handler.</summary>
/// <param name="repository">The repository supplied to this operation.</param>
internal sealed class ListPublishedRecipesQueryHandler(IRecipeSearchRepository repository)
    : IRequestHandler<ListPublishedRecipesQuery, PageEnvelope<RecipeDto>>
{
    /// <summary>Handles the list published recipes query handler.</summary>
    /// <param name="request">The request supplied to this operation.</param>
    /// <param name="cancellationToken">Cancels the operation when requested by the caller.</param>
    /// <returns>A task representing the operation and its result.</returns>
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
            request.CanonicalSort,
            request.Page,
            request.PageSize,
            request.Category,
            request.MaxTime,
            cancellationToken);
}
