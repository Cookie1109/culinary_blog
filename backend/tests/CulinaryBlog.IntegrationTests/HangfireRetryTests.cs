// Culinary Blog. Authorship is recorded in the repository history.

using System.Net.Http.Json;
using System.Text.Json;
using CulinaryBlog.Infrastructure.Email;
using CulinaryBlog.Infrastructure.Persistence;
using Hangfire.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace CulinaryBlog.IntegrationTests;

/// <summary>Contains regression checks for hangfire retry tests.</summary>
/// <param name="factory">The factory supplied to this operation.</param>
public sealed class HangfireRetryTests(AuthApiFactory factory) : IClassFixture<AuthApiFactory>
{
    /// <summary>Verifies that welcome job persists scheduled retries across worker restart and ends in failed state.</summary>
    /// <returns>A task representing the operation and its result.</returns>
    [Fact]
    public async Task WelcomeJobPersistsScheduledRetriesAcrossWorkerRestartAndEndsInFailedState()
    {
        await using var custom = factory.WithWebHostBuilder(builder =>
        {
            builder.UseSetting("Email:Enabled", "true");
            builder.UseSetting("Email:Host", "127.0.0.1");
            builder.UseSetting("Email:Port", "1");
        });
        using var client = custom.CreateClient();
        var email = $"retry-{Guid.NewGuid():N}@example.test";
        using var response = await client.PostAsJsonAsync("/api/v1/auth/register", new { email, password = "Valid#Password1", displayName = "Retry Author" });
        response.EnsureSuccessStatusCode();
        var userId = (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("data").GetProperty("user").GetProperty("id").GetString();
        Guid outboxId;
        await using (var scope = custom.Services.CreateAsyncScope())
        {
            outboxId = await scope.ServiceProvider.GetRequiredService<AppDbContext>().WelcomeEmailOutbox.Where(item => item.UserId == userId).Select(item => item.Id).SingleAsync();
        }

        await HangfireRetryProbe.FailAsync(
            custom.Services,
            Job.FromExpression<WelcomeEmailJob>(job => job.SendAsync(outboxId, CancellationToken.None)),
            "g04-welcome");
        await using var verify = custom.Services.CreateAsyncScope();
        var context = verify.ServiceProvider.GetRequiredService<AppDbContext>();
        var message = await context.WelcomeEmailOutbox.AsNoTracking().SingleAsync(item => item.Id == outboxId);
        Assert.Equal(4, message.Attempts);
        Assert.Null(message.SentAt);
        Assert.NotNull(message.LastError);
        Assert.True(await context.Users.AnyAsync(user => user.Id == userId));
    }
}
