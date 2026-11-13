// Culinary Blog. Authorship is recorded in the repository history.

using CulinaryBlog.Application.Abstractions.Caching;
using CulinaryBlog.Application.Auth;
using CulinaryBlog.Application.Behaviors;
using CulinaryBlog.Application.Content;
using CulinaryBlog.Domain.Recipes;
using FluentValidation;
using MediatR;

namespace CulinaryBlog.Application.Content;

/// <summary>Represents the get published recipe query handler.</summary>
/// <param name="service">The service supplied to this operation.</param>
internal sealed class GetPublishedRecipeQueryHandler(IContentService service) : IRequestHandler<GetPublishedRecipeQuery, RecipeDto>
{
    /// <summary>Handles the get published recipe query handler.</summary>
    /// <param name="request">The request supplied to this operation.</param>
    /// <param name="cancellationToken">Cancels the operation when requested by the caller.</param>
    /// <returns>A task representing the operation and its result.</returns>
    public Task<RecipeDto> Handle(GetPublishedRecipeQuery request, CancellationToken cancellationToken) =>
        service.GetPublishedRecipeAsync(request.Slug, cancellationToken);
}
