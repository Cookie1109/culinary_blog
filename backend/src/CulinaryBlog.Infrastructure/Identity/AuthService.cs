// Culinary Blog. Authorship is recorded in the repository history.

using System.Diagnostics;
using CulinaryBlog.Application.Auth;
using CulinaryBlog.Infrastructure.Email;
using CulinaryBlog.Infrastructure.Persistence;
using FluentValidation;
using FluentValidation.Results;
using Google.Apis.Auth;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CulinaryBlog.Infrastructure.Identity;

/// <summary>Represents the auth service.</summary>
/// <param name="userManager">The user manager supplied to this operation.</param>
/// <param name="signInManager">The sign in manager supplied to this operation.</param>
/// <param name="dbContext">The db context supplied to this operation.</param>
/// <param name="tokenService">The token service supplied to this operation.</param>
/// <param name="jwtOptions">The jwt options supplied to this operation.</param>
/// <param name="configuration">The configuration supplied to this operation.</param>
/// <param name="passwordResetEmailSender">The password reset email sender supplied to this operation.</param>
/// <param name="timeProvider">The time provider supplied to this operation.</param>
/// <param name="logger">The logger supplied to this operation.</param>
internal sealed class AuthService(
    UserManager<ApplicationUser> userManager,
    SignInManager<ApplicationUser> signInManager,
    AppDbContext dbContext,
    TokenService tokenService,
    IOptions<JwtOptions> jwtOptions,
    IConfiguration configuration,
    IPasswordResetEmailSender passwordResetEmailSender,
    TimeProvider timeProvider,
    ILogger<AuthService> logger) : IAuthService
{
    private const string AuthorRole = "Author";
    private static readonly Action<ILogger, Guid, string, Exception?> RefreshReuse = LoggerMessage.Define<Guid, string>(
        LogLevel.Warning,
        new EventId(4401, nameof(RefreshReuse)),
        "Refresh token reuse detected for family {FamilyId} and user {UserId}; revoking family");

    /// <summary>Registers the auth service.</summary>
    /// <param name="request">The request supplied to this operation.</param>
    /// <param name="ipAddress">The ip address supplied to this operation.</param>
    /// <param name="userAgent">The user agent supplied to this operation.</param>
    /// <param name="cancellationToken">Cancels the operation when requested by the caller.</param>
    /// <returns>A task representing the operation and its result.</returns>
    public async Task<AuthSessionDto> RegisterAsync(
        RegisterRequest request,
        string? ipAddress,
        string? userAgent,
        CancellationToken cancellationToken)
    {
        var normalizedEmail = userManager.NormalizeEmail(request.Email.Trim());
        if (await userManager.Users.AnyAsync(
                user => user.NormalizedEmail == normalizedEmail,
                cancellationToken).ConfigureAwait(false))
        {
            throw new AuthProblemException("AUTH_EMAIL_EXISTS", "An account with this email already exists.");
        }

        await using var transaction = await dbContext.Database
            .BeginTransactionAsync(cancellationToken)
            .ConfigureAwait(false);

        var now = timeProvider.GetUtcNow();
        var user = new ApplicationUser
        {
            Id = Guid.NewGuid().ToString(),
            UserName = request.Email.Trim(),
            Email = request.Email.Trim(),
            DisplayName = request.DisplayName.Trim(),
            IsActive = true,
            CreatedAt = now,
        };

        var createResult = await userManager.CreateAsync(user, request.Password).ConfigureAwait(false);
        ThrowIfIdentityFailed(createResult);

        var roleResult = await userManager.AddToRoleAsync(user, AuthorRole).ConfigureAwait(false);
        ThrowIfIdentityFailed(roleResult);

        var session = await this.CreateSessionAsync(
            user,
            Guid.NewGuid(),
            ipAddress,
            userAgent,
            cancellationToken).ConfigureAwait(false);

        dbContext.WelcomeEmailOutbox.Add(new WelcomeEmailOutbox
        {
            TraceParent = Activity.Current?.Id,
            CorrelationId = Activity.Current?.GetBaggageItem("correlation.id"),
            Id = Guid.NewGuid(),
            UserId = user.Id,
            Recipient = user.Email,
            DisplayName = user.DisplayName,
            Attempts = 0,
            CreatedAt = now,
            NextAttemptAt = now,
        });
        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
        return session;
    }

    /// <summary>Performs login for the auth service.</summary>
    /// <param name="request">The request supplied to this operation.</param>
    /// <param name="ipAddress">The ip address supplied to this operation.</param>
    /// <param name="userAgent">The user agent supplied to this operation.</param>
    /// <param name="cancellationToken">Cancels the operation when requested by the caller.</param>
    /// <returns>A task representing the operation and its result.</returns>
    public async Task<AuthSessionDto> LoginAsync(
        LoginRequest request,
        string? ipAddress,
        string? userAgent,
        CancellationToken cancellationToken)
    {
        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken).ConfigureAwait(false);
        var user = await userManager.FindByEmailAsync(request.Email.Trim()).ConfigureAwait(false);
        if (user is null)
        {
            throw InvalidCredentials();
        }

        await this.LockUserAsync(user, cancellationToken).ConfigureAwait(false);
        if (!user.IsActive)
        {
            throw new AuthProblemException("AUTH_ACCOUNT_DISABLED", "This account is disabled.");
        }

        if (await userManager.IsLockedOutAsync(user).ConfigureAwait(false))
        {
            throw new AuthProblemException("AUTH_ACCOUNT_LOCKED", $"This account is temporarily locked until {user.LockoutEnd:O}.");
        }

        var result = await signInManager.CheckPasswordSignInAsync(user, request.Password, lockoutOnFailure: true)
            .ConfigureAwait(false);
        if (!result.Succeeded)
        {
            // Failed-attempt counters must survive an unsuccessful login.
            await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
        }

        if (result.IsLockedOut)
        {
            throw new AuthProblemException("AUTH_ACCOUNT_LOCKED", $"This account is temporarily locked until {user.LockoutEnd:O}.");
        }

        if (!result.Succeeded)
        {
            throw InvalidCredentials();
        }

        var session = await this.CreateSessionAsync(
            user,
            Guid.NewGuid(),
            ipAddress,
            userAgent,
            cancellationToken).ConfigureAwait(false);
        await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
        return session;
    }

    /// <summary>Performs login for with google.</summary>
    /// <param name="idToken">The id token supplied to this operation.</param>
    /// <param name="ipAddress">The ip address supplied to this operation.</param>
    /// <param name="userAgent">The user agent supplied to this operation.</param>
    /// <param name="cancellationToken">Cancels the operation when requested by the caller.</param>
    /// <returns>A task representing the operation and its result.</returns>
    public async Task<AuthSessionDto> LoginWithGoogleAsync(
        string idToken,
        string? ipAddress,
        string? userAgent,
        CancellationToken cancellationToken)
    {
        var clientId = configuration["Google:ClientId"];
        if (string.IsNullOrWhiteSpace(clientId))
        {
            throw new AuthProblemException("AUTH_GOOGLE_UNAVAILABLE", "Google sign-in is not configured.");
        }

        GoogleJsonWebSignature.Payload payload;
        try
        {
            payload = await GoogleJsonWebSignature.ValidateAsync(
                idToken,
                new GoogleJsonWebSignature.ValidationSettings { Audience = [clientId] }).ConfigureAwait(false);
        }
        catch (InvalidJwtException)
        {
            throw new AuthProblemException("AUTH_GOOGLE_TOKEN_INVALID", "The Google ID token is invalid.");
        }

        if (!payload.EmailVerified || string.IsNullOrWhiteSpace(payload.Email))
        {
            throw new AuthProblemException("AUTH_GOOGLE_EMAIL_UNVERIFIED", "A verified Google email is required.");
        }

        if (string.IsNullOrWhiteSpace(payload.Subject) || payload.Email.Length > 256)
        {
            throw new AuthProblemException("AUTH_GOOGLE_TOKEN_INVALID", "The Google ID token is invalid.");
        }

        await using var transaction = await dbContext.Database
            .BeginTransactionAsync(cancellationToken).ConfigureAwait(false);
        var user = await userManager.FindByLoginAsync("Google", payload.Subject).ConfigureAwait(false);
        if (user is null)
        {
            user = await userManager.FindByEmailAsync(payload.Email).ConfigureAwait(false);
            if (user is not null)
            {
                var authoritativeEmail = payload.Email.EndsWith("@gmail.com", StringComparison.OrdinalIgnoreCase)
                    || !string.IsNullOrWhiteSpace(payload.HostedDomain);
                var googleLogins = await userManager.GetLoginsAsync(user).ConfigureAwait(false);
                if (!authoritativeEmail || googleLogins.Any(login => login.LoginProvider == "Google"))
                {
                    throw new AuthProblemException("AUTH_EXTERNAL_ACCOUNT_CONFLICT", "This email belongs to another account.");
                }
            }
            else
            {
                var displayName = string.IsNullOrWhiteSpace(payload.Name)
                    ? payload.Email.Split('@')[0]
                    : payload.Name.Trim();
                user = new ApplicationUser
                {
                    Id = Guid.NewGuid().ToString(),
                    UserName = payload.Email,
                    Email = payload.Email,
                    EmailConfirmed = true,
                    DisplayName = displayName[..Math.Min(displayName.Length, 100)],
                    AvatarUrl = Uri.TryCreate(payload.Picture, UriKind.Absolute, out var picture) && picture.Scheme == Uri.UriSchemeHttps ? picture.AbsoluteUri : null,
                    IsActive = true,
                    CreatedAt = timeProvider.GetUtcNow(),
                };
                ThrowIfIdentityFailed(await userManager.CreateAsync(user).ConfigureAwait(false));
                ThrowIfIdentityFailed(await userManager.AddToRoleAsync(user, AuthorRole).ConfigureAwait(false));
            }

            ThrowIfIdentityFailed(await userManager.AddLoginAsync(
                user, new UserLoginInfo("Google", payload.Subject, "Google")).ConfigureAwait(false));
        }

        await this.LockUserAsync(user, cancellationToken).ConfigureAwait(false);
        if (!user.IsActive)
        {
            throw new AuthProblemException("AUTH_ACCOUNT_DISABLED", "This account is disabled.");
        }

        var session = await this.CreateSessionAsync(
            user, Guid.NewGuid(), ipAddress, userAgent, cancellationToken).ConfigureAwait(false);
        await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
        return session;
    }

    /// <summary>Refreshes the auth service.</summary>
    /// <param name="refreshToken">The opaque refresh token supplied by the caller.</param>
    /// <param name="ipAddress">The ip address supplied to this operation.</param>
    /// <param name="userAgent">The user agent supplied to this operation.</param>
    /// <param name="cancellationToken">Cancels the operation when requested by the caller.</param>
    /// <returns>A task representing the operation and its result.</returns>
    public async Task<AuthSessionDto> RefreshAsync(
        string refreshToken,
        string? ipAddress,
        string? userAgent,
        CancellationToken cancellationToken)
    {
        var tokenHash = TokenService.HashRefreshToken(refreshToken);
        await using var transaction = await dbContext.Database
            .BeginTransactionAsync(cancellationToken)
            .ConfigureAwait(false);

        var storedToken = await dbContext.RefreshTokens
            .AsNoTracking()
            .SingleOrDefaultAsync(token => token.TokenHash == tokenHash, cancellationToken)
            .ConfigureAwait(false);
        if (storedToken is null)
        {
            throw new AuthProblemException("AUTH_REFRESH_TOKEN_INVALID", "The refresh token is invalid.");
        }

        var now = timeProvider.GetUtcNow();
        if (storedToken.RevokedAt is not null)
        {
            RefreshReuse(logger, storedToken.FamilyId, storedToken.UserId, null);
            await this.RevokeFamilyAsync(storedToken.FamilyId, now, ipAddress, cancellationToken).ConfigureAwait(false);
            await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
            throw new AuthProblemException("AUTH_REFRESH_TOKEN_REVOKED", "The refresh token has been revoked.");
        }

        if (storedToken.ExpiresAt <= now)
        {
            throw new AuthProblemException("AUTH_REFRESH_TOKEN_EXPIRED", "The refresh token has expired.");
        }

        var user = await userManager.FindByIdAsync(storedToken.UserId.ToString()).ConfigureAwait(false);
        if (user is null)
        {
            throw new AuthProblemException("AUTH_REFRESH_TOKEN_INVALID", "The refresh token is invalid.");
        }

        await this.LockUserAsync(user, cancellationToken).ConfigureAwait(false);
        if (!user.IsActive || await userManager.IsLockedOutAsync(user).ConfigureAwait(false))
        {
            await this.RevokeFamilyAsync(storedToken.FamilyId, now, ipAddress, cancellationToken).ConfigureAwait(false);
            await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
            throw new AuthProblemException("AUTH_REFRESH_TOKEN_INVALID", "The session is no longer valid.");
        }

        var nextRawToken = TokenService.CreateRefreshToken();
        var nextHash = TokenService.HashRefreshToken(nextRawToken);
        var claimed = await dbContext.RefreshTokens
            .Where(token => token.Id == storedToken.Id && token.RevokedAt == null)
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(token => token.RevokedAt, now)
                    .SetProperty(token => token.RevokedByIp, NormalizeAuditValue(ipAddress, 45))
                    .SetProperty(token => token.ReplacedByTokenHash, nextHash),
                cancellationToken)
            .ConfigureAwait(false);

        if (claimed == 0)
        {
            RefreshReuse(logger, storedToken.FamilyId, storedToken.UserId, null);
            await this.RevokeFamilyAsync(storedToken.FamilyId, now, ipAddress, cancellationToken).ConfigureAwait(false);
            await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
            throw new AuthProblemException("AUTH_REFRESH_TOKEN_REVOKED", "The refresh token has already been used.");
        }

        var session = await this.CreateSessionAsync(
            user,
            storedToken.FamilyId,
            ipAddress,
            userAgent,
            cancellationToken,
            nextRawToken,
            nextHash).ConfigureAwait(false);
        await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
        return session;
    }

    /// <summary>Finds refresh token owner.</summary>
    /// <param name="refreshToken">The opaque refresh token supplied by the caller.</param>
    /// <param name="cancellationToken">Cancels the operation when requested by the caller.</param>
    /// <returns>A task representing the operation and its result.</returns>
    public async Task<string?> FindRefreshTokenOwnerAsync(string refreshToken, CancellationToken cancellationToken)
    {
        var tokenHash = TokenService.HashRefreshToken(refreshToken);
        var owner = await dbContext.RefreshTokens.AsNoTracking()
            .Where(token => token.TokenHash == tokenHash)
            .Select(token => token.UserId)
            .SingleOrDefaultAsync(cancellationToken).ConfigureAwait(false);
        return owner?.ToString();
    }

    /// <summary>Performs logout for the auth service.</summary>
    /// <param name="refreshToken">The opaque refresh token supplied by the caller.</param>
    /// <param name="ipAddress">The ip address supplied to this operation.</param>
    /// <param name="cancellationToken">Cancels the operation when requested by the caller.</param>
    /// <returns>A task representing the operation and its result.</returns>
    public async Task LogoutAsync(string refreshToken, string? ipAddress, CancellationToken cancellationToken)
    {
        var tokenHash = TokenService.HashRefreshToken(refreshToken);
        var now = timeProvider.GetUtcNow();
        await dbContext.RefreshTokens
            .Where(token => token.TokenHash == tokenHash && token.RevokedAt == null)
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(token => token.RevokedAt, now)
                    .SetProperty(token => token.RevokedByIp, NormalizeAuditValue(ipAddress, 45)),
                cancellationToken)
            .ConfigureAwait(false);
    }

    /// <summary>Performs the change password operation.</summary>
    /// <param name="userId">The identifier of the user performing or owning the operation.</param>
    /// <param name="request">The request supplied to this operation.</param>
    /// <param name="cancellationToken">Cancels the operation when requested by the caller.</param>
    /// <returns>A task representing the operation and its result.</returns>
    public async Task ChangePasswordAsync(
        string userId, ChangePasswordRequest request, CancellationToken cancellationToken)
    {
        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken).ConfigureAwait(false);
        var user = await this.FindActiveUserAsync(userId, cancellationToken).ConfigureAwait(false);
        var result = await userManager.ChangePasswordAsync(user, request.CurrentPassword, request.NewPassword).ConfigureAwait(false);
        if (result.Errors.Any(error => error.Code == "PasswordMismatch"))
        {
            throw new AuthProblemException("AUTH_CURRENT_PASSWORD_INVALID", "The current password is incorrect.");
        }

        ThrowIfIdentityFailed(result);
        await this.RevokeUserSessionsAsync(user.Id, cancellationToken).ConfigureAwait(false);
        await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Performs the forgot password operation.</summary>
    /// <param name="request">The request supplied to this operation.</param>
    /// <param name="cancellationToken">Cancels the operation when requested by the caller.</param>
    /// <returns>A task representing the operation and its result.</returns>
    public async Task ForgotPasswordAsync(ForgotPasswordRequest request, CancellationToken cancellationToken)
    {
        if (!passwordResetEmailSender.IsEnabled)
        {
            throw new AuthProblemException("AUTH_EMAIL_UNAVAILABLE", "Password recovery email is not configured.");
        }

        var user = await userManager.FindByEmailAsync(request.Email.Trim()).ConfigureAwait(false);

        // Keep the same response for unknown, disabled and external-only accounts.
        if (user?.IsActive != true || !await userManager.HasPasswordAsync(user).ConfigureAwait(false))
        {
            return;
        }

        var token = await userManager.GeneratePasswordResetTokenAsync(user).ConfigureAwait(false);
        await passwordResetEmailSender.SendAsync(user.Email!, token, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Performs the reset password operation.</summary>
    /// <param name="request">The request supplied to this operation.</param>
    /// <param name="cancellationToken">Cancels the operation when requested by the caller.</param>
    /// <returns>A task representing the operation and its result.</returns>
    public async Task ResetPasswordAsync(ResetPasswordRequest request, CancellationToken cancellationToken)
    {
        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken).ConfigureAwait(false);
        var user = await userManager.FindByEmailAsync(request.Email.Trim()).ConfigureAwait(false);
        if (user?.IsActive != true || !await userManager.HasPasswordAsync(user).ConfigureAwait(false))
        {
            throw InvalidResetToken();
        }

        var result = await userManager.ResetPasswordAsync(user, request.Token, request.NewPassword).ConfigureAwait(false);
        if (result.Errors.Any(error => error.Code is "InvalidToken" or "ConcurrencyFailure"))
        {
            throw InvalidResetToken();
        }

        ThrowIfIdentityFailed(result);
        await this.RevokeUserSessionsAsync(user.Id, cancellationToken).ConfigureAwait(false);
        await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Gets current user.</summary>
    /// <param name="userId">The identifier of the user performing or owning the operation.</param>
    /// <param name="cancellationToken">Cancels the operation when requested by the caller.</param>
    /// <returns>A task representing the operation and its result.</returns>
    public async Task<UserDto> GetCurrentUserAsync(string userId, CancellationToken cancellationToken)
    {
        var user = await this.FindActiveUserAsync(userId, cancellationToken).ConfigureAwait(false);
        return await this.ToDtoAsync(user).ConfigureAwait(false);
    }

    /// <summary>Updates current user.</summary>
    /// <param name="userId">The identifier of the user performing or owning the operation.</param>
    /// <param name="request">The request supplied to this operation.</param>
    /// <param name="cancellationToken">Cancels the operation when requested by the caller.</param>
    /// <returns>A task representing the operation and its result.</returns>
    public async Task<UserDto> UpdateCurrentUserAsync(
        string userId,
        UpdateProfileRequest request,
        CancellationToken cancellationToken)
    {
        var user = await this.FindActiveUserAsync(userId, cancellationToken).ConfigureAwait(false);
        if (request.UpdateDisplayName)
        {
            user.DisplayName = request.DisplayName!.Trim();
        }

        if (request.UpdateAvatarUrl)
        {
            user.AvatarUrl = string.IsNullOrWhiteSpace(request.AvatarUrl) ? null : request.AvatarUrl.Trim();
        }

        if (request.UpdateBio)
        {
            user.Bio = string.IsNullOrWhiteSpace(request.Bio) ? null : request.Bio.Trim();
        }

        var result = await userManager.UpdateAsync(user).ConfigureAwait(false);
        ThrowIfIdentityFailed(result);
        return await this.ToDtoAsync(user).ConfigureAwait(false);
    }

    private static AuthProblemException InvalidResetToken() =>
        new("AUTH_RESET_TOKEN_INVALID", "The password reset link is invalid or has expired.");

    private static AuthProblemException InvalidCredentials()
    {
        return new AuthProblemException("AUTH_INVALID_CREDENTIALS", "The email or password is invalid.");
    }

    private static void ThrowIfIdentityFailed(IdentityResult result)
    {
        if (result.Succeeded)
        {
            return;
        }

        if (result.Errors.Any(error => error.Code is "DuplicateEmail" or "DuplicateUserName"))
        {
            throw new AuthProblemException("AUTH_EMAIL_EXISTS", "An account with this email already exists.");
        }

        throw new ValidationException(result.Errors.Select(error => new ValidationFailure("password", error.Description)));
    }

    private static string? NormalizeAuditValue(string? value, int maxLength)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value[..Math.Min(value.Length, maxLength)];
    }

    private Task<int> RevokeUserSessionsAsync(string userId, CancellationToken cancellationToken)
    {
        var now = timeProvider.GetUtcNow();
        return dbContext.RefreshTokens.Where(token => token.UserId == userId && token.RevokedAt == null)
            .ExecuteUpdateAsync(setters => setters.SetProperty(token => token.RevokedAt, now), cancellationToken);
    }

    private async Task LockUserAsync(ApplicationUser user, CancellationToken cancellationToken)
    {
        // Serialize session issuance with credential changes so refresh cannot escape revocation.
        await dbContext.Database.ExecuteSqlInterpolatedAsync(
            $"SELECT 1 FROM \"AspNetUsers\" WHERE \"Id\" = {user.Id} FOR UPDATE", cancellationToken).ConfigureAwait(false);
        await dbContext.Entry(user).ReloadAsync(cancellationToken).ConfigureAwait(false);
    }

    private async Task<AuthSessionDto> CreateSessionAsync(
        ApplicationUser user,
        Guid familyId,
        string? ipAddress,
        string? userAgent,
        CancellationToken cancellationToken,
        string? rawRefreshToken = null,
        string? refreshTokenHash = null)
    {
        var roles = await userManager.GetRolesAsync(user).ConfigureAwait(false);
        var accessToken = tokenService.CreateAccessToken(user, roles.ToArray());
        var rawToken = rawRefreshToken ?? TokenService.CreateRefreshToken();
        var tokenHash = refreshTokenHash ?? TokenService.HashRefreshToken(rawToken);
        var now = timeProvider.GetUtcNow();

        dbContext.RefreshTokens.Add(new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            TokenHash = tokenHash,
            FamilyId = familyId,
            CreatedAt = now,
            ExpiresAt = now.AddDays(jwtOptions.Value.RefreshTokenDays),
            CreatedByIp = NormalizeAuditValue(ipAddress, 45),
            UserAgentHash = string.IsNullOrWhiteSpace(userAgent) ? null : Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(userAgent))),
        });
        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return new AuthSessionDto(accessToken.Token, rawToken, accessToken.ExpiresAt, await this.ToDtoAsync(user).ConfigureAwait(false));
    }

    private async Task<UserDto> ToDtoAsync(ApplicationUser user)
    {
        var roles = await userManager.GetRolesAsync(user).ConfigureAwait(false);
        return new UserDto(
            user.Id.ToString(),
            user.Email!,
            user.DisplayName,
            user.AvatarUrl,
            user.Bio,
            roles.ToArray(),
            user.EmailConfirmed,
            user.IsActive,
            user.CreatedAt);
    }

    private async Task<ApplicationUser> FindActiveUserAsync(string userId, CancellationToken cancellationToken)
    {
        var user = await userManager.Users.SingleOrDefaultAsync(user => user.Id == userId, cancellationToken)
            .ConfigureAwait(false);
        if (user is null)
        {
            throw new AuthProblemException("USER_NOT_FOUND", "The current user was not found.");
        }

        if (!user.IsActive)
        {
            throw new AuthProblemException("AUTH_ACCOUNT_DISABLED", "This account is disabled.");
        }

        return user;
    }

    private async Task RevokeFamilyAsync(
        Guid familyId,
        DateTimeOffset revokedAt,
        string? ipAddress,
        CancellationToken cancellationToken)
    {
        await dbContext.RefreshTokens
            .Where(token => token.FamilyId == familyId && token.RevokedAt == null)
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(token => token.RevokedAt, revokedAt)
                    .SetProperty(token => token.RevokedByIp, NormalizeAuditValue(ipAddress, 45)),
                cancellationToken)
            .ConfigureAwait(false);
    }
}
