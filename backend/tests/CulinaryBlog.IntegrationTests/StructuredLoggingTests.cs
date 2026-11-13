// Culinary Blog. Authorship is recorded in the repository history.

using System.Security.Claims;
using CulinaryBlog.Api.Logging;
using CulinaryBlog.Infrastructure.Persistence;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Core;
using Serilog.Events;
using Serilog.Extensions.Logging;

namespace CulinaryBlog.IntegrationTests;

/// <summary>Contains regression checks for structured logging tests.</summary>
/// <param name="factory">The factory supplied to this operation.</param>
public sealed class StructuredLoggingTests(AuthApiFactory factory) : IClassFixture<AuthApiFactory>
{
    /// <summary>Verifies that request logs have authenticated context and sink failure does not escape.</summary>
    [Fact]
    public void RequestLogsHaveAuthenticatedContextAndSinkFailureDoesNotEscape()
    {
        var context = new DefaultHttpContext { TraceIdentifier = "log-test-correlation", User = new ClaimsPrincipal(new ClaimsIdentity([new Claim("sub", "log-test-user")], "test")) };
        context.Request.Path = "/api/v1/auth/me";
        context.Request.Headers.Authorization = "Bearer must-not-log-token";
        var capture = new CaptureSink();
        using var log = new LoggerConfiguration()
            .Enrich.With(new RequestContextEnricher(new HttpContextAccessor { HttpContext = context }))
            .WriteTo.Sink(new FailedSink()).WriteTo.Sink(capture).CreateLogger();
        log.Information("Profile request completed");
        var entry = Assert.Single(capture.Events);
        Assert.Equal("log-test-correlation", ((ScalarValue)entry.Properties["CorrelationId"]).Value);
        Assert.Equal("log-test-user", ((ScalarValue)entry.Properties["UserId"]).Value);
        Assert.Equal("/api/v1/auth/me", ((ScalarValue)entry.Properties["RequestPath"]).Value);
        Assert.DoesNotContain("must-not-log-token", entry.ToString(), StringComparison.Ordinal);
    }

    /// <summary>Verifies that slow sql above100 ms logs duration and command id without sql or parameters.</summary>
    /// <returns>A task representing the operation and its result.</returns>
    [Fact]
    public async Task SlowSqlAbove100MsLogsDurationAndCommandIdWithoutSqlOrParameters()
    {
        await using var scope = factory.Services.CreateAsyncScope();
        var current = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var capture = new CaptureSink();
        using var log = new LoggerConfiguration().WriteTo.Sink(capture).CreateLogger();
        using var logs = new SerilogLoggerFactory(log, dispose: false);
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(current.Database.GetConnectionString())
            .AddInterceptors(new SlowQueryInterceptor(logs.CreateLogger<SlowQueryInterceptor>())).Options;
        await using var db = new AppDbContext(options);
        await db.Database.ExecuteSqlRawAsync("SELECT pg_sleep(0.15) /* sensitive-query-marker */");
        var warning = Assert.Single(capture.Events, entry => entry.Level == LogEventLevel.Warning);
        Assert.True((double)((ScalarValue)warning.Properties["ElapsedMilliseconds"]).Value! > 100);
        Assert.True(warning.Properties.ContainsKey("CommandId"));
        Assert.DoesNotContain("sensitive-query-marker", warning.ToString(), StringComparison.Ordinal);
    }

    /// <summary>Verifies that job filter adds job identity and disposes the log scope.</summary>
    [Fact]
    public void JobFilterAddsJobIdentityAndDisposesTheLogScope()
    {
        var storage = factory.Services.GetRequiredService<Hangfire.JobStorage>();
        using var connection = storage.GetConnection();
        var definition = Hangfire.Common.Job.FromExpression<CulinaryBlog.Infrastructure.Email.WelcomeEmailJob>(job => job.SendAsync(Guid.Empty, CancellationToken.None));
        var jobId = factory.Services.GetRequiredService<Hangfire.IBackgroundJobClient>().Create(definition, new Hangfire.States.EnqueuedState());
        var job = new Hangfire.BackgroundJob(jobId, definition, DateTime.UtcNow);
        var context = new Hangfire.Server.PerformContext(storage, connection, job, new Hangfire.JobCancellationToken(false));
        var capture = new CaptureSink();
        using var log = new LoggerConfiguration().Enrich.FromLogContext().WriteTo.Sink(capture).CreateLogger();
        using var logs = new SerilogLoggerFactory(log, dispose: false);
        var filter = new CulinaryBlog.Infrastructure.Jobs.JobLogContextFilter(logs.CreateLogger<CulinaryBlog.Infrastructure.Jobs.JobLogContextFilter>());
        filter.OnPerforming(new Hangfire.Server.PerformingContext(context));
        log.Information("Inside job");
        filter.OnPerformed(new Hangfire.Server.PerformedContext(context, null, false, null));
        log.Information("After job");
        Assert.Equal(jobId, ((ScalarValue)capture.Events[0].Properties["JobId"]).Value);
        Assert.Equal("job-" + jobId, ((ScalarValue)capture.Events[0].Properties["CorrelationId"]).Value);
        Assert.Equal("system", ((ScalarValue)capture.Events[0].Properties["UserId"]).Value);
        Assert.Contains("SendAsync", capture.Events[0].Properties["RequestPath"].ToString(), StringComparison.Ordinal);
        Assert.False(capture.Events[1].Properties.ContainsKey("JobId"));
    }

    private sealed class CaptureSink : ILogEventSink
    {
        public List<LogEvent> Events { get; } = [];

        public void Emit(LogEvent logEvent) => this.Events.Add(logEvent);
    }

    private sealed class FailedSink : ILogEventSink
    {
        public void Emit(LogEvent logEvent) => throw new IOException("Simulated log sink failure");
    }
}
