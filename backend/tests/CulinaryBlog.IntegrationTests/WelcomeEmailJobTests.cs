// Culinary Blog. Authorship is recorded in the repository history.

using System.Net;
using System.Net.Http.Json;
using System.Net.Mail;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using CulinaryBlog.Infrastructure.Configuration;
using CulinaryBlog.Infrastructure.Email;
using CulinaryBlog.Infrastructure.Persistence;
using DotNet.Testcontainers.Builders;
using Hangfire;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace CulinaryBlog.IntegrationTests;

/// <summary>Contains regression checks for welcome email job tests.</summary>
/// <param name="factory">The factory supplied to this operation.</param>
public sealed class WelcomeEmailJobTests(AuthApiFactory factory) : IClassFixture<AuthApiFactory>
{
    /// <summary>Verifies that dispatch queues due outbox in hangfire once and leaves future retries pending.</summary>
    /// <returns>A task representing the operation and its result.</returns>
    [Fact]
    public async Task DispatchQueuesDueOutboxInHangfireOnceAndLeavesFutureRetriesPending()
    {
        var dueId = await this.RegisterAsync("Due Author");
        var futureId = await this.RegisterAsync("Future Author");
        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var future = await db.WelcomeEmailOutbox.SingleAsync(message => message.Id == futureId);
            future.NextAttemptAt = DateTimeOffset.UtcNow.AddDays(1);
            await db.SaveChangesAsync();
        }

