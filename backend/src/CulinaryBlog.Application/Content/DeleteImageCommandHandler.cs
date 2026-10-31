// Culinary Blog. Authorship is recorded in the repository history.

using CulinaryBlog.Application.Behaviors;
using CulinaryBlog.Application.Content;
using CulinaryBlog.Domain.Recipes;
using FluentValidation;
using MediatR;

namespace CulinaryBlog.Application.Content;

/// <summary>Represents the delete image command handler.</summary>
/// <param name="service">The service supplied to this operation.</param>
internal sealed class DeleteImageCommandHandler(IContentService service) : IRequestHandler<DeleteImageCommand, long>
{
    /// <summary>Handles the delete image command handler.</summary>
    /// <param name="request">The request supplied to this operation.</param>
    /// <param name="cancellationToken">Cancels the operation when requested by the caller.</param>
    /// <returns>A task representing the operation and its result.</returns>
    public Task<long> Handle(DeleteImageCommand request, CancellationToken cancellationToken) =>
        service.DeleteImageAsync(request.RecipeId, request.ImageId, request.UserId, request.IsAdmin, request.ExpectedVersion, cancellationToken);
}
