using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using CulinaryBlog.Application.Audit;
using CulinaryBlog.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;

namespace CulinaryBlog.IntegrationTests;

public sealed class AdminAuditLogsApiTests(AuthApiFactory factory) : IClassFixture<AuthApiFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task AnonymousRequestIsRejectedWithUnauthorized()
    {
        SetToken(null);
        using var response = await _client.GetAsync("/api/v1/admin/audit-logs");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task AuthorRequestIsRejectedWithForbidden()
    {
        var author = await RegisterAsync($"author-{Guid.NewGuid():N}@example.com");
        SetToken(author.AccessToken);

        using var response = await _client.GetAsync("/api/v1/admin/audit-logs");
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task AdminRequestReturnsAuditLogsAndTelemetryMetadata()
    {
        var adminEmail = $"admin-{Guid.NewGuid():N}@example.com";
        var user = await RegisterAsync(adminEmail);

        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var appUser = await userManager.FindByEmailAsync(adminEmail);
            Assert.NotNull(appUser);
            Assert.True((await userManager.AddToRoleAsync(appUser, "Admin")).Succeeded);
        }

        var admin = await LoginAsync(adminEmail);
        SetToken(admin.AccessToken);

        var correlationId = $"audit-{Guid.NewGuid():N}";
        _client.DefaultRequestHeaders.Add("X-Correlation-ID", correlationId);
        using var createResponse = await _client.PostAsJsonAsync("/api/v1/categories", new
        {
            name = $"Audit Category {Guid.NewGuid():N}",
            description = "Created to verify the structured audit trail.",
            imageUrl = (string?)null,
            orderIndex = 1,
        });
        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);

        using var response = await _client.GetAsync(
            $"/api/v1/admin/audit-logs?search={correlationId}&page=1&pageSize=20");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<AuditLogsEnvelope>();
        Assert.NotNull(body);
        Assert.NotNull(body.Data);
        Assert.NotNull(body.Meta);
        Assert.NotNull(body.Telemetry);
        Assert.True(body.Telemetry.OperationalNetworkRestricted, "Telemetry must specify operational network restriction");
        var auditEntry = Assert.Single(body.Data, entry => entry.EventName == "category.created");
        Assert.Equal("category.created", auditEntry.EventName);
        Assert.Equal(correlationId, auditEntry.CorrelationId);
        Assert.False(string.IsNullOrWhiteSpace(auditEntry.UserId));
        Assert.Equal("POST", auditEntry.RequestMethod);
        Assert.Equal("/api/v1/categories", auditEntry.RequestPath);

        _client.DefaultRequestHeaders.Remove("X-Correlation-ID");
    }

    private async Task<AuthSession> RegisterAsync(string email)
    {
        using var response = await _client.PostAsJsonAsync("/api/v1/auth/register", new
        {
            email,
            password = "Password123!",
            displayName = "Audit Test User",
        });
        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadFromJsonAsync<DataEnvelope<AuthSession>>();
        return body!.Data;
    }

    private async Task<AuthSession> LoginAsync(string email)
    {
        using var response = await _client.PostAsJsonAsync("/api/v1/auth/login", new
        {
            email,
            password = "Password123!",
        });
        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadFromJsonAsync<DataEnvelope<AuthSession>>();
        return body!.Data;
    }

    private void SetToken(string? token) => _client.DefaultRequestHeaders.Authorization = string.IsNullOrWhiteSpace(token)
        ? null
        : new AuthenticationHeaderValue("Bearer", token);

    private sealed record DataEnvelope<T>(T Data);

    private sealed record AuthSession(string AccessToken, AuthUser User);

    private sealed record AuthUser(string Email);
}
