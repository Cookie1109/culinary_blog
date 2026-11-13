// Culinary Blog. Authorship is recorded in the repository history.

using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using CulinaryBlog.Application.Auth;
using CulinaryBlog.Application.Content;
using CulinaryBlog.Application.Users;
using CulinaryBlog.Infrastructure.Identity;
using CulinaryBlog.Infrastructure.Persistence;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace CulinaryBlog.IntegrationTests;

/// <summary>Contains regression checks for admin users api tests.</summary>
/// <param name="factory">The factory supplied to this operation.</param>
public sealed class AdminUsersApiTests(AuthApiFactory factory) : IClassFixture<AuthApiFactory>
{
    private const string Password = "Synthetic#Users123";
    private static readonly string[] BothRoles = ["Admin", "Author"];
    private static readonly string[] SafeFields = ["createdAt", "displayName", "email", "id", "isActive", "roles"];
    private static readonly string[] QueryNames = ["page", "pageSize", "role", "search"];

    /// <summary>Verifies that directory rejects guest authors inactive and demoted admins.</summary>
    /// <returns>A task representing the operation and its result.</returns>
    [Fact]
    public async Task DirectoryRejectsGuestAuthorsInactiveAndDemotedAdmins()
    {
        using var client = factory.CreateClient();
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/v1/admin/users")).StatusCode);
        var author = await this.CreateSessionAsync(false);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", author.AccessToken);
        Assert.Equal(HttpStatusCode.Forbidden, (await client.GetAsync("/api/v1/admin/users")).StatusCode);

        var admin = await this.CreateSessionAsync(true);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", admin.AccessToken);
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/api/v1/admin/users")).StatusCode);
        await using var scope = factory.Services.CreateAsyncScope();
        var manager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var user = (await manager.FindByIdAsync(admin.User.Id))!;
        Assert.True((await manager.RemoveFromRoleAsync(user, "Admin")).Succeeded);
        Assert.Equal(HttpStatusCode.Forbidden, (await client.GetAsync("/api/v1/admin/users")).StatusCode);
        Assert.True((await manager.AddToRoleAsync(user, "Admin")).Succeeded);
        user.IsActive = false;
        Assert.True((await manager.UpdateAsync(user)).Succeeded);
        Assert.Equal(HttpStatusCode.Forbidden, (await client.GetAsync("/api/v1/admin/users")).StatusCode);
    }

    /// <summary>Verifies that directory filters before paging keeps all roles and excludes credentials.</summary>
    /// <returns>A task representing the operation and its result.</returns>
    [Fact]
    public async Task DirectoryFiltersBeforePagingKeepsAllRolesAndExcludesCredentials()
    {
        using var client = factory.CreateClient();
        var admin = await this.CreateSessionAsync(true);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", admin.AccessToken);
        string marker = $"directory-{Guid.NewGuid():N}";
        var createdAt = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var manager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            for (int index = 1; index <= 3; index++)
            {
                var user = new ApplicationUser
                {
                    Id = Guid.NewGuid().ToString(),
                    DisplayName = $"{marker} Người {index}",
                    Email = $"{marker}-{index}@users.test",
                    UserName = $"{marker}-{index}@users.test",
                    CreatedAt = createdAt.AddMinutes(index),
                    IsActive = index != 2,
                };
                Assert.True((await manager.CreateAsync(user)).Succeeded);
                Assert.True((await manager.AddToRoleAsync(user, "Author")).Succeeded);
                if (index == 3)
                {
                    Assert.True((await manager.AddToRoleAsync(user, "Admin")).Succeeded);
                }
            }
        }

        using var response = await client.GetAsync($"/api/v1/admin/users?search={marker}&pageSize=1");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.True(response.Headers.CacheControl?.Private);
        Assert.True(response.Headers.CacheControl?.NoStore);
        var result = (await response.Content.ReadFromJsonAsync<PageEnvelope<AdminUserDto>>())!;
        Assert.Equal(3, result.Meta.Total);
        Assert.Equal(3, result.Meta.TotalPages);
        Assert.True(result.Meta.HasNextPage);
        Assert.False(result.Meta.HasPreviousPage);
        var first = Assert.Single(result.Data);
        Assert.EndsWith("Người 3", first.DisplayName, StringComparison.Ordinal);
        Assert.Equal(BothRoles, first.Roles);

