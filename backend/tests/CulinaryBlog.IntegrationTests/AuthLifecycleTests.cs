// Culinary Blog. Authorship is recorded in the repository history.

using System.Buffers.Binary;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using CulinaryBlog.Infrastructure.Identity;
using CulinaryBlog.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace CulinaryBlog.IntegrationTests;

/// <summary>Contains regression checks for auth lifecycle tests.</summary>
/// <param name="factory">The factory supplied to this operation.</param>
public sealed class AuthLifecycleTests(AuthApiFactory factory) : IClassFixture<AuthApiFactory>
{
    private readonly HttpClient sharedClient = factory.CreateClient();

    /// <summary>Verifies that profile avatar can be updated and cleared and invalid urls do not partially update profile.</summary>
    /// <returns>A task representing the operation and its result.</returns>
    [Fact]
    public async Task ProfileAvatarCanBeUpdatedAndClearedAndInvalidUrlsDoNotPartiallyUpdateProfile()
    {
        var registered = await this.RegisterAsync($"avatar-{Guid.NewGuid():N}@example.com");
        this.sharedClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", registered.AccessToken);
        using var invalid = await this.sharedClient.PatchAsJsonAsync("/api/v1/auth/me", new { displayName = "Must not persist", avatarUrl = "javascript:alert(1)" });
        Assert.Equal(HttpStatusCode.UnprocessableEntity, invalid.StatusCode);
        using var unchanged = await this.sharedClient.GetAsync("/api/v1/auth/me");
        Assert.Equal(registered.User.DisplayName, (await unchanged.Content.ReadFromJsonAsync<DataEnvelope<AuthUser>>())!.Data.DisplayName);

        using var updated = await this.sharedClient.PatchAsJsonAsync("/api/v1/auth/me", new { displayName = "Avatar Author", avatarUrl = "https://example.com/avatar.jpg", bio = "New biography" });
        Assert.Equal(HttpStatusCode.OK, updated.StatusCode);
        var user = (await updated.Content.ReadFromJsonAsync<DataEnvelope<AuthUser>>())!.Data;
        Assert.Equal("https://example.com/avatar.jpg", user.AvatarUrl);
        Assert.Equal("New biography", user.Bio);

        using var cleared = await this.sharedClient.PatchAsJsonAsync("/api/v1/auth/me", new { avatarUrl = (string?)null });
        Assert.Equal(HttpStatusCode.OK, cleared.StatusCode);
        var clearedUser = (await cleared.Content.ReadFromJsonAsync<DataEnvelope<AuthUser>>())!.Data;
        Assert.Null(clearedUser.AvatarUrl);
        Assert.Equal("Avatar Author", clearedUser.DisplayName);
        Assert.Equal("New biography", clearedUser.Bio);
    }

    /// <summary>Verifies that register profile refresh logout lifecycle is secure.</summary>
    /// <returns>A task representing the operation and its result.</returns>
    [Fact]
    public async Task RegisterProfileRefreshLogoutLifecycleIsSecure()
    {
        const string password = "Valid#Password1";
        using var registerResponse = await this.sharedClient.PostAsJsonAsync(
            "/api/v1/auth/register",
            new { displayName = "Integration Author", email = "author@example.com", password });
        var registered = await registerResponse.Content.ReadFromJsonAsync<DataEnvelope<AuthSession>>();

        Assert.Equal(HttpStatusCode.Created, registerResponse.StatusCode);
        Assert.NotNull(registered);
        Assert.Contains("Author", registered.Data.User.Roles);

        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var userId = registered.Data.User.Id;
            var user = await dbContext.Users.SingleAsync(item => item.Id == userId);
            var tokenHash = TokenService.HashRefreshToken(registered.Data.RefreshToken);
            var storedToken = await dbContext.RefreshTokens.SingleAsync(item => item.TokenHash == tokenHash);
            Assert.NotEqual(password, user.PasswordHash);
            var passwordHashPayload = Convert.FromBase64String(user.PasswordHash!);
            Assert.True(BinaryPrimitives.ReadUInt32BigEndian(passwordHashPayload.AsSpan(5, 4)) >= 100_000);
            Assert.NotEqual(registered.Data.RefreshToken, storedToken.TokenHash);
            Assert.Equal(tokenHash, storedToken.TokenHash);
            Assert.Single(dbContext.WelcomeEmailOutbox, item => item.UserId == userId);
        }

