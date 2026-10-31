// Culinary Blog. Authorship is recorded in the repository history.

using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;

namespace CulinaryBlog.IntegrationTests;

/// <summary>Contains regression checks for password lifecycle tests.</summary>
/// <param name="factory">The factory supplied to this operation.</param>
public sealed class PasswordLifecycleTests(AuthApiFactory factory) : IClassFixture<AuthApiFactory>
{
    private const string OldPassword = "Old#Password1";
    private const string NewPassword = "New#Password2";

    /// <summary>Verifies that change requires current password and revokes every session.</summary>
    /// <returns>A task representing the operation and its result.</returns>
    [Fact]
    public async Task ChangeRequiresCurrentPasswordAndRevokesEverySession()
    {
        using var client = factory.CreateClient();
        const string email = "change-password@example.test";
        var session = await RegisterAsync(client, email);
        using var secondLogin = await client.PostAsJsonAsync("/api/v1/auth/login", new { email, password = OldPassword });
        var second = (await secondLogin.Content.ReadFromJsonAsync<Envelope<Session>>())!.Data;
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", session.AccessToken);
        using var wrong = await client.PostAsJsonAsync("/api/v1/auth/change-password", new { currentPassword = "Wrong#Password1", newPassword = NewPassword });
        Assert.Equal(HttpStatusCode.BadRequest, wrong.StatusCode);
        using var profile = await client.GetAsync("/api/v1/auth/me");
        Assert.Equal(HttpStatusCode.OK, profile.StatusCode);
        using var changed = await client.PostAsJsonAsync("/api/v1/auth/change-password", new { currentPassword = OldPassword, newPassword = NewPassword });
        Assert.Equal(HttpStatusCode.NoContent, changed.StatusCode);
        await AssertSessionRevokedAsync(client, session);
        await AssertSessionRevokedAsync(client, second);
        await AssertPasswordChangedAsync(client, email);
    }

    /// <summary>Verifies that forgot does not disclose unknown accounts and reset token is bound and single use.</summary>
    /// <returns>A task representing the operation and its result.</returns>
    [Fact]
    public async Task ForgotDoesNotDiscloseUnknownAccountsAndResetTokenIsBoundAndSingleUse()
    {
        using var client = factory.CreateClient();
        const string email = "reset-password@example.test";
        var session = await RegisterAsync(client, email);
        const string otherEmail = "reset-other@example.test";
        await RegisterAsync(client, otherEmail);
        using var known = await client.PostAsJsonAsync("/api/v1/auth/forgot-password", new { email });
        using var unknown = await client.PostAsJsonAsync("/api/v1/auth/forgot-password", new { email = "missing@example.test" });
        Assert.Equal(HttpStatusCode.Accepted, known.StatusCode);
        Assert.Equal(known.StatusCode, unknown.StatusCode);
        Assert.Equal(await known.Content.ReadAsStringAsync(), await unknown.Content.ReadAsStringAsync());
        Assert.DoesNotContain(factory.PasswordResetEmails, item => item.Email == "missing@example.test");
        var token = Assert.Single(factory.PasswordResetEmails, item => item.Email == email).Token;
        using var wrongAccount = await client.PostAsJsonAsync("/api/v1/auth/reset-password", new { email = otherEmail, token, newPassword = NewPassword });
        Assert.Equal(HttpStatusCode.BadRequest, wrongAccount.StatusCode);
        using var tampered = await client.PostAsJsonAsync("/api/v1/auth/reset-password", new { email, token = token + "tampered", newPassword = NewPassword });
        Assert.Equal(HttpStatusCode.BadRequest, tampered.StatusCode);
        using var weak = await client.PostAsJsonAsync("/api/v1/auth/reset-password", new { email, token, newPassword = "weak" });
        Assert.Equal(HttpStatusCode.UnprocessableEntity, weak.StatusCode);
        using var reset = await client.PostAsJsonAsync("/api/v1/auth/reset-password", new { email, token, newPassword = NewPassword });
        Assert.Equal(HttpStatusCode.NoContent, reset.StatusCode);
        using var replay = await client.PostAsJsonAsync("/api/v1/auth/reset-password", new { email, token, newPassword = OldPassword });
        Assert.Equal(HttpStatusCode.BadRequest, replay.StatusCode);
        await AssertSessionRevokedAsync(client, session);
        await AssertPasswordChangedAsync(client, email);
    }

    /// <summary>Verifies that expired reset token is rejected.</summary>
    /// <returns>A task representing the operation and its result.</returns>
    [Fact]
    public async Task ExpiredResetTokenIsRejected()
    {
        await using var expiredFactory = factory.WithWebHostBuilder(builder => builder.ConfigureTestServices(services =>
            services.PostConfigure<DataProtectionTokenProviderOptions>(options => options.TokenLifespan = TimeSpan.FromSeconds(-1))));
        using var client = expiredFactory.CreateClient();
        const string email = "expired-reset@example.test";
        await RegisterAsync(client, email);
        using var forgot = await client.PostAsJsonAsync("/api/v1/auth/forgot-password", new { email });
        Assert.Equal(HttpStatusCode.Accepted, forgot.StatusCode);
        var token = Assert.Single(factory.PasswordResetEmails, item => item.Email == email).Token;
        using var reset = await client.PostAsJsonAsync("/api/v1/auth/reset-password", new { email, token, newPassword = NewPassword });
        Assert.Equal(HttpStatusCode.BadRequest, reset.StatusCode);
    }