        var second = (await client.GetFromJsonAsync<PageEnvelope<AdminUserDto>>($"/api/v1/admin/users?search={marker}&pageSize=1&page=2"))!;
        Assert.False(Assert.Single(second.Data).IsActive);
        Assert.NotEqual(first.Id, Assert.Single(second.Data).Id);
        var admins = (await client.GetFromJsonAsync<PageEnvelope<AdminUserDto>>($"/api/v1/admin/users?search={marker}&role=Admin"))!;
        Assert.Equal(1, admins.Meta.Total);
        Assert.Equal(first.Id, Assert.Single(admins.Data).Id);
        var authors = (await client.GetFromJsonAsync<PageEnvelope<AdminUserDto>>($"/api/v1/admin/users?search={marker}&role=Author"))!;
        Assert.Equal(3, authors.Data.Count);
        var emailSearch = (await client.GetFromJsonAsync<PageEnvelope<AdminUserDto>>($"/api/v1/admin/users?search={marker}-1@USERS.TEST"))!;
        Assert.EndsWith("Người 1", Assert.Single(emailSearch.Data).DisplayName, StringComparison.Ordinal);
        var empty = (await client.GetFromJsonAsync<PageEnvelope<AdminUserDto>>($"/api/v1/admin/users?search={marker}&page=99"))!;
        Assert.Empty(empty.Data);
        Assert.False(empty.Meta.HasNextPage);

        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal(
            SafeFields,
            json.RootElement.GetProperty("data")[0].EnumerateObject().Select(property => property.Name).Order(StringComparer.Ordinal));
        using var defaultResponse = await client.GetAsync("/api/v1/admin/users");
        Assert.Equal(20, (await defaultResponse.Content.ReadFromJsonAsync<PageEnvelope<AdminUserDto>>())!.Meta.PageSize);
    }

    /// <summary>Verifies that directory search treats wildcards literally and uses stable tie break.</summary>
    /// <returns>A task representing the operation and its result.</returns>
    [Fact]
    public async Task DirectorySearchTreatsWildcardsLiterallyAndUsesStableTieBreak()
    {
        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", (await this.CreateSessionAsync(true)).AccessToken);
        string marker = $"literal-{Guid.NewGuid():N}";
        var ids = new[] { Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid() }.Order().ToArray();
        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var createdAt = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
            foreach (var id in ids)
            {
                db.Users.Add(new ApplicationUser
                {
                    Id = id.ToString(),
                    DisplayName = marker + "%_\\",
                    Email = $"{id:N}@users.test",
                    UserName = $"{id:N}@users.test",
                    NormalizedUserName = $"{id:N}@users.test".ToUpperInvariant(),
                    CreatedAt = createdAt,
                });
            }

            db.Users.Add(new ApplicationUser { Id = Guid.NewGuid().ToString(), DisplayName = marker + "XYZ", CreatedAt = createdAt });
            await db.SaveChangesAsync();
        }

        string search = Uri.EscapeDataString(marker + "%_\\");
        var result = (await client.GetFromJsonAsync<PageEnvelope<AdminUserDto>>($"/api/v1/admin/users?search={search}"))!;
        Assert.Equal(ids.Select(id => id.ToString()), result.Data.Select(user => user.Id));
        Assert.Equal(3, result.Meta.Total);
    }

    /// <summary>Verifies that directory rejects invalid queries with validation problem.</summary>
    /// <param name="query">The query supplied to this operation.</param>
    /// <returns>A task representing the operation and its result.</returns>
    [Theory]
    [InlineData("role=Guest")]
    [InlineData("role=admin")]
    [InlineData("page=0")]
    [InlineData("pageSize=0")]
    [InlineData("pageSize=101")]
    [InlineData("page=2147483647&pageSize=100")]
    public async Task DirectoryRejectsInvalidQueriesWithValidationProblem(string query)
    {
        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", (await this.CreateSessionAsync(true)).AccessToken);
        using var response = await client.GetAsync($"/api/v1/admin/users?{query}");
        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal("VALIDATION_ERROR", json.RootElement.GetProperty("code").GetString());
        Assert.True(json.RootElement.GetProperty("errors").EnumerateObject().Any());
    }

    /// <summary>Verifies that direct application dispatch without admin cannot read users.</summary>
    /// <returns>A task representing the operation and its result.</returns>
    [Fact]
    public async Task DirectApplicationDispatchWithoutAdminCannotReadUsers()
    {
        await using var scope = factory.Services.CreateAsyncScope();
        scope.ServiceProvider.GetRequiredService<IHttpContextAccessor>().HttpContext = null;
        var exception = await Assert.ThrowsAsync<ContentProblemException>(() =>
            scope.ServiceProvider.GetRequiredService<ISender>().Send(new ListAdminUsersQuery(null, null)));
        Assert.Equal(ContentProblemKind.Forbidden, exception.Kind);
    }

    /// <summary>Verifies that open api describes user directory and safe response.</summary>
    /// <returns>A task representing the operation and its result.</returns>
    [Fact]
    public async Task OpenApiDescribesUserDirectoryAndSafeResponse()
    {
        using var client = factory.CreateClient();
        using var response = await client.GetAsync("/openapi/v1.json");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var operation = json.RootElement.GetProperty("paths").GetProperty("/api/v1/admin/users").GetProperty("get");
        Assert.Equal("ListAdminUsers", operation.GetProperty("operationId").GetString());
        Assert.Equal(
            QueryNames,
            operation.GetProperty("parameters").EnumerateArray().Select(parameter => parameter.GetProperty("name").GetString()).Order(StringComparer.Ordinal));
        Assert.Equal(
            SafeFields,
            json.RootElement.GetProperty("components").GetProperty("schemas").GetProperty("AdminUserDto").GetProperty("properties").EnumerateObject().Select(property => property.Name).Order(StringComparer.Ordinal));
    }

    private async Task<AuthSessionDto> CreateSessionAsync(bool admin)
    {
        using var client = factory.CreateClient();
        string email = $"users-{Guid.NewGuid():N}@users.test";
        using var registration = await client.PostAsJsonAsync("/api/v1/auth/register", new { displayName = "Directory Actor", email, password = Password });
        Assert.Equal(HttpStatusCode.Created, registration.StatusCode);
        var session = (await registration.Content.ReadFromJsonAsync<DataEnvelope<AuthSessionDto>>())!.Data;
        if (!admin)
        {
            return session;
        }

        await using var scope = factory.Services.CreateAsyncScope();
        var manager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        Assert.True((await manager.AddToRoleAsync((await manager.FindByEmailAsync(email))!, "Admin")).Succeeded);
        using var login = await client.PostAsJsonAsync("/api/v1/auth/login", new { email, password = Password });
        Assert.Equal(HttpStatusCode.OK, login.StatusCode);
        return (await login.Content.ReadFromJsonAsync<DataEnvelope<AuthSessionDto>>())!.Data;
    }
}