        using var dispatcher = new WelcomeEmailWorker(
            factory.Services.GetRequiredService<IServiceScopeFactory>(),
            Options.Create(new EmailOptions { Enabled = true }),
            TimeProvider.System,
            NullLogger<WelcomeEmailWorker>.Instance);
        await Task.WhenAll(dispatcher.DispatchAsync(CancellationToken.None), dispatcher.DispatchAsync(CancellationToken.None));
        await using var checkScope = factory.Services.CreateAsyncScope();
        var context = checkScope.ServiceProvider.GetRequiredService<AppDbContext>();
        var due = await context.WelcomeEmailOutbox.AsNoTracking().SingleAsync(message => message.Id == dueId);
        Assert.False(string.IsNullOrWhiteSpace(due.JobId));
        Assert.Null((await context.WelcomeEmailOutbox.AsNoTracking().SingleAsync(message => message.Id == futureId)).JobId);
        using var connection = factory.Services.GetRequiredService<JobStorage>().GetConnection();
        var job = connection.GetJobData(due.JobId);
        Assert.NotNull(job);
        Assert.Equal(typeof(WelcomeEmailJob), job.Job.Type);
        Assert.Equal(nameof(WelcomeEmailJob.SendAsync), job.Job.Method.Name);
        Assert.Equal(dueId, job.Job.Args[0]);
        await dispatcher.DispatchAsync(CancellationToken.None);
        Assert.Equal(due.JobId, (await context.WelcomeEmailOutbox.AsNoTracking().SingleAsync(message => message.Id == dueId)).JobId);
    }

    /// <summary>Verifies that welcome email is html with safe name and trusted application link and concurrent jobs send once.</summary>
    /// <returns>A task representing the operation and its result.</returns>
    [Fact]
    public async Task WelcomeEmailIsHtmlWithSafeNameAndTrustedApplicationLinkAndConcurrentJobsSendOnce()
    {
        await using var mail = new ContainerBuilder("mailhog/mailhog:v1.0.1@sha256:8d76a3d4ffa32a3661311944007a415332c4bb855657f4f6c57996405c009bea")
            .WithPortBinding(1025, true)
            .WithPortBinding(8025, true)
            .WithWaitStrategy(Wait.ForUnixContainer().UntilHttpRequestIsSucceeded(request => request.ForPort(8025).ForPath("/api/v2/messages")))
            .Build();
        await mail.StartAsync();
        var id = await this.RegisterAsync("<Chef & Author>");
        var email = Options.Create(new EmailOptions { Enabled = true, Host = mail.Hostname, Port = mail.GetMappedPublicPort(1025) });
        await using var firstScope = factory.Services.CreateAsyncScope();
        await using var secondScope = factory.Services.CreateAsyncScope();
        await Task.WhenAll(
            CreateJob(firstScope.ServiceProvider, email).SendAsync(id, CancellationToken.None),
            CreateJob(secondScope.ServiceProvider, email).SendAsync(id, CancellationToken.None));

        using var client = new HttpClient();
        using var messages = JsonDocument.Parse(await client.GetStringAsync(new Uri($"http://{mail.Hostname}:{mail.GetMappedPublicPort(8025)}/api/v2/messages")));
        var content = Assert.Single(messages.RootElement.GetProperty("items").EnumerateArray()).GetProperty("Content");
        Assert.Contains("text/html", content.GetProperty("Headers").GetProperty("Content-Type")[0].GetString(), StringComparison.Ordinal);
        var body = Encoding.UTF8.GetString(Convert.FromBase64String(content.GetProperty("Body").GetString()!));
        Assert.Contains(WebUtility.HtmlEncode("<Chef & Author>"), body, StringComparison.Ordinal);
        Assert.DoesNotContain("<Chef & Author>", body, StringComparison.Ordinal);
        Assert.Contains("href=\"https://recipes.example.test/\"", body, StringComparison.Ordinal);
        await using var checkScope = factory.Services.CreateAsyncScope();
        var message = await checkScope.ServiceProvider.GetRequiredService<AppDbContext>().WelcomeEmailOutbox.SingleAsync(item => item.Id == id);
        Assert.NotNull(message.SentAt);
        Assert.Null(message.LastError);
    }

    /// <summary>Verifies that smtp failure keeps registration and persists retry schedule and stops after four attempts.</summary>
    /// <returns>A task representing the operation and its result.</returns>
    [Fact]
    public async Task SmtpFailureKeepsRegistrationAndPersistsRetryScheduleAndStopsAfterFourAttempts()
    {
        var id = await this.RegisterAsync("Retry Author");

        // Close a reserved local port so SMTP fails immediately without contacting
        // an external mail server.
        var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        var port = ((IPEndPoint)listener.LocalEndpoint).Port;
        listener.Stop();
        var email = Options.Create(new EmailOptions { Enabled = true, Host = "127.0.0.1", Port = port });
        for (var attempt = 1; attempt <= 4; attempt++)
        {
            await using var scope = factory.Services.CreateAsyncScope();
            var before = DateTimeOffset.UtcNow;
            await Assert.ThrowsAsync<SmtpException>(() => CreateJob(scope.ServiceProvider, email).SendAsync(id, CancellationToken.None));
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var message = await db.WelcomeEmailOutbox.AsNoTracking().SingleAsync(item => item.Id == id);
            var delay = attempt switch { 1 => 1, 2 => 5, _ => 30 };
            Assert.Equal(attempt, message.Attempts);
            Assert.Null(message.SentAt);
            Assert.False(string.IsNullOrEmpty(message.LastError));
            Assert.InRange(message.NextAttemptAt, before.AddMinutes(delay), DateTimeOffset.UtcNow.AddMinutes(delay));
            Assert.True(await db.Users.AnyAsync(user => user.Id == message.UserId));
        }

        await using var finalScope = factory.Services.CreateAsyncScope();
        await CreateJob(finalScope.ServiceProvider, email).SendAsync(id, CancellationToken.None);
        Assert.Equal(4, (await finalScope.ServiceProvider.GetRequiredService<AppDbContext>().WelcomeEmailOutbox.SingleAsync(item => item.Id == id)).Attempts);
    }

    private static WelcomeEmailJob CreateJob(IServiceProvider services, IOptions<EmailOptions> email) => new(
        services.GetRequiredService<AppDbContext>(),
        email,
        Options.Create(new SitemapOptions { PublicBaseUrl = "https://recipes.example.test" }),
        TimeProvider.System,
        NullLogger<WelcomeEmailJob>.Instance);

    private async Task<Guid> RegisterAsync(string name)
    {
        using var client = factory.CreateClient();
        var email = $"welcome-{Guid.NewGuid():N}@example.test";
        using var response = await client.PostAsJsonAsync("/api/v1/auth/register", new { displayName = name, email, password = "Valid#Password1" });
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        await using var scope = factory.Services.CreateAsyncScope();
        return await scope.ServiceProvider.GetRequiredService<AppDbContext>().WelcomeEmailOutbox.Where(message => message.Recipient == email).Select(message => message.Id).SingleAsync();
    }
}
