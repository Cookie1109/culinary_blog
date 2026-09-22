using CulinaryBlog.Application.Abstractions.Persistence;
using CulinaryBlog.Application.Content;
using MediatR;

namespace CulinaryBlog.Application.Features.Recipes.Commands;

public sealed record DeleteRecipeCommand(Guid Id, Guid UserId, bool IsAdmin, long ExpectedVersion) : IRequest;

internal sealed class DeleteRecipeCommandHandler(IUnitOfWork unitOfWork) : IRequestHandler<DeleteRecipeCommand>
{
    public async Task Handle(DeleteRecipeCommand command, CancellationToken cancellationToken)
    {
        var recipe = await unitOfWork.Recipes.GetByIdAsync(command.Id, cancellationToken).ConfigureAwait(false)
            ?? throw new ContentProblemException(
                "RECIPE_NOT_FOUND", "Recipe was not found.", ContentProblemKind.NotFound);
        if (!command.IsAdmin && recipe.AuthorId != command.UserId)
        {
            throw new ContentProblemException(
                "FORBIDDEN", "You cannot edit this recipe.", ContentProblemKind.Forbidden);
        }

        if (recipe.Version != command.ExpectedVersion)
        {
            throw new ContentProblemException(
                "RECIPE_CONCURRENCY_CONFLICT", "The recipe version is stale.",
                ContentProblemKind.Conflict, recipe.Version);
        }

        recipe.Delete();
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }
}
