// Culinary Blog. Authorship is recorded in the repository history.

namespace CulinaryBlog.Application.Auth;

/// <summary>Carries update profile request data.</summary>
/// <param name="DisplayName">The display name supplied to this operation.</param>
/// <param name="AvatarUrl">The avatar url supplied to this operation.</param>
/// <param name="Bio">The bio supplied to this operation.</param>
/// <param name="UpdateDisplayName">The update display name supplied to this operation.</param>
/// <param name="UpdateAvatarUrl">The update avatar url supplied to this operation.</param>
/// <param name="UpdateBio">The update bio supplied to this operation.</param>
public sealed record UpdateProfileRequest(
    string? DisplayName,
    string? AvatarUrl,
    string? Bio,
    bool UpdateDisplayName = false,
    bool UpdateAvatarUrl = false,
    bool UpdateBio = false);
