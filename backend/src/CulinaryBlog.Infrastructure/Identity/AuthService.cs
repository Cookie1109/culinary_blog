using CulinaryBlog.Application.Auth;
using CulinaryBlog.Infrastructure.Email;
using CulinaryBlog.Infrastructure.Persistence;
using FluentValidation;
using FluentValidation.Results;
using Google.Apis.Auth;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;

namespace CulinaryBlog.Infrastructure.Identity;

internal sealed class AuthService(
    UserManager<ApplicationUser> userManager,
    SignInManager<ApplicationUser> signInManager,
    AppDbContext dbContext,
    TokenService tokenService,
    IOptions<JwtOptions> jwtOptions,
    IConfiguration configuration,
    TimeProvider timeProvider) : IAuthService
{
    private const string AuthorRole = "Author";

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
            Id = Guid.NewGuid(),
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

        var session = await CreateSessionAsync(
            user,
            Guid.NewGuid(),
            ipAddress,
            userAgent,
            cancellationToken).ConfigureAwait(false);

        dbContext.WelcomeEmailOutbox.Add(new WelcomeEmailOutbox
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            Recipient = user.Email!,
            DisplayName = user.DisplayName,
            Attempts = 0,
            CreatedAt = now,
            NextAttemptAt = now,
        });
        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
        return session;
    }

    public async Task<AuthSessionDto> LoginAsync(
        LoginRequest request,
        string? ipAddress,
        string? userAgent,
        CancellationToken cancellationToken)
    {
        var user = await userManager.FindByEmailAsync(request.Email.Trim()).ConfigureAwait(false);
        if (user is null)
        {
            throw InvalidCredentials();
        }

        if (!user.IsActive)
        {
            throw new AuthProblemException("AUTH_ACCOUNT_DISABLED", "This account is disabled.");
        }

        if (await userManager.IsLockedOutAsync(user).ConfigureAwait(false))
        {
            throw new AuthProblemException("AUTH_ACCOUNT_LOCKED", "This account is temporarily locked.");
        }

        var result = await signInManager.CheckPasswordSignInAsync(user, request.Password, lockoutOnFailure: true)
            .ConfigureAwait(false);
        if (result.IsLockedOut)
        {
            throw new AuthProblemException("AUTH_ACCOUNT_LOCKED", "This account is temporarily locked.");
        }

        if (!result.Succeeded)
        {
            throw InvalidCredentials();
        }

        return await CreateSessionAsync(
            user,
            Guid.NewGuid(),
            ipAddress,
            userAgent,
            cancellationToken).ConfigureAwait(false);
    }

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
                    Id = Guid.NewGuid(),
                    UserName = payload.Email,
                    Email = payload.Email,
                    EmailConfirmed = true,
                    DisplayName = displayName[..Math.Min(displayName.Length, 100)],
                    IsActive = true,
                    CreatedAt = timeProvider.GetUtcNow(),
                };
                ThrowIfIdentityFailed(await userManager.CreateAsync(user).ConfigureAwait(false));
                ThrowIfIdentityFailed(await userManager.AddToRoleAsync(user, AuthorRole).ConfigureAwait(false));
            }

            ThrowIfIdentityFailed(await userManager.AddLoginAsync(
                user, new UserLoginInfo("Google", payload.Subject, "Google")).ConfigureAwait(false));
        }

        if (!user.IsActive)
        {
            throw new AuthProblemException("AUTH_ACCOUNT_DISABLED", "This account is disabled.");
        }

        var session = await CreateSessionAsync(
            user, Guid.NewGuid(), ipAddress, userAgent, cancellationToken).ConfigureAwait(false);
        await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
        return session;
    }

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
            await RevokeFamilyAsync(storedToken.FamilyId, now, ipAddress, cancellationToken).ConfigureAwait(false);
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

        if (!user.IsActive)
        {
            await RevokeFamilyAsync(storedToken.FamilyId, now, ipAddress, cancellationToken).ConfigureAwait(false);
            await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
            throw new AuthProblemException("AUTH_ACCOUNT_DISABLED", "This account is disabled.");
        }

        var nextRawToken = TokenService.CreateRefreshToken();
        var nextHash = TokenService.HashRefreshToken(nextRawToken);
        var claimed = await dbContext.RefreshTokens
            .Where(token => token.Id == storedToken.Id && token.RevokedAt == null)
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(token => token.RevokedAt, now)
                    .SetProperty(token => token.RevokedByIp, NormalizeAuditValue(ipAddress, 64))
                    .SetProperty(token => token.ReplacedByTokenHash, nextHash),
                cancellationToken)
            .ConfigureAwait(false);

        if (claimed == 0)
        {
            await RevokeFamilyAsync(storedToken.FamilyId, now, ipAddress, cancellationToken).ConfigureAwait(false);
            await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
            throw new AuthProblemException("AUTH_REFRESH_TOKEN_REVOKED", "The refresh token has already been used.");
        }

        var session = await CreateSessionAsync(
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

    public async Task LogoutAsync(string refreshToken, string? ipAddress, CancellationToken cancellationToken)
    {
        var tokenHash = TokenService.HashRefreshToken(refreshToken);
        var now = timeProvider.GetUtcNow();
        await dbContext.RefreshTokens
            .Where(token => token.TokenHash == tokenHash && token.RevokedAt == null)
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(token => token.RevokedAt, now)
                    .SetProperty(token => token.RevokedByIp, NormalizeAuditValue(ipAddress, 64)),
                cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<UserDto> GetCurrentUserAsync(string userId, CancellationToken cancellationToken)
    {
        var user = await FindActiveUserAsync(userId, cancellationToken).ConfigureAwait(false);
        return await ToDtoAsync(user).ConfigureAwait(false);
    }

    public async Task<UserDto> UpdateCurrentUserAsync(
        string userId,
        UpdateProfileRequest request,
        CancellationToken cancellationToken)
    {
        var user = await FindActiveUserAsync(userId, cancellationToken).ConfigureAwait(false);
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
        return await ToDtoAsync(user).ConfigureAwait(false);
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
            CreatedByIp = NormalizeAuditValue(ipAddress, 64),
            UserAgent = NormalizeAuditValue(userAgent, 512),
        });
        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return new AuthSessionDto(accessToken.Token, rawToken, accessToken.ExpiresAt, await ToDtoAsync(user).ConfigureAwait(false));
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
        if (!Guid.TryParse(userId, out var id))
        {
            throw new AuthProblemException("USER_NOT_FOUND", "The current user was not found.");
        }

        var user = await userManager.Users.SingleOrDefaultAsync(user => user.Id == id, cancellationToken)
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
                    .SetProperty(token => token.RevokedByIp, NormalizeAuditValue(ipAddress, 64)),
                cancellationToken)
            .ConfigureAwait(false);
    }

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
}
