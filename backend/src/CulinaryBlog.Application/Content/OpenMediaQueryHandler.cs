// Culinary Blog. Authorship is recorded in the repository history.

using CulinaryBlog.Application.Abstractions.Caching;
using CulinaryBlog.Application.Auth;
using CulinaryBlog.Application.Behaviors;
using CulinaryBlog.Application.Content;
using CulinaryBlog.Domain.Recipes;
using FluentValidation;
using MediatR;

namespace CulinaryBlog.Application.Content;

/// <summary>Represents the open media query handler.</summary>
/// <param name="service">The service supplied to this operation.</param>
/// <param name="currentActor">The current actor supplied to this operation.</param>
internal sealed class OpenMediaQueryHandler(IContentService service, IApplicationActor currentActor) : IRequestHandler<OpenMediaQuery, MediaFileDto>
{
    /// <summary>Handles the open media query handler.</summary>
    /// <param name="request">The request supplied to this operation.</param>
    /// <param name="cancellationToken">Cancels the operation when requested by the caller.</param>
    /// <returns>A task representing the operation and its result.</returns>
    public async Task<MediaFileDto> Handle(OpenMediaQuery request, CancellationToken cancellationToken)
    {
        var actor = await currentActor.GetAsync(cancellationToken).ConfigureAwait(false);
        var id = actor?.IsActive == true ? actor.Id : null;
        return await service.OpenMediaAsync(request.ImageId, request.Variant, id, id is not null && actor!.Roles.Contains("Admin"), cancellationToken).ConfigureAwait(false);
    }
}
