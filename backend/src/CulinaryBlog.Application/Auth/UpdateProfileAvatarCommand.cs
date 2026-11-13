// Culinary Blog. Authorship is recorded in the repository history.

using CulinaryBlog.Application.Behaviors;
using CulinaryBlog.Application.Content;
using MediatR;

namespace CulinaryBlog.Application.Auth;

/// <summary>Updates profile details and uploads the selected avatar.</summary>
public sealed record UpdateProfileAvatarCommand(
    string UserId,
    UpdateProfileRequest Body,
    Stream Content,
    long Length,
    string ContentType) : IRequest<UserDto>, IRequestWithBody<UpdateProfileRequest>, IContentMutation;
