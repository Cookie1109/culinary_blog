// Culinary Blog. Authorship is recorded in the repository history.

using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using CulinaryBlog.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;

namespace CulinaryBlog.IntegrationTests;

/// <summary>Contains regression checks for jobs dashboard tests.</summary>
/// <param name="factory">The factory supplied to this operation.</param>
public sealed class JobsDashboardTests(AuthApiFactory factory) : IClassFixture<AuthApiFactory>
{
    /// <summary>Verifies that running hangfire dashboard rejects guest and author and accepts admin.</summary>
    /// <returns>A task representing the operation and its result.</returns>
    [Fact]
    public async Task RunningHangfireDashboardRejectsGuestAndAuthorAndAcceptsAdmin()
    {
        await using var custom = factory.WithWebHostBuilder(builder =>
        {
            builder.UseSetting("BackgroundJobs:Enabled", "true");
            builder.UseSetting("BackgroundJobs:NetworkRestrictionEnabled", "false");
        });
        using var client = custom.CreateClient();
        using var guest = await client.GetAsync("/jobs/");
        Assert.Equal(HttpStatusCode.Unauthorized, guest.StatusCode);
        var email = $"dashboard-{Guid.NewGuid():N}@example.test";
        using var registration = await client.PostAsJsonAsync("/api/v1/auth/register", new { email, password = "Valid#Password1", displayName = "Dashboard Author" });
        registration.EnsureSuccessStatusCode();
        var session = (await registration.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("data");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", session.GetProperty("accessToken").GetString());
        using var author = await client.GetAsync("/jobs/");
        Assert.Equal(HttpStatusCode.Forbidden, author.StatusCode);
        await using (var scope = custom.Services.CreateAsyncScope())
        {
            var manager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var user = await manager.FindByEmailAsync(email);
            Assert.True((await manager.AddToRoleAsync(user!, "Admin")).Succeeded);
        }

        client.DefaultRequestHeaders.Authorization = null;
        using var login = await client.PostAsJsonAsync("/api/v1/auth/login", new { email, password = "Valid#Password1" });
        login.EnsureSuccessStatusCode();
        var adminToken = (await login.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("data").GetProperty("accessToken").GetString();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);
        using var admin = await client.GetAsync("/jobs/");
        Assert.Equal(HttpStatusCode.OK, admin.StatusCode);
        Assert.Contains("Culinary Blog Jobs", await admin.Content.ReadAsStringAsync(), StringComparison.Ordinal);
    }
}
