// Culinary Blog. Authorship is recorded in the repository history.

using CulinaryBlog.Application.Behaviors;
using CulinaryBlog.Application.Content;
using CulinaryBlog.Domain.Recipes;
using FluentValidation;
using MediatR;

namespace CulinaryBlog.Application.Features.Recipes.Commands;

/// <summary>Represents the unarchive recipe command handler.</summary>
/// <param name="service">The service supplied to this operation.</param>
internal sealed class UnarchiveRecipeCommandHandler(IContentService service) : IRequestHandler<UnarchiveRecipeCommand, RecipeDto>
{
    /// <summary>Handles the unarchive recipe command handler.</summary>
    /// <param name="request">The request supplied to this operation.</param>
    /// <param name="cancellationToken">Cancels the operation when requested by the caller.</param>
    /// <returns>A task representing the operation and its result.</returns>
    public Task<RecipeDto> Handle(UnarchiveRecipeCommand request, CancellationToken cancellationToken) =>
        service.UnarchiveRecipeAsync(request.Id, request.UserId, request.IsAdmin, request.ExpectedVersion, cancellationToken);
}
