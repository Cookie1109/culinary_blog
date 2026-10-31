// Culinary Blog. Authorship is recorded in the repository history.

using CulinaryBlog.Application.Behaviors;
using CulinaryBlog.Application.Content;
using CulinaryBlog.Domain.Recipes;
using FluentValidation;
using MediatR;

namespace CulinaryBlog.Application.Features.Recipes.Commands;

/// <summary>Represents the create recipe command handler.</summary>
/// <param name="service">The service supplied to this operation.</param>
internal sealed class CreateRecipeCommandHandler(IContentService service) : IRequestHandler<CreateRecipeCommand, RecipeDto>
{
    /// <summary>Handles the create recipe command handler.</summary>
    /// <param name="request">The request supplied to this operation.</param>
    /// <param name="cancellationToken">Cancels the operation when requested by the caller.</param>
    /// <returns>A task representing the operation and its result.</returns>
    public Task<RecipeDto> Handle(CreateRecipeCommand request, CancellationToken cancellationToken) =>
        service.CreateRecipeAsync(request.UserId, request.Body, cancellationToken);
}
