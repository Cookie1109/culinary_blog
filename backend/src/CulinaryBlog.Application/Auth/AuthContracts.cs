namespace CulinaryBlog.Application.Auth;

public sealed record RegisterRequest(string DisplayName, string Email, string Password);

public sealed record LoginRequest(string Email, string Password);

public sealed record GoogleLoginRequest(string IdToken);

public sealed record RefreshTokenRequest(string RefreshToken);

public sealed record ChangePasswordRequest(string CurrentPassword, string NewPassword);

public sealed record ForgotPasswordRequest(string Email);

public sealed record ResetPasswordRequest(string Email, string Token, string NewPassword);

public interface IPasswordResetEmailSender
{
    bool IsEnabled { get; }

    Task SendAsync(string email, string token, CancellationToken cancellationToken);
}

public sealed record UpdateProfileRequest(
    string? DisplayName,
    string? AvatarUrl,
    string? Bio,
    bool UpdateDisplayName = false,
    bool UpdateAvatarUrl = false,
    bool UpdateBio = false);

public sealed record UserDto(
    string Id,
    string Email,
    string DisplayName,
    string? AvatarUrl,
    string? Bio,
    IReadOnlyCollection<string> Roles,
    bool EmailConfirmed,
    bool IsActive,
    DateTimeOffset CreatedAt);

public sealed record AuthSessionDto(
    string AccessToken,
    string RefreshToken,
    DateTimeOffset ExpiresAt,
    UserDto User);

public sealed record DataEnvelope<T>(T Data);

public interface IAuthService
{
    Task<AuthSessionDto> RegisterAsync(
        RegisterRequest request,
        string? ipAddress,
        string? userAgent,
        CancellationToken cancellationToken);

    Task<AuthSessionDto> LoginAsync(
        LoginRequest request,
        string? ipAddress,
        string? userAgent,
        CancellationToken cancellationToken);

    Task<AuthSessionDto> LoginWithGoogleAsync(
        string idToken,
        string? ipAddress,
        string? userAgent,
        CancellationToken cancellationToken);

    Task<AuthSessionDto> RefreshAsync(
        string refreshToken,
        string? ipAddress,
        string? userAgent,
        CancellationToken cancellationToken);

    Task LogoutAsync(string refreshToken, string? ipAddress, CancellationToken cancellationToken);

    Task ChangePasswordAsync(string userId, ChangePasswordRequest request, CancellationToken cancellationToken);

    Task ForgotPasswordAsync(ForgotPasswordRequest request, CancellationToken cancellationToken);

    Task ResetPasswordAsync(ResetPasswordRequest request, CancellationToken cancellationToken);

    Task<UserDto> GetCurrentUserAsync(string userId, CancellationToken cancellationToken);

    Task<UserDto> UpdateCurrentUserAsync(
        string userId,
        UpdateProfileRequest request,
        CancellationToken cancellationToken);
}
