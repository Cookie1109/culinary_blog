// Culinary Blog. Authorship is recorded in the repository history.

using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using CulinaryBlog.Application.Audit;
using CulinaryBlog.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;

namespace CulinaryBlog.IntegrationTests;

/// <summary>Contains regression checks for admin audit logs api tests.</summary>
/// <param name="factory">The factory supplied to this operation.</param>
public sealed class AdminAuditLogsApiTests(AuthApiFactory factory) : IClassFixture<AuthApiFactory>
{
    private readonly HttpClient client = factory.CreateClient();

    /// <summary>Verifies that anonymous request is rejected with unauthorized.</summary>
    /// <returns>A task representing the operation and its result.</returns>
    [Fact]
    public async Task AnonymousRequestIsRejectedWithUnauthorized()
    {
        this.SetToken(null);
        using var response = await this.client.GetAsync("/api/v1/admin/audit-logs");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    /// <summary>Verifies that author request is rejected with forbidden.</summary>
    /// <returns>A task representing the operation and its result.</returns>
    [Fact]
    public async Task AuthorRequestIsRejectedWithForbidden()
    {
        var author = await this.RegisterAsync($"author-{Guid.NewGuid():N}@example.com");
        this.SetToken(author.AccessToken);

        using var response = await this.client.GetAsync("/api/v1/admin/audit-logs");
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    /// <summary>Verifies that admin request returns audit logs and telemetry metadata.</summary>
    /// <returns>A task representing the operation and its result.</returns>
    [Fact]
    public async Task AdminRequestReturnsAuditLogsAndTelemetryMetadata()
    {
        var adminEmail = $"admin-{Guid.NewGuid():N}@example.com";
        await this.RegisterAsync(adminEmail);

        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var appUser = await userManager.FindByEmailAsync(adminEmail);
            Assert.NotNull(appUser);
            Assert.True((await userManager.AddToRoleAsync(appUser, "Admin")).Succeeded);
        }

        var admin = await this.LoginAsync(adminEmail);
        this.SetToken(admin.AccessToken);

        var correlationId = $"audit-{Guid.NewGuid():N}";
        this.client.DefaultRequestHeaders.Add("X-Correlation-ID", correlationId);
        using var createResponse = await this.client.PostAsJsonAsync("/api/v1/categories", new
        {
            name = $"Audit Category {Guid.NewGuid():N}",
            description = "Created to verify the structured audit trail.",
            imageUrl = (string?)null,
            orderIndex = 1,
        });
        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);

        using var response = await this.client.GetAsync(
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
        Assert.Equal("Audit Test User", auditEntry.Properties!["UserName"]);
        Assert.Contains("Audit Category", auditEntry.Properties["EntityName"], StringComparison.Ordinal);
        Assert.Equal(201, auditEntry.StatusCode);

        using var sameResponse = await this.client.GetAsync($"/api/v1/admin/audit-logs?search={correlationId}");
        var sameBody = await sameResponse.Content.ReadFromJsonAsync<AuditLogsEnvelope>();
        Assert.Equal(auditEntry.Id, Assert.Single(sameBody!.Data, entry => entry.EventName == "category.created").Id);

        using var searchResponse = await this.client.GetAsync("/api/v1/admin/audit-logs?search=Audit%20Test%20User");
        var searchBody = await searchResponse.Content.ReadFromJsonAsync<AuditLogsEnvelope>();
        Assert.Contains(searchBody!.Data, entry => entry.CorrelationId == correlationId);

        using var createdBody = JsonDocument.Parse(await createResponse.Content.ReadAsStringAsync());
        var categoryId = createdBody.RootElement.GetProperty("data").GetProperty("id").GetGuid();
        var updatedName = $"Changed audit {Guid.NewGuid():N}";
        using var updateResponse = await this.client.PutAsJsonAsync($"/api/v1/categories/{categoryId}", new
        {
            name = updatedName,
            description = "Updated in audit test",
            imageUrl = (string?)null,
            orderIndex = 2,
        });
        Assert.Equal(HttpStatusCode.OK, updateResponse.StatusCode);
        using var deleteResponse = await this.client.DeleteAsync($"/api/v1/categories/{categoryId}");
        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);
        using var historyResponse = await this.client.GetAsync($"/api/v1/admin/audit-logs?search={correlationId}");
        var history = await historyResponse.Content.ReadFromJsonAsync<AuditLogsEnvelope>();
        foreach (var action in new[] { "created", "updated", "deleted" })
        {
            var entry = Assert.Single(history!.Data, entry => entry.EventName == $"category.{action}");
            Assert.Equal("Audit Test User", entry.Properties!["UserName"]);
            Assert.Equal(categoryId.ToString(), entry.Properties["EntityId"]);
            if (action != "created")
            {
                Assert.Equal(updatedName, entry.Properties["EntityName"]);
            }
        }

        this.client.DefaultRequestHeaders.Remove("X-Correlation-ID");
    }

    private async Task<AuthSession> RegisterAsync(string email)
    {
        using var response = await this.client.PostAsJsonAsync("/api/v1/auth/register", new
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
        using var response = await this.client.PostAsJsonAsync("/api/v1/auth/login", new
        {
            email,
            password = "Password123!",
        });
        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadFromJsonAsync<DataEnvelope<AuthSession>>();
        return body!.Data;
    }

    private void SetToken(string? token) => this.client.DefaultRequestHeaders.Authorization = string.IsNullOrWhiteSpace(token)
        ? null
        : new AuthenticationHeaderValue("Bearer", token);

    private sealed record DataEnvelope<T>(T Data);

    private sealed record AuthSession(string AccessToken, AuthUser User);

    private sealed record AuthUser(string Email);
}
