// Culinary Blog. Authorship is recorded in the repository history.

using CulinaryBlog.Application.Content;
using MediatR;

namespace CulinaryBlog.Application.Auth;

/// <summary>Handles reads of public profile avatar images.</summary>
internal sealed class OpenProfileAvatarQueryHandler(IAuthService service) : IRequestHandler<OpenProfileAvatarQuery, MediaFileDto>
{
    /// <summary>Reads the currently selected avatar.</summary>
    public Task<MediaFileDto> Handle(OpenProfileAvatarQuery request, CancellationToken cancellationToken) =>
        service.OpenProfileAvatarAsync(request.UserId, request.FileName, cancellationToken);
}
