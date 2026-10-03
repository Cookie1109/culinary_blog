using System.Security.Claims;
using CulinaryBlog.Api.Middleware;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace CulinaryBlog.IntegrationTests;

public sealed class AuditMutationMiddlewareTests
{
    [Fact]
    public async Task SuccessfulMutationWritesStructuredAuditFields()
    {
        var provider = new CapturingLoggerProvider();
        using var loggerFactory = LoggerFactory.Create(builder => builder.AddProvider(provider));
        var middleware = new AuditMutationMiddleware(
            context =>
            {
                context.Response.StatusCode = StatusCodes.Status201Created;
                context.Response.Headers.Location = "/api/v1/categories/baking";
                return Task.CompletedTask;
            },
            loggerFactory.CreateLogger<AuditMutationMiddleware>(),
            TimeProvider.System);
        var context = new DefaultHttpContext
        {
            TraceIdentifier = "audit-correlation-123",
            User = new ClaimsPrincipal(new ClaimsIdentity(
                [new Claim("sub", "user-123")],
                "test")),
        };
        context.Request.Method = HttpMethods.Post;
        context.Request.Path = "/api/v1/categories/";

        await middleware.InvokeAsync(context);

        var entry = Assert.Single(provider.Entries);
        Assert.Equal("category.created", entry["AuditEvent"]);
        Assert.Equal("category", entry["EntityType"]);
        Assert.Equal("baking", entry["EntityId"]);
        Assert.Equal("user-123", entry["UserId"]);
        Assert.Equal("audit-correlation-123", entry["CorrelationId"]);
        Assert.Equal("POST", entry["RequestMethod"]);
        Assert.Equal("/api/v1/categories/", entry["RequestPath"]);
        Assert.IsType<DateTimeOffset>(entry["OccurredAt"]);
    }

    private sealed class CapturingLoggerProvider : ILoggerProvider
    {
        public List<IReadOnlyDictionary<string, object?>> Entries { get; } = [];

        public ILogger CreateLogger(string categoryName) => new CapturingLogger(Entries);

        public void Dispose()
        {
        }
    }

    private sealed class CapturingLogger(List<IReadOnlyDictionary<string, object?>> entries) : ILogger
    {
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            var properties = Assert.IsAssignableFrom<IEnumerable<KeyValuePair<string, object?>>>(state);
            entries.Add(properties.ToDictionary(item => item.Key, item => item.Value, StringComparer.Ordinal));
        }
    }
}
