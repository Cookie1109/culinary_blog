// Culinary Blog. Authorship is recorded in the repository history.

using CulinaryBlog.Application.Behaviors;
using CulinaryBlog.Application.Content;
using CulinaryBlog.Domain.Recipes;
using FluentValidation;
using MediatR;

namespace CulinaryBlog.Application.Features.Recipes.Commands;

/// <summary>Represents the publish recipe command handler.</summary>
/// <param name="service">The service supplied to this operation.</param>
internal sealed class PublishRecipeCommandHandler(IContentService service) : IRequestHandler<PublishRecipeCommand, RecipeDto>
{
    /// <summary>Handles the publish recipe command handler.</summary>
    /// <param name="request">The request supplied to this operation.</param>
    /// <param name="cancellationToken">Cancels the operation when requested by the caller.</param>
    /// <returns>A task representing the operation and its result.</returns>
    public Task<RecipeDto> Handle(PublishRecipeCommand request, CancellationToken cancellationToken) =>
        service.PublishRecipeAsync(request.Id, request.UserId, request.IsAdmin, request.ExpectedVersion, cancellationToken);
}
