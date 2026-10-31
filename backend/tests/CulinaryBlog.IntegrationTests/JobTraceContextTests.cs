// Culinary Blog. Authorship is recorded in the repository history.

using System.Diagnostics;
using System.Net.Http.Json;
using System.Text.Json;
using CulinaryBlog.Infrastructure.Email;
using CulinaryBlog.Infrastructure.Jobs;
using CulinaryBlog.Infrastructure.Persistence;
using Hangfire;
using Hangfire.Server;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace CulinaryBlog.IntegrationTests;

/// <summary>Checks persisted request context across outbox dispatch and a job consumer.</summary>
/// <param name="factory">Supplies isolated PostgreSQL and job storage.</param>
public sealed class JobTraceContextTests(AuthApiFactory factory) : IClassFixture<AuthApiFactory>
{
    /// <summary>Verifies the originating trace and correlation survive the database and job queue.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Fact]
    public async Task RequestContextSurvivesOutboxDispatchAndJobExecutionWithoutAmbientRequest()
    {
        using var listener = new ActivityListener
        {
            ShouldListenTo = source => source.Name == JobTraceContext.SourceName,
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllDataAndRecorded,
            SampleUsingParentId = (ref ActivityCreationOptions<string> _) => ActivitySamplingResult.AllDataAndRecorded,
        };
        ActivitySource.AddActivityListener(listener);
        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Correlation-ID", "g06-job-context");
        using var request = new Activity("synthetic-request").SetIdFormat(ActivityIdFormat.W3C).Start();
        client.DefaultRequestHeaders.Add("traceparent", request.Id);
        using var response = await client.PostAsJsonAsync("/api/v1/auth/register", new
        {
            email = $"trace-{Guid.NewGuid():N}@example.test",
            password = "Synthetic#Trace123",
            displayName = "Trace Author",
        });
        response.EnsureSuccessStatusCode();
        var userId = (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("data").GetProperty("user").GetProperty("id").GetString();
        await using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var outbox = await db.WelcomeEmailOutbox.AsNoTracking().SingleAsync(message => message.UserId == userId);
        Assert.Equal("g06-job-context", outbox.CorrelationId);
        Assert.True(ActivityContext.TryParse(outbox.TraceParent, null, out var original));
        Assert.Equal(request.TraceId, original.TraceId);
        request.Stop();

        using var dispatcher = new WelcomeEmailWorker(
            factory.Services.GetRequiredService<IServiceScopeFactory>(),
            Options.Create(new EmailOptions { Enabled = true }),
            TimeProvider.System,
            NullLogger<WelcomeEmailWorker>.Instance);
        await dispatcher.DispatchAsync(CancellationToken.None);
        var queued = await db.WelcomeEmailOutbox.AsNoTracking().SingleAsync(message => message.Id == outbox.Id);
        Assert.NotNull(queued.JobId);
        var storage = factory.Services.GetRequiredService<JobStorage>();
        using var connection = storage.GetConnection();
        Assert.Equal("g06-job-context", JsonSerializer.Deserialize<string>(connection.GetJobParameter(queued.JobId, "CorrelationId")));
        var parent = JsonSerializer.Deserialize<string>(connection.GetJobParameter(queued.JobId, "TraceParent"));
        Assert.True(ActivityContext.TryParse(parent, null, out var producer));
        Assert.Equal(original.TraceId, producer.TraceId);
        Assert.NotEqual(original.SpanId, producer.SpanId);

        var job = connection.GetJobData(queued.JobId);
        var background = new BackgroundJob(queued.JobId, job.Job, job.CreatedAt);
        var context = new PerformContext(storage, connection, background, new JobCancellationToken(false));
        var filter = factory.Services.GetRequiredService<JobLogContextFilter>();
        var previous = Activity.Current;
        filter.OnPerforming(new PerformingContext(context));
        try
        {
            Assert.NotNull(Activity.Current);
            Assert.Equal(original.TraceId, Activity.Current.TraceId);
            Assert.Equal(producer.SpanId, Activity.Current.ParentSpanId);
            Assert.Equal("g06-job-context", Activity.Current.GetBaggageItem("correlation.id"));
            Assert.Equal(ActivityKind.Consumer, Activity.Current.Kind);
        }
        finally
        {
            filter.OnPerformed(new PerformedContext(context, null, false, null));
        }

        Assert.Same(previous, Activity.Current);
    }
}
