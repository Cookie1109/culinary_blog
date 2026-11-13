// Culinary Blog. Authorship is recorded in the repository history.

using CulinaryBlog.Application.Content;
using MediatR;

namespace CulinaryBlog.Application.Auth;

/// <summary>Reads a user's current public avatar.</summary>
/// <param name="UserId">The account whose current avatar is requested.</param>
/// <param name="FileName">The unique file name of the current avatar.</param>
public sealed record OpenProfileAvatarQuery(string UserId, string FileName) : IRequest<MediaFileDto>;
