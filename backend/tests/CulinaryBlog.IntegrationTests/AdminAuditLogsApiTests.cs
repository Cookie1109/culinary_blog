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
        string adminEmail = $"admin-{Guid.NewGuid():N}@example.com";
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

        string correlationId = $"audit-{Guid.NewGuid():N}";
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
        string updatedName = $"Changed audit {Guid.NewGuid():N}";
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
        foreach (string? action in new[] { "created", "updated", "deleted" })
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

    /// <summary>Verifies that error responses retain the request correlation header.</summary>
    /// <param name="validEmail">The valid email supplied to this operation.</param>
    /// <param name="expectedStatus">The expected status supplied to this operation.</param>
    /// <returns>A task representing the operation and its result.</returns>
    [Theory]
    [InlineData(false, HttpStatusCode.UnprocessableEntity)]
    [InlineData(true, HttpStatusCode.Unauthorized)]
    public async Task ErrorResponsesRetainTheRequestCorrelationHeader(bool validEmail, HttpStatusCode expectedStatus)
    {
        string correlation = "rejected-" + Guid.NewGuid().ToString("N");
        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/v1/auth/login")
        {
            Content = JsonContent.Create(new { email = validEmail ? "not-registered@example.test" : "invalid", password = "Synthetic#Invalid123" }),
        };
        request.Headers.Add("X-Correlation-ID", correlation);
        using var response = await this.client.SendAsync(request);
        Assert.Equal(expectedStatus, response.StatusCode);
        Assert.Equal(correlation, Assert.Single(response.Headers.GetValues("X-Correlation-ID")));
        var problem = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(correlation, problem.GetProperty("correlationId").GetString());
    }

    /// <summary>Verifies that recipe history keeps every real edit and transition without snapshot duplicates.</summary>
    /// <returns>A task representing the operation and its result.</returns>
    [Fact]
    public async Task RecipeHistoryKeepsEveryRealEditAndTransitionWithoutSnapshotDuplicates()
    {
        string email = $"history-{Guid.NewGuid():N}@example.com";
        await this.RegisterAsync(email);
        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            Assert.True((await users.AddToRoleAsync((await users.FindByEmailAsync(email))!, "Admin")).Succeeded);
        }

        this.SetToken((await this.LoginAsync(email)).AccessToken);
        using var categories = await this.client.GetAsync("/api/v1/categories");
        var category = (await categories.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("data")[0].GetProperty("id").GetGuid();
        string prefix = $"history-{Guid.NewGuid():N}";
        var expected = new List<(string Event, string Correlation)>();
        string actorId = string.Empty;
        async Task<JsonElement> Send(string method, string path, string action, object? body = null, long? version = null)
        {
            string correlation = $"{prefix}-{expected.Count}";
            using var request = new HttpRequestMessage(new HttpMethod(method), path);
            request.Headers.Add("X-Correlation-ID", correlation);
            if (version.HasValue)
            {
                request.Headers.TryAddWithoutValidation("If-Match", $"\"{version}\"");
            }

            if (body is not null)
            {
                request.Content = JsonContent.Create(body);
            }

            using var response = await this.client.SendAsync(request);
            response.EnsureSuccessStatusCode();
            expected.Add((action, correlation));
            return response.StatusCode == HttpStatusCode.NoContent ? default : (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("data");
        }

        object Payload(string title) => new { title, description = "History check", categoryId = category, prepTime = 10, cookTime = 0, servings = 2, difficulty = "easy" };
        var created = await Send("POST", "/api/v1/recipes", "recipe.created", Payload("Audit history recipe"));
        var id = created.GetProperty("id").GetGuid();
        string path = $"/api/v1/recipes/{id}";
        await Send("PUT", path, "recipe.updated", Payload("Audit history edit one"), 1);
        await Send("PUT", path, "recipe.updated", Payload("Audit history edit two"), 2);
        await Send("POST", path + "/ingredients", "recipe.ingredients.created", new { name = "Rice" }, 3);
        await Send("POST", path + "/steps", "recipe.steps.created", new { title = "Cook", description = "Cook rice" }, 4);

        // Later lifecycle changes must not be mistaken for a third explicit edit.
        await Task.Delay(1100);
        long nextVersion = 5;
        foreach (var (route, action) in new[] { ("publish", "published"), ("unpublish", "unpublished"), ("publish", "published"), ("archive", "archived"), ("unarchive", "unarchived") })
        {
            await Send("PATCH", path + "/" + route, "recipe." + action, version: nextVersion++);
        }

        await Send("DELETE", path, "recipe.deleted", version: nextVersion);
        using var historyResponse = await this.client.GetAsync($"/api/v1/admin/audit-logs?search={id}&pageSize=100");
        var history = (await historyResponse.Content.ReadFromJsonAsync<AuditLogsEnvelope>())!.Data
            .Where(entry => entry.EventName.StartsWith("recipe.", StringComparison.Ordinal)).ToArray();
        Assert.Equal(expected.Count, history.Length);
        foreach (var (action, correlation) in expected)
        {
            var entry = Assert.Single(history, item => item.CorrelationId == correlation);
            Assert.Equal(action, entry.EventName);
            Assert.Equal(id.ToString(), entry.Properties!["EntityId"]);
            Assert.NotEqual("snapshot", entry.Properties.GetValueOrDefault("Source"));
            Assert.Equal("Audit Test User", entry.Properties["UserName"]);
            Assert.False(string.IsNullOrWhiteSpace(entry.UserId));
            actorId = string.IsNullOrEmpty(actorId) ? entry.UserId : actorId;
            Assert.Equal(actorId, entry.UserId);
            Assert.InRange(entry.Timestamp, DateTimeOffset.UtcNow.AddMinutes(-5), DateTimeOffset.UtcNow.AddMinutes(1));
        }

        Assert.Equal(2, history.Count(entry => entry.EventName == "recipe.updated"));
        Assert.Equal(2, history.Count(entry => entry.EventName == "recipe.published"));
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