    /// <summary>Verifies that change requires authentication and recovery validates input.</summary>
    /// <returns>A task representing the operation and its result.</returns>
    [Fact]
    public async Task ChangeRequiresAuthenticationAndRecoveryValidatesInput()
    {
        using var client = factory.CreateClient();
        using var change = await client.PostAsJsonAsync("/api/v1/auth/change-password", new { currentPassword = OldPassword, newPassword = NewPassword });
        Assert.Equal(HttpStatusCode.Unauthorized, change.StatusCode);
        using var invalid = await client.PostAsJsonAsync("/api/v1/auth/forgot-password", new { email = "invalid" });
        Assert.Equal(HttpStatusCode.UnprocessableEntity, invalid.StatusCode);
        using var missingPassword = await client.PostAsJsonAsync("/api/v1/auth/reset-password", new { email = "a@example.test", token = "token" });
        Assert.Equal(HttpStatusCode.UnprocessableEntity, missingPassword.StatusCode);
    }

    /// <summary>Verifies that reset lifetime is thirty minutes and smtp failure still does not disclose accounts.</summary>
    /// <returns>A task representing the operation and its result.</returns>
    [Fact]
    public async Task ResetLifetimeIsThirtyMinutesAndSmtpFailureStillDoesNotDiscloseAccounts()
    {
        await using var smtpFailure = factory.WithWebHostBuilder(builder =>
        {
            builder.UseSetting("Email:Enabled", "true");
            builder.UseSetting("Email:Host", "127.0.0.1");
            builder.UseSetting("Email:Port", "1");
            builder.ConfigureTestServices(services =>
            {
                Microsoft.Extensions.DependencyInjection.Extensions.ServiceCollectionDescriptorExtensions.RemoveAll<CulinaryBlog.Application.Auth.IPasswordResetEmailSender>(services);
                services.AddScoped<CulinaryBlog.Application.Auth.IPasswordResetEmailSender, CulinaryBlog.Infrastructure.Email.PasswordResetEmailSender>();
            });
        });
        using var client = smtpFailure.CreateClient();
        var email = $"smtp-failure-{Guid.NewGuid():N}@example.test";
        await RegisterAsync(client, email);
        using var known = await client.PostAsJsonAsync("/api/v1/auth/forgot-password", new { email });
        using var unknown = await client.PostAsJsonAsync("/api/v1/auth/forgot-password", new { email = $"unknown-{Guid.NewGuid():N}@example.test" });
        Assert.Equal(HttpStatusCode.Accepted, known.StatusCode);
        Assert.Equal(known.StatusCode, unknown.StatusCode);
        Assert.Equal(await known.Content.ReadAsStringAsync(), await unknown.Content.ReadAsStringAsync());
        await using var scope = smtpFailure.Services.CreateAsyncScope();
        var options = scope.ServiceProvider.GetRequiredService<Microsoft.Extensions.Options.IOptions<DataProtectionTokenProviderOptions>>();
        Assert.Equal(TimeSpan.FromMinutes(30), options.Value.TokenLifespan);
    }

    private static async Task<Session> RegisterAsync(HttpClient client, string email)
    {
        using var response = await client.PostAsJsonAsync("/api/v1/auth/register", new { displayName = "Password Test", email, password = OldPassword });
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<Envelope<Session>>())!.Data;
    }

    private static async Task AssertSessionRevokedAsync(HttpClient client, Session session)
    {
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", session.AccessToken);
        using var access = await client.GetAsync("/api/v1/auth/me");
        Assert.Equal(HttpStatusCode.Unauthorized, access.StatusCode);
        client.DefaultRequestHeaders.Authorization = null;
        using var refresh = await client.PostAsJsonAsync("/api/v1/auth/refresh", new { refreshToken = session.RefreshToken });
        Assert.Equal(HttpStatusCode.Unauthorized, refresh.StatusCode);
    }

    private static async Task AssertPasswordChangedAsync(HttpClient client, string email)
    {
        using var oldLogin = await client.PostAsJsonAsync("/api/v1/auth/login", new { email, password = OldPassword });
        using var newLogin = await client.PostAsJsonAsync("/api/v1/auth/login", new { email, password = NewPassword });
        Assert.Equal(HttpStatusCode.Unauthorized, oldLogin.StatusCode);
        Assert.Equal(HttpStatusCode.OK, newLogin.StatusCode);
    }

    private sealed record Envelope<T>(T Data);

    private sealed record Session(string AccessToken, string RefreshToken);
}
