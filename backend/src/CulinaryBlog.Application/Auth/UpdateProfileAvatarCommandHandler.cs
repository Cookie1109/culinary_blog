// Culinary Blog. Authorship is recorded in the repository history.

using MediatR;

namespace CulinaryBlog.Application.Auth;

/// <summary>Handles profile avatar uploads.</summary>
/// <param name="service">Updates the profile and stores the uploaded avatar.</param>
internal sealed class UpdateProfileAvatarCommandHandler(IAuthService service) : IRequestHandler<UpdateProfileAvatarCommand, UserDto>
{
    /// <summary>Updates the profile using the uploaded image.</summary>
    /// <param name="request">The profile and selected image.</param>
    /// <param name="cancellationToken">Cancels the update operation.</param>
    /// <returns>The updated user profile.</returns>
    public Task<UserDto> Handle(UpdateProfileAvatarCommand request, CancellationToken cancellationToken) =>
        service.UpdateProfileAvatarAsync(request.UserId, request.Body, request.Content, request.Length, request.ContentType, cancellationToken);
}
