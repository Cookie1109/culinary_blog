// Culinary Blog. Authorship is recorded in the repository history.

using CulinaryBlog.Application.Abstractions.Persistence;
using CulinaryBlog.Application.Content;
using MediatR;

namespace CulinaryBlog.Application.Features.Recipes.Commands;

/// <summary>Represents the delete recipe command handler.</summary>
/// <param name="unitOfWork">The unit of work supplied to this operation.</param>
internal sealed class DeleteRecipeCommandHandler(IUnitOfWork unitOfWork) : IRequestHandler<DeleteRecipeCommand>
{
    /// <summary>Handles the delete recipe command handler.</summary>
    /// <param name="request">The request supplied to this operation.</param>
    /// <param name="cancellationToken">Cancels the operation when requested by the caller.</param>
    /// <returns>A task representing the operation and its result.</returns>
    public async Task Handle(DeleteRecipeCommand request, CancellationToken cancellationToken)
    {
        var recipe = await unitOfWork.Recipes.GetByIdAsync(request.Id, cancellationToken).ConfigureAwait(false)
            ?? throw new ContentProblemException(
                "RECIPE_NOT_FOUND", "Recipe was not found.", ContentProblemKind.NotFound);
        if (!request.IsAdmin && recipe.AuthorId != request.UserId)
        {
            throw new ContentProblemException(
                "FORBIDDEN", "You cannot edit this recipe.", ContentProblemKind.Forbidden);
        }

        if (recipe.Version != request.ExpectedVersion)
        {
            throw new ContentProblemException(
                "RECIPE_CONCURRENCY_CONFLICT",
                "The recipe version is stale.",
                ContentProblemKind.Conflict,
                recipe.Version);
        }

        recipe.Delete();
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }
}