        this.sharedClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", registered.Data.AccessToken);
        using var profileResponse = await this.sharedClient.PatchAsJsonAsync(
            "/api/v1/auth/me",
            new { displayName = "Updated Author", bio = "A short bio" });
        var profile = await profileResponse.Content.ReadFromJsonAsync<DataEnvelope<AuthUser>>();
        Assert.Equal(HttpStatusCode.OK, profileResponse.StatusCode);
        Assert.Equal("Updated Author", profile?.Data.DisplayName);

        this.sharedClient.DefaultRequestHeaders.Authorization = null;
        using var refreshResponse = await this.sharedClient.PostAsJsonAsync(
            "/api/v1/auth/refresh",
            new { refreshToken = registered.Data.RefreshToken });
        var refreshed = await refreshResponse.Content.ReadFromJsonAsync<DataEnvelope<AuthSession>>();
        Assert.Equal(HttpStatusCode.OK, refreshResponse.StatusCode);
        Assert.NotNull(refreshed);
        Assert.NotEqual(registered.Data.RefreshToken, refreshed.Data.RefreshToken);

        using var replayResponse = await this.sharedClient.PostAsJsonAsync(
            "/api/v1/auth/refresh",
            new { refreshToken = registered.Data.RefreshToken });
        Assert.Equal(HttpStatusCode.Unauthorized, replayResponse.StatusCode);

        using var logoutResponse = await this.sharedClient.PostAsJsonAsync(
            "/api/v1/auth/logout",
            new { refreshToken = refreshed.Data.RefreshToken });
        using var repeatedLogoutResponse = await this.sharedClient.PostAsJsonAsync(
            "/api/v1/auth/logout",
            new { refreshToken = refreshed.Data.RefreshToken });
        Assert.Equal(HttpStatusCode.NoContent, logoutResponse.StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, repeatedLogoutResponse.StatusCode);

