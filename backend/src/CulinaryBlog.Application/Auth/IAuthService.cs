// Culinary Blog. Authorship is recorded in the repository history.

using CulinaryBlog.Application.Content;

namespace CulinaryBlog.Application.Auth;

/// <summary>Defines the auth service contract.</summary>
public interface IAuthService
{
    /// <summary>Registers the i auth service.</summary>
    /// <param name="request">The request supplied to this operation.</param>
    /// <param name="ipAddress">The ip address supplied to this operation.</param>
    /// <param name="userAgent">The user agent supplied to this operation.</param>
    /// <param name="cancellationToken">Cancels the operation when requested by the caller.</param>
    /// <returns>A task representing the operation and its result.</returns>
    Task<AuthSessionDto> RegisterAsync(
        RegisterRequest request,
        string? ipAddress,
        string? userAgent,
        CancellationToken cancellationToken);

    /// <summary>Performs login for the i auth service.</summary>
    /// <param name="request">The request supplied to this operation.</param>
    /// <param name="ipAddress">The ip address supplied to this operation.</param>
    /// <param name="userAgent">The user agent supplied to this operation.</param>
    /// <param name="cancellationToken">Cancels the operation when requested by the caller.</param>
    /// <returns>A task representing the operation and its result.</returns>
    Task<AuthSessionDto> LoginAsync(
        LoginRequest request,
        string? ipAddress,
        string? userAgent,
        CancellationToken cancellationToken);

    /// <summary>Performs login for with google.</summary>
    /// <param name="idToken">The id token supplied to this operation.</param>
    /// <param name="ipAddress">The ip address supplied to this operation.</param>
    /// <param name="userAgent">The user agent supplied to this operation.</param>
    /// <param name="cancellationToken">Cancels the operation when requested by the caller.</param>
    /// <returns>A task representing the operation and its result.</returns>
    Task<AuthSessionDto> LoginWithGoogleAsync(
        string idToken,
        string? ipAddress,
        string? userAgent,
        CancellationToken cancellationToken);

    /// <summary>Refreshes the i auth service.</summary>
    /// <param name="refreshToken">The opaque refresh token supplied by the caller.</param>
    /// <param name="ipAddress">The ip address supplied to this operation.</param>
    /// <param name="userAgent">The user agent supplied to this operation.</param>
    /// <param name="cancellationToken">Cancels the operation when requested by the caller.</param>
    /// <returns>A task representing the operation and its result.</returns>
    Task<AuthSessionDto> RefreshAsync(
        string refreshToken,
        string? ipAddress,
        string? userAgent,
        CancellationToken cancellationToken);

    /// <summary>Finds refresh token owner.</summary>
    /// <param name="refreshToken">The opaque refresh token supplied by the caller.</param>
    /// <param name="cancellationToken">Cancels the operation when requested by the caller.</param>
    /// <returns>A task representing the operation and its result.</returns>
    Task<string?> FindRefreshTokenOwnerAsync(string refreshToken, CancellationToken cancellationToken);

    /// <summary>Performs logout for the i auth service.</summary>
    /// <param name="refreshToken">The opaque refresh token supplied by the caller.</param>
    /// <param name="ipAddress">The ip address supplied to this operation.</param>
    /// <param name="cancellationToken">Cancels the operation when requested by the caller.</param>
    /// <returns>A task representing the operation and its result.</returns>
    Task LogoutAsync(string refreshToken, string? ipAddress, CancellationToken cancellationToken);

    /// <summary>Performs the change password operation.</summary>
    /// <param name="userId">The identifier of the user performing or owning the operation.</param>
    /// <param name="request">The request supplied to this operation.</param>
    /// <param name="cancellationToken">Cancels the operation when requested by the caller.</param>
    /// <returns>A task representing the operation and its result.</returns>
    Task ChangePasswordAsync(string userId, ChangePasswordRequest request, CancellationToken cancellationToken);

    /// <summary>Performs the forgot password operation.</summary>
    /// <param name="request">The request supplied to this operation.</param>
    /// <param name="cancellationToken">Cancels the operation when requested by the caller.</param>
    /// <returns>A task representing the operation and its result.</returns>
    Task ForgotPasswordAsync(ForgotPasswordRequest request, CancellationToken cancellationToken);

    /// <summary>Performs the reset password operation.</summary>
    /// <param name="request">The request supplied to this operation.</param>
    /// <param name="cancellationToken">Cancels the operation when requested by the caller.</param>
    /// <returns>A task representing the operation and its result.</returns>
    Task ResetPasswordAsync(ResetPasswordRequest request, CancellationToken cancellationToken);

    /// <summary>Gets current user.</summary>
    /// <param name="userId">The identifier of the user performing or owning the operation.</param>
    /// <param name="cancellationToken">Cancels the operation when requested by the caller.</param>
    /// <returns>A task representing the operation and its result.</returns>
    Task<UserDto> GetCurrentUserAsync(string userId, CancellationToken cancellationToken);

    /// <summary>Updates current user.</summary>
    /// <param name="userId">The identifier of the user performing or owning the operation.</param>
    /// <param name="request">The request supplied to this operation.</param>
    /// <param name="cancellationToken">Cancels the operation when requested by the caller.</param>
    /// <returns>A task representing the operation and its result.</returns>
    Task<UserDto> UpdateCurrentUserAsync(
        string userId,
        UpdateProfileRequest request,
        CancellationToken cancellationToken);

    /// <summary>Updates a profile with an uploaded avatar.</summary>
    /// <param name="userId">The authenticated account to update.</param>
    /// <param name="request">The accompanying profile fields.</param>
    /// <param name="content">The uploaded image stream.</param>
    /// <param name="length">The declared image length in bytes.</param>
    /// <param name="contentType">The declared image MIME type.</param>
    /// <param name="cancellationToken">Cancels the update operation.</param>
    /// <returns>The updated user profile.</returns>
    Task<UserDto> UpdateProfileAvatarAsync(
        string userId,
        UpdateProfileRequest request,
        Stream content,
        long length,
        string contentType,
        CancellationToken cancellationToken);

    /// <summary>Reads the current public avatar for an active account.</summary>
    /// <param name="userId">The account whose current avatar is requested.</param>
    /// <param name="fileName">The unique file name of the current avatar.</param>
    /// <param name="cancellationToken">Cancels the read operation.</param>
    /// <returns>The image stream and MIME type.</returns>
    Task<MediaFileDto> OpenProfileAvatarAsync(string userId, string fileName, CancellationToken cancellationToken);
}
