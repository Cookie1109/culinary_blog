// Culinary Blog. Authorship is recorded in the repository history.

using CulinaryBlog.Application.Behaviors;
using CulinaryBlog.Application.Content;
using CulinaryBlog.Domain.Recipes;
using FluentValidation;
using MediatR;

namespace CulinaryBlog.Application.Content;

/// <summary>Represents the update ingredient command handler.</summary>
/// <param name="service">The service supplied to this operation.</param>
internal sealed class UpdateIngredientCommandHandler(IContentService service) : IRequestHandler<UpdateIngredientCommand, ChildMutationDto<IngredientDto>>
{
    /// <summary>Handles the update ingredient command handler.</summary>
    /// <param name="request">The request supplied to this operation.</param>
    /// <param name="cancellationToken">Cancels the operation when requested by the caller.</param>
    /// <returns>A task representing the operation and its result.</returns>
    public Task<ChildMutationDto<IngredientDto>> Handle(UpdateIngredientCommand request, CancellationToken cancellationToken) =>
        service.UpdateIngredientAsync(request.RecipeId, request.IngredientId, request.UserId, request.IsAdmin, request.ExpectedVersion, request.Body, cancellationToken);
}
