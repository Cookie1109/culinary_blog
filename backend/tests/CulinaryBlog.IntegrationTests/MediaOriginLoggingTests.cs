// Culinary Blog. Authorship is recorded in the repository history.

using System.Security.Claims;
using CulinaryBlog.Api.Logging;
using CulinaryBlog.Infrastructure.Jobs;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Core;
using Serilog.Events;
using Serilog.Extensions.Logging;

namespace CulinaryBlog.IntegrationTests;

/// <summary>Contains regression checks for media origin logging tests.</summary>
public sealed class MediaOriginLoggingTests
{
    private static readonly Action<Microsoft.Extensions.Logging.ILogger, Exception?> WriteTestLog =
        LoggerMessage.Define(LogLevel.Information, new EventId(1), "Test job context");

    /// <summary>Verifies that authenticated context uses both claim formats and survives an independent sink failure.</summary>
    /// <param name="claim">The claim supplied to this operation.</param>
    [Theory]
    [InlineData("sub")]
    [InlineData(ClaimTypes.NameIdentifier)]
    public void AuthenticatedContextUsesBothClaimFormatsAndSurvivesAnIndependentSinkFailure(string claim)
    {
        var context = new DefaultHttpContext
        {
            TraceIdentifier = "structured-request",
            User = new ClaimsPrincipal(new ClaimsIdentity([new Claim(claim, "actor-id")], "test")),
        };
        context.Request.Method = "POST";
        context.Request.Path = "/api/v1/auth/login";
        context.Request.QueryString = new QueryString("?token=synthetic-query-secret");
        context.Request.Headers.Authorization = "Bearer synthetic-header-secret";
        var capture = new CaptureSink();
        using var log = new LoggerConfiguration()
            .Enrich.With(new RequestContextEnricher(new HttpContextAccessor { HttpContext = context }))
            .WriteTo.Sink(new FailedSink()).WriteTo.Sink(capture).CreateLogger();
        log.Information("Request completed");
        var entry = Assert.Single(capture.Events);
        Assert.Equal("structured-request", Scalar(entry, "CorrelationId"));
        Assert.Equal("actor-id", Scalar(entry, "UserId"));
        Assert.Equal("POST", Scalar(entry, "RequestMethod"));
        Assert.Equal("/api/v1/auth/login", Scalar(entry, "RequestPath"));
        Assert.DoesNotContain("synthetic-header-secret", entry.ToString(), StringComparison.Ordinal);
        Assert.DoesNotContain("synthetic-query-secret", entry.ToString(), StringComparison.Ordinal);
    }

    /// <summary>Verifies that media context persists only origin metadata and scopes do not leak between jobs.</summary>
    [Fact]
    public void MediaContextPersistsOnlyOriginMetadataAndScopesDoNotLeakBetweenJobs()
    {
        var id = Guid.NewGuid();
        var message = new MediaOutboxMessage { Id = id, Type = "image.resize", Payload = $"{{\"ImageId\":\"{Guid.NewGuid()}\"}}" };
        var context = new DefaultHttpContext { TraceIdentifier = "upload-origin" };
        context.Request.Path = "/api/v1/recipes/recipe/images";
        context.Request.Headers.Authorization = "Bearer synthetic-header-secret";
        MediaJobLogContext.Capture(message, context, "upload-author");
        Assert.DoesNotContain("synthetic-header-secret", message.Payload, StringComparison.Ordinal);
        Assert.Contains("ImageId", message.Payload, StringComparison.Ordinal);
        var capture = new CaptureSink();
        using var log = new LoggerConfiguration().Enrich.FromLogContext().WriteTo.Sink(capture).CreateLogger();
        using var factory = new SerilogLoggerFactory(log, dispose: false);
        var logger = factory.CreateLogger("MediaJob");
        using (MediaJobLogContext.Begin(logger, message, id))
        {
            WriteTestLog(logger, null);
        }

        WriteTestLog(logger, null);
        using (MediaJobLogContext.Begin(logger, null, id))
        {
            WriteTestLog(logger, null);
        }

        Assert.Equal("upload-origin", Scalar(capture.Events[0], "CorrelationId"));
        Assert.Equal("upload-author", Scalar(capture.Events[0], "UserId"));
        Assert.Equal(id, Scalar(capture.Events[0], "OutboxId"));
        Assert.False(capture.Events[1].Properties.ContainsKey("CorrelationId"));
        Assert.Equal("system", Scalar(capture.Events[2], "UserId"));
        Assert.Equal($"outbox-{id:N}", Scalar(capture.Events[2], "CorrelationId"));
    }

    /// <summary>Verifies that invalid logging metadata does not interrupt payload error handling.</summary>
    /// <param name="payload">The payload supplied to this operation.</param>
    [Theory]
    [InlineData("{")]
    [InlineData("[]")]
    [InlineData("{\"MediaJobLogContext\":{\"UserId\":42}}")]
    public void InvalidLoggingMetadataDoesNotInterruptPayloadErrorHandling(string payload)
    {
        var capture = new CaptureSink();
        using var log = new LoggerConfiguration().Enrich.FromLogContext().WriteTo.Sink(capture).CreateLogger();
        using var factory = new SerilogLoggerFactory(log, dispose: false);
        var logger = factory.CreateLogger("MediaJob");
        var id = Guid.NewGuid();
        using (MediaJobLogContext.Begin(logger, new MediaOutboxMessage { Id = id, Type = "invalid", Payload = payload }, id))
        {
            WriteTestLog(logger, null);
        }

        Assert.Equal("system", Scalar(Assert.Single(capture.Events), "UserId"));
    }

    private static object? Scalar(LogEvent entry, string name) => ((ScalarValue)entry.Properties[name]).Value;

    private sealed class CaptureSink : ILogEventSink
    {
        public List<LogEvent> Events { get; } = [];

        public void Emit(LogEvent logEvent) => this.Events.Add(logEvent);
    }

    private sealed class FailedSink : ILogEventSink
    {
        public void Emit(LogEvent logEvent) => throw new IOException("Synthetic log sink failure");
    }
}
