// Culinary Blog. Authorship is recorded in the repository history.

using CulinaryBlog.Application.Content;
using MediatR;

namespace CulinaryBlog.Application.Auth;

/// <summary>Handles reads of public profile avatar images.</summary>
/// <param name="service">Provides access to the current public avatar.</param>
internal sealed class OpenProfileAvatarQueryHandler(IAuthService service) : IRequestHandler<OpenProfileAvatarQuery, MediaFileDto>
{
    /// <summary>Reads the currently selected avatar.</summary>
    /// <param name="request">The account and avatar file to read.</param>
    /// <param name="cancellationToken">Cancels the read operation.</param>
    /// <returns>The image stream and MIME type.</returns>
    public Task<MediaFileDto> Handle(OpenProfileAvatarQuery request, CancellationToken cancellationToken) =>
        service.OpenProfileAvatarAsync(request.UserId, request.FileName, cancellationToken);
}
