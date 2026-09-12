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

public sealed class AuthLifecycleTests(AuthApiFactory factory) : IClassFixture<AuthApiFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task RegisterProfileRefreshLogoutLifecycleIsSecure()
    {
        const string password = "Valid#Password1";
        using var registerResponse = await _client.PostAsJsonAsync(
            "/api/v1/auth/register",
            new { displayName = "Integration Author", email = "author@example.com", password });
        var registered = await registerResponse.Content.ReadFromJsonAsync<DataEnvelope<AuthSession>>();

        Assert.Equal(HttpStatusCode.Created, registerResponse.StatusCode);
        Assert.NotNull(registered);
        Assert.Contains("Author", registered.Data.User.Roles);

        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var userId = Guid.Parse(registered.Data.User.Id);
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

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", registered.Data.AccessToken);
        using var profileResponse = await _client.PatchAsJsonAsync(
            "/api/v1/auth/me",
            new { displayName = "Updated Author", bio = "A short bio" });
        var profile = await profileResponse.Content.ReadFromJsonAsync<DataEnvelope<AuthUser>>();
        Assert.Equal(HttpStatusCode.OK, profileResponse.StatusCode);
        Assert.Equal("Updated Author", profile?.Data.DisplayName);

        _client.DefaultRequestHeaders.Authorization = null;
        using var refreshResponse = await _client.PostAsJsonAsync(
            "/api/v1/auth/refresh",
            new { refreshToken = registered.Data.RefreshToken });
        var refreshed = await refreshResponse.Content.ReadFromJsonAsync<DataEnvelope<AuthSession>>();
        Assert.Equal(HttpStatusCode.OK, refreshResponse.StatusCode);
        Assert.NotNull(refreshed);
        Assert.NotEqual(registered.Data.RefreshToken, refreshed.Data.RefreshToken);

        using var replayResponse = await _client.PostAsJsonAsync(
            "/api/v1/auth/refresh",
            new { refreshToken = registered.Data.RefreshToken });
        Assert.Equal(HttpStatusCode.Unauthorized, replayResponse.StatusCode);

        using var logoutResponse = await _client.PostAsJsonAsync(
            "/api/v1/auth/logout",
            new { refreshToken = refreshed.Data.RefreshToken });
        using var repeatedLogoutResponse = await _client.PostAsJsonAsync(
            "/api/v1/auth/logout",
            new { refreshToken = refreshed.Data.RefreshToken });
        Assert.Equal(HttpStatusCode.NoContent, logoutResponse.StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, repeatedLogoutResponse.StatusCode);

        using var afterLogoutResponse = await _client.PostAsJsonAsync(
            "/api/v1/auth/refresh",
            new { refreshToken = refreshed.Data.RefreshToken });
        Assert.Equal(HttpStatusCode.Unauthorized, afterLogoutResponse.StatusCode);
    }

    [Fact]
    public async Task FiveInvalidPasswordsLockTheAccountWithoutEnumeratingIt()
    {
        await RegisterAsync("locked@example.com");
        for (var attempt = 1; attempt <= 5; attempt++)
        {
            using var response = await _client.PostAsJsonAsync(
                "/api/v1/auth/login",
                new { email = "locked@example.com", password = "Wrong#Password1" });
            Assert.Equal(attempt == 5 ? HttpStatusCode.Locked : HttpStatusCode.Unauthorized, response.StatusCode);
        }

        using var unknownResponse = await _client.PostAsJsonAsync(
            "/api/v1/auth/login",
            new { email = "unknown@example.com", password = "Wrong#Password1" });
        var problem = await unknownResponse.Content.ReadFromJsonAsync<Problem>();
        Assert.Equal(HttpStatusCode.Unauthorized, unknownResponse.StatusCode);
        Assert.Equal("AUTH_INVALID_CREDENTIALS", problem?.Code);
    }

    [Fact]
    public async Task ConcurrentRefreshProducesAtMostOneSuccessAndRevokesTheFamily()
    {
        var registered = await RegisterAsync("concurrent@example.com");
        var first = _client.PostAsJsonAsync(
            "/api/v1/auth/refresh",
            new { refreshToken = registered.RefreshToken });
        var second = _client.PostAsJsonAsync(
            "/api/v1/auth/refresh",
            new { refreshToken = registered.RefreshToken });

        var responses = await Task.WhenAll(first, second);
        Assert.Single(responses, response => response.StatusCode == HttpStatusCode.OK);
        Assert.Single(responses, response => response.StatusCode == HttpStatusCode.Unauthorized);

        var successfulResponse = responses.Single(response => response.StatusCode == HttpStatusCode.OK);
        var rotated = await successfulResponse.Content.ReadFromJsonAsync<DataEnvelope<AuthSession>>();
        Assert.NotNull(rotated);
        using var familyResponse = await _client.PostAsJsonAsync(
            "/api/v1/auth/refresh",
            new { refreshToken = rotated.Data.RefreshToken });
        Assert.Equal(HttpStatusCode.Unauthorized, familyResponse.StatusCode);
    }

    [Fact]
    public async Task ExpiredRefreshAndTamperedAccessTokensAreRejected()
    {
        var registered = await RegisterAsync("expired@example.com");
        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var tokenHash = TokenService.HashRefreshToken(registered.RefreshToken);
            var token = await dbContext.RefreshTokens.SingleAsync(item => item.TokenHash == tokenHash);
            token.ExpiresAt = DateTimeOffset.UtcNow.AddMinutes(-1);
            await dbContext.SaveChangesAsync();
        }

        using var expiredResponse = await _client.PostAsJsonAsync(
            "/api/v1/auth/refresh",
            new { refreshToken = registered.RefreshToken });
        Assert.Equal(HttpStatusCode.Unauthorized, expiredResponse.StatusCode);

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", registered.AccessToken + "tampered");
        using var tamperedResponse = await _client.GetAsync("/api/v1/auth/me");
        _client.DefaultRequestHeaders.Authorization = null;
        Assert.Equal(HttpStatusCode.Unauthorized, tamperedResponse.StatusCode);
    }

    [Fact]
    public async Task AuthorAndAdminPoliciesAlsoRequireAnActiveUser()
    {
        var registered = await RegisterAsync("policy@example.com");
        await using var scope = factory.Services.CreateAsyncScope();
        var authorization = scope.ServiceProvider.GetRequiredService<IAuthorizationService>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var user = await userManager.FindByEmailAsync("policy@example.com");
        Assert.NotNull(user);

        var principal = new ClaimsPrincipal(new ClaimsIdentity(
        [
            new Claim(ClaimTypes.NameIdentifier, registered.User.Id),
            new Claim(ClaimTypes.Role, "Author"),
        ], "test"));
        Assert.True((await authorization.AuthorizeAsync(principal, null, "AuthorPolicy")).Succeeded);
        Assert.False((await authorization.AuthorizeAsync(principal, null, "AdminPolicy")).Succeeded);

        user.IsActive = false;
        Assert.True((await userManager.UpdateAsync(user)).Succeeded);
        Assert.False((await authorization.AuthorizeAsync(principal, null, "AuthorPolicy")).Succeeded);
    }

    private async Task<AuthSession> RegisterAsync(string email)
    {
        using var response = await _client.PostAsJsonAsync(
            "/api/v1/auth/register",
            new { displayName = "Integration Author", email, password = "Valid#Password1" });
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<DataEnvelope<AuthSession>>())!.Data;
    }

    private sealed record DataEnvelope<T>(T Data);

    private sealed record AuthSession(string AccessToken, string RefreshToken, DateTimeOffset ExpiresAt, AuthUser User);

    private sealed record AuthUser(string Id, string Email, string DisplayName, IReadOnlyCollection<string> Roles);

    private sealed record Problem(string Code);
}
