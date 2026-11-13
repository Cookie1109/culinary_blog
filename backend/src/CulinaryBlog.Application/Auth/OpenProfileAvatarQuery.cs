// Culinary Blog. Authorship is recorded in the repository history.

using CulinaryBlog.Application.Content;
using MediatR;

namespace CulinaryBlog.Application.Auth;

/// <summary>Reads a user's current public avatar.</summary>
public sealed record OpenProfileAvatarQuery(string UserId, string FileName) : IRequest<MediaFileDto>;