        using var afterLogoutResponse = await this.sharedClient.PostAsJsonAsync(
            "/api/v1/auth/refresh",
            new { refreshToken = refreshed.Data.RefreshToken });
        Assert.Equal(HttpStatusCode.Unauthorized, afterLogoutResponse.StatusCode);
    }

    /// <summary>Verifies that five invalid passwords lock the account without enumerating it.</summary>
    /// <returns>A task representing the operation and its result.</returns>
    [Fact]
    public async Task FiveInvalidPasswordsLockTheAccountWithoutEnumeratingIt()
    {
        await this.RegisterAsync("locked@example.com");
        for (var attempt = 1; attempt <= 5; attempt++)
        {
            using var response = await this.sharedClient.PostAsJsonAsync(
                "/api/v1/auth/login",
                new { email = "locked@example.com", password = "Wrong#Password1" });
            Assert.Equal(attempt == 5 ? HttpStatusCode.Locked : HttpStatusCode.Unauthorized, response.StatusCode);
        }

        using var unknownResponse = await this.sharedClient.PostAsJsonAsync(
            "/api/v1/auth/login",
            new { email = "unknown@example.com", password = "Wrong#Password1" });
        var problem = await unknownResponse.Content.ReadFromJsonAsync<Problem>();
        Assert.Equal(HttpStatusCode.Unauthorized, unknownResponse.StatusCode);
        Assert.Equal("AUTH_INVALID_CREDENTIALS", problem?.Code);

        using var locked = await this.sharedClient.PostAsJsonAsync("/api/v1/auth/login", new { email = "locked@example.com", password = "Valid#Password1" });
        Assert.Equal(HttpStatusCode.Locked, locked.StatusCode);
        Assert.Contains("locked until", await locked.Content.ReadAsStringAsync(), StringComparison.Ordinal);
        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var user = (await users.FindByEmailAsync("locked@example.com"))!;
            Assert.InRange(user.LockoutEnd!.Value - DateTimeOffset.UtcNow, TimeSpan.FromMinutes(14), TimeSpan.FromMinutes(15));
            user.LockoutEnd = DateTimeOffset.UtcNow.AddMinutes(-1);
            Assert.True((await users.UpdateAsync(user)).Succeeded);
        }

        using var unlocked = await this.sharedClient.PostAsJsonAsync("/api/v1/auth/login", new { email = "locked@example.com", password = "Valid#Password1" });
        Assert.Equal(HttpStatusCode.OK, unlocked.StatusCode);
    }

    /// <summary>Verifies that concurrent refresh produces at most one success and revokes the family.</summary>
    /// <returns>A task representing the operation and its result.</returns>
    [Fact]
    public async Task ConcurrentRefreshProducesAtMostOneSuccessAndRevokesTheFamily()
    {
        var registered = await this.RegisterAsync("concurrent@example.com");
        var first = this.sharedClient.PostAsJsonAsync(
            "/api/v1/auth/refresh",
            new { refreshToken = registered.RefreshToken });
        var second = this.sharedClient.PostAsJsonAsync(
            "/api/v1/auth/refresh",
            new { refreshToken = registered.RefreshToken });

        var responses = await Task.WhenAll(first, second);
        Assert.Single(responses, response => response.StatusCode == HttpStatusCode.OK);
        Assert.Single(responses, response => response.StatusCode == HttpStatusCode.Unauthorized);

        var successfulResponse = responses.Single(response => response.StatusCode == HttpStatusCode.OK);
        var rotated = await successfulResponse.Content.ReadFromJsonAsync<DataEnvelope<AuthSession>>();
        Assert.NotNull(rotated);
        using var familyResponse = await this.sharedClient.PostAsJsonAsync(
            "/api/v1/auth/refresh",
            new { refreshToken = rotated.Data.RefreshToken });
        Assert.Equal(HttpStatusCode.Unauthorized, familyResponse.StatusCode);
    }

    /// <summary>Verifies that expired refresh and tampered access tokens are rejected.</summary>
    /// <returns>A task representing the operation and its result.</returns>
    [Fact]
    public async Task ExpiredRefreshAndTamperedAccessTokensAreRejected()
    {
        var registered = await this.RegisterAsync("expired@example.com");
        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var tokenHash = TokenService.HashRefreshToken(registered.RefreshToken);
            var token = await dbContext.RefreshTokens.SingleAsync(item => item.TokenHash == tokenHash);
            token.ExpiresAt = DateTimeOffset.UtcNow.AddMinutes(-1);
            await dbContext.SaveChangesAsync();
        }

        using var expiredResponse = await this.sharedClient.PostAsJsonAsync(
            "/api/v1/auth/refresh",
            new { refreshToken = registered.RefreshToken });
        Assert.Equal(HttpStatusCode.Unauthorized, expiredResponse.StatusCode);

        this.sharedClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", registered.AccessToken + "tampered");
        using var tamperedResponse = await this.sharedClient.GetAsync("/api/v1/auth/me");
        this.sharedClient.DefaultRequestHeaders.Authorization = null;
        Assert.Equal(HttpStatusCode.Unauthorized, tamperedResponse.StatusCode);
    }

    /// <summary>Verifies that author and admin policies also require an active user.</summary>
    /// <returns>A task representing the operation and its result.</returns>
    [Fact]
    public async Task AuthorAndAdminPoliciesAlsoRequireAnActiveUser()
    {
        var registered = await this.RegisterAsync("policy@example.com");
        await using var scope = factory.Services.CreateAsyncScope();
        var authorization = scope.ServiceProvider.GetRequiredService<IAuthorizationService>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var user = await userManager.FindByEmailAsync("policy@example.com");
        Assert.NotNull(user);

        var principal = new ClaimsPrincipal(new ClaimsIdentity(
            [
                new Claim(ClaimTypes.NameIdentifier, registered.User.Id),
                new Claim(ClaimTypes.Role, "Author"),
            ],
            "test"));
        Assert.True((await authorization.AuthorizeAsync(principal, null, "AuthorPolicy")).Succeeded);
        Assert.False((await authorization.AuthorizeAsync(principal, null, "AdminPolicy")).Succeeded);

        user.IsActive = false;
        Assert.True((await userManager.UpdateAsync(user)).Succeeded);
        Assert.False((await authorization.AuthorizeAsync(principal, null, "AuthorPolicy")).Succeeded);
    }

    /// <summary>Verifies that logout checks bearer owner and rejects invalid signatures before revoking.</summary>
    /// <returns>A task representing the operation and its result.</returns>
    [Fact]
    public async Task LogoutChecksBearerOwnerAndRejectsInvalidSignaturesBeforeRevoking()
    {
        using var client = factory.CreateClient();
        var first = await this.RegisterAsync($"logout-a-{Guid.NewGuid():N}@example.test");
        var second = await this.RegisterAsync($"logout-b-{Guid.NewGuid():N}@example.test");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", first.AccessToken);
        using var forbidden = await client.PostAsJsonAsync("/api/v1/auth/logout", new { refreshToken = second.RefreshToken });
        Assert.Equal(HttpStatusCode.Forbidden, forbidden.StatusCode);
        Assert.Equal("FORBIDDEN", (await forbidden.Content.ReadFromJsonAsync<Problem>())!.Code);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", first.AccessToken + "tampered");
        using var invalid = await client.PostAsJsonAsync("/api/v1/auth/logout", new { refreshToken = second.RefreshToken });
        Assert.Equal(HttpStatusCode.Unauthorized, invalid.StatusCode);
        client.DefaultRequestHeaders.Authorization = null;
        using var refresh = await client.PostAsJsonAsync("/api/v1/auth/refresh", new { refreshToken = second.RefreshToken });
        Assert.Equal(HttpStatusCode.OK, refresh.StatusCode);
        var rotated = (await refresh.Content.ReadFromJsonAsync<DataEnvelope<AuthSession>>())!.Data;
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", rotated.AccessToken);
        for (var attempt = 0; attempt < 2; attempt++)
        {
            using var logout = await client.PostAsJsonAsync("/api/v1/auth/logout", new { refreshToken = rotated.RefreshToken });
            Assert.Equal(HttpStatusCode.NoContent, logout.StatusCode);
            Assert.Equal("no-store", logout.Headers.CacheControl?.ToString());
        }

        using var revoked = await client.PostAsJsonAsync("/api/v1/auth/refresh", new { refreshToken = rotated.RefreshToken });
        Assert.Equal(HttpStatusCode.Unauthorized, revoked.StatusCode);
    }

    /// <summary>Verifies that logout allows signed expired bearer but not tampered expired bearer.</summary>
    /// <returns>A task representing the operation and its result.</returns>
    [Fact]
    public async Task LogoutAllowsSignedExpiredBearerButNotTamperedExpiredBearer()
    {
        using var client = factory.CreateClient();
        var session = await this.RegisterAsync($"logout-expired-{Guid.NewGuid():N}@example.test");
        await using var scope = factory.Services.CreateAsyncScope();
        var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var user = (await users.FindByIdAsync(session.User.Id))!;
        var options = scope.ServiceProvider.GetRequiredService<Microsoft.Extensions.Options.IOptions<JwtOptions>>();
        var tokens = new TokenService(options, new PastTimeProvider());
        var expired = tokens.CreateAccessToken(user, ["Author"]).Token;
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", expired + "tampered");
        using var invalid = await client.PostAsJsonAsync("/api/v1/auth/logout", new { refreshToken = session.RefreshToken });
        Assert.Equal(HttpStatusCode.Unauthorized, invalid.StatusCode);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", expired);
        using var logout = await client.PostAsJsonAsync("/api/v1/auth/logout", new { refreshToken = session.RefreshToken });
        Assert.Equal(HttpStatusCode.NoContent, logout.StatusCode);
        using var revoked = await client.PostAsJsonAsync("/api/v1/auth/refresh", new { refreshToken = session.RefreshToken });
        Assert.Equal(HttpStatusCode.Unauthorized, revoked.StatusCode);
    }

    /// <summary>Verifies that refresh rejects inactive and locked users and revokes their family.</summary>
    /// <param name="inactive">The inactive supplied to this operation.</param>
    /// <returns>A task representing the operation and its result.</returns>
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task RefreshRejectsInactiveAndLockedUsersAndRevokesTheirFamily(bool inactive)
    {
        using var client = factory.CreateClient();
        var session = await this.RegisterAsync($"refresh-disabled-{Guid.NewGuid():N}@example.test");
        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var user = (await users.FindByIdAsync(session.User.Id))!;
            if (inactive)
            {
                user.IsActive = false;
            }
            else
            {
                user.LockoutEnd = DateTimeOffset.UtcNow.AddMinutes(15);
            }

            Assert.True((await users.UpdateAsync(user)).Succeeded);
        }

        using var response = await client.PostAsJsonAsync("/api/v1/auth/refresh", new { refreshToken = session.RefreshToken });
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        await using var verify = factory.Services.CreateAsyncScope();
        var db = verify.ServiceProvider.GetRequiredService<AppDbContext>();
        var hash = TokenService.HashRefreshToken(session.RefreshToken);
        Assert.NotNull((await db.RefreshTokens.SingleAsync(token => token.TokenHash == hash)).RevokedAt);
    }

    /// <summary>Verifies that registration duplicate ttls generic login and safe profile match local contract.</summary>
    /// <returns>A task representing the operation and its result.</returns>
    [Fact]
    public async Task RegistrationDuplicateTtlsGenericLoginAndSafeProfileMatchLocalContract()
    {
        using var client = factory.CreateClient();
        var email = $"local-contract-{Guid.NewGuid():N}@example.test";
        var registered = await this.RegisterAsync(email);
        Assert.Equal("Author", Assert.Single(registered.User.Roles));
        Assert.InRange(registered.ExpiresAt - DateTimeOffset.UtcNow, TimeSpan.FromMinutes(14), TimeSpan.FromMinutes(15));
        using var duplicate = await client.PostAsJsonAsync("/api/v1/auth/register", new { displayName = "Duplicate Author", email, password = "Valid#Password1" });
        Assert.Equal(HttpStatusCode.Conflict, duplicate.StatusCode);
        using var wrong = await client.PostAsJsonAsync("/api/v1/auth/login", new { email, password = "Wrong#Password1" });
        using var unknown = await client.PostAsJsonAsync("/api/v1/auth/login", new { email = $"unknown-{Guid.NewGuid():N}@example.test", password = "Wrong#Password1" });
        Assert.Equal(HttpStatusCode.Unauthorized, wrong.StatusCode);
        Assert.Equal(wrong.StatusCode, unknown.StatusCode);
        var wrongProblem = await wrong.Content.ReadFromJsonAsync<System.Text.Json.JsonElement>();
        var unknownProblem = await unknown.Content.ReadFromJsonAsync<System.Text.Json.JsonElement>();
        Assert.Equal(wrongProblem.GetProperty("detail").GetString(), unknownProblem.GetProperty("detail").GetString());
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", registered.AccessToken);
        using var profile = await client.GetAsync("/api/v1/auth/me");
        Assert.Equal(HttpStatusCode.OK, profile.StatusCode);
        var userJson = await profile.Content.ReadAsStringAsync();
        Assert.DoesNotContain("passwordHash", userJson, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("securityStamp", userJson, StringComparison.OrdinalIgnoreCase);
        await using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        Assert.Equal(1, await db.Users.CountAsync(user => user.Email == email));
        var hash = TokenService.HashRefreshToken(registered.RefreshToken);
        var stored = await db.RefreshTokens.SingleAsync(token => token.TokenHash == hash);
        Assert.InRange(stored.ExpiresAt - DateTimeOffset.UtcNow, TimeSpan.FromDays(7) - TimeSpan.FromMinutes(1), TimeSpan.FromDays(7));
    }

    private async Task<AuthSession> RegisterAsync(string email)
    {
        using var response = await this.sharedClient.PostAsJsonAsync(
            "/api/v1/auth/register",
            new { displayName = "Integration Author", email, password = "Valid#Password1" });
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<DataEnvelope<AuthSession>>())!.Data;
    }

    private sealed class PastTimeProvider : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => DateTimeOffset.UtcNow.AddHours(-1);
    }

    private sealed record DataEnvelope<T>(T Data);

    private sealed record AuthSession(string AccessToken, string RefreshToken, DateTimeOffset ExpiresAt, AuthUser User);

    private sealed record AuthUser(string Id, string Email, string DisplayName, IReadOnlyCollection<string> Roles, string? AvatarUrl, string? Bio);

    private sealed record Problem(string Code);
}
