// Culinary Blog. Authorship is recorded in the repository history.

using CulinaryBlog.Application.Behaviors;
using CulinaryBlog.Application.Content;
using CulinaryBlog.Domain.Recipes;
using FluentValidation;
using MediatR;

namespace CulinaryBlog.Application.Content;

/// <summary>Represents the upload image command handler.</summary>
/// <param name="service">The service supplied to this operation.</param>
internal sealed class UploadImageCommandHandler(IContentService service) : IRequestHandler<UploadImageCommand, ChildMutationDto<RecipeImageDto>>
{
    /// <summary>Handles the upload image command handler.</summary>
    /// <param name="request">The request supplied to this operation.</param>
    /// <param name="cancellationToken">Cancels the operation when requested by the caller.</param>
    /// <returns>A task representing the operation and its result.</returns>
    public Task<ChildMutationDto<RecipeImageDto>> Handle(UploadImageCommand request, CancellationToken cancellationToken) =>
        service.UploadImageAsync(request.RecipeId, request.UserId, request.IsAdmin, request.ExpectedVersion, request.Content, request.Length, request.ContentType, request.AltText, request.IsPrimary, cancellationToken);
}
