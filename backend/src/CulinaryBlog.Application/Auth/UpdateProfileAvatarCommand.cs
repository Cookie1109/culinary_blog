// Culinary Blog. Authorship is recorded in the repository history.

using CulinaryBlog.Application.Behaviors;
using CulinaryBlog.Application.Content;
using MediatR;

namespace CulinaryBlog.Application.Auth;

/// <summary>Updates profile details and uploads the selected avatar.</summary>
/// <param name="UserId">The authenticated account to update.</param>
/// <param name="Body">The accompanying profile fields.</param>
/// <param name="Content">The uploaded image stream.</param>
/// <param name="Length">The declared image length in bytes.</param>
/// <param name="ContentType">The declared image MIME type.</param>
public sealed record UpdateProfileAvatarCommand(
    string UserId,
    UpdateProfileRequest Body,
    Stream Content,
    long Length,
    string ContentType) : IRequest<UserDto>, IRequestWithBody<UpdateProfileRequest>, IContentMutation;
