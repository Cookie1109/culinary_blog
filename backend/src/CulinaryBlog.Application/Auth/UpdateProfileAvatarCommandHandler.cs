// Culinary Blog. Authorship is recorded in the repository history.

using MediatR;

namespace CulinaryBlog.Application.Auth;

/// <summary>Handles profile avatar uploads.</summary>
internal sealed class UpdateProfileAvatarCommandHandler(IAuthService service) : IRequestHandler<UpdateProfileAvatarCommand, UserDto>
{
    /// <summary>Updates the profile using the uploaded image.</summary>
    public Task<UserDto> Handle(UpdateProfileAvatarCommand request, CancellationToken cancellationToken) =>
        service.UpdateProfileAvatarAsync(request.UserId, request.Body, request.Content, request.Length, request.ContentType, cancellationToken);
}
