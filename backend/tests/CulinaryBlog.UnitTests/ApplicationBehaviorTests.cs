// Culinary Blog. Authorship is recorded in the repository history.

using CulinaryBlog.Application;
using CulinaryBlog.Application.Abstractions.Caching;
using CulinaryBlog.Application.Behaviors;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;

namespace CulinaryBlog.UnitTests;

/// <summary>Contains regression checks for application behavior tests.</summary>
public sealed class ApplicationBehaviorTests
{
    /// <summary>Verifies that validation behavior runs next when valid and throws when invalid.</summary>
    /// <returns>A task representing the operation and its result.</returns>
    [Fact]
    public async Task ValidationBehaviorRunsNextWhenValidAndThrowsWhenInvalid()
    {
        var valid = new ValidationBehavior<TestRequest, string>([new TestRequestValidator()]);
        var withoutValidators = new ValidationBehavior<TestRequest, string>([]);

        Assert.Equal("ok", await valid.Handle(new TestRequest("value"), _ => Task.FromResult("ok"), default));
        Assert.Equal("ok", await withoutValidators.Handle(new TestRequest(string.Empty), _ => Task.FromResult("ok"), default));
        await Assert.ThrowsAsync<ValidationException>(() =>
            valid.Handle(new TestRequest(string.Empty), _ => Task.FromResult("unreachable"), default));
    }

    /// <summary>Verifies that caching behavior uses cached value or stores handler result.</summary>
    /// <returns>A task representing the operation and its result.</returns>
    [Fact]
    public async Task CachingBehaviorUsesCachedValueOrStoresHandlerResult()
    {
        var cache = new StubCache();
        var behavior = new CachingBehavior<CacheableTestRequest, string>(cache);
        var calls = 0;

        var first = await behavior.Handle(
            new CacheableTestRequest(),
            _ =>
            {
                calls++;
                return Task.FromResult("fresh");
            },
            default);
        var second = await behavior.Handle(
            new CacheableTestRequest(),
            _ =>
            {
                calls++;
                return Task.FromResult("unexpected");
            },
            default);

        Assert.Equal("fresh", first);
        Assert.Equal("fresh", second);
        Assert.Equal(1, calls);
        Assert.Equal(1, cache.SetCalls);
        Assert.Equal(["test-tag"], cache.LastTags);
    }

    /// <summary>Verifies that caching behavior bypasses non cacheable requests.</summary>
    /// <returns>A task representing the operation and its result.</returns>
    [Fact]
    public async Task CachingBehaviorBypassesNonCacheableRequests()
    {
        var cache = new StubCache { Value = "cached" };
        var behavior = new CachingBehavior<TestRequest, string>(cache);

        var response = await behavior.Handle(
            new TestRequest("value"),
            _ => Task.FromResult("fresh"),
            default);
        Assert.Equal("fresh", response);
    }

    /// <summary>Verifies that logging behavior returns responses and rethrows failures.</summary>
    /// <returns>A task representing the operation and its result.</returns>
    [Fact]
    public async Task LoggingBehaviorReturnsResponsesAndRethrowsFailures()
    {
        var behavior = new LoggingBehavior<TestRequest, string>(
            NullLogger<LoggingBehavior<TestRequest, string>>.Instance);

        Assert.Equal("ok", await behavior.Handle(new TestRequest("value"), _ => Task.FromResult("ok"), default));
        await Assert.ThrowsAsync<InvalidOperationException>(() => behavior.Handle(
            new TestRequest("value"),
            _ => Task.FromException<string>(new InvalidOperationException("failed")),
            default));
    }

    /// <summary>Verifies that performance behavior returns handler response.</summary>
    /// <returns>A task representing the operation and its result.</returns>
    [Fact]
    public async Task PerformanceBehaviorReturnsHandlerResponse()
    {
        var behavior = new PerformanceBehavior<TestRequest, string>(
            NullLogger<PerformanceBehavior<TestRequest, string>>.Instance);

        Assert.Equal("ok", await behavior.Handle(new TestRequest("value"), _ => Task.FromResult("ok"), default));
    }

    /// <summary>Verifies that sensitive requests are not serialized and slow pipeline warns.</summary>
    /// <returns>A task representing the operation and its result.</returns>
    [Fact]
    public async Task SensitiveRequestsAreNotSerializedAndSlowPipelineWarns()
    {
        var log = new CapturingLogger<CulinaryBlog.Application.Auth.LoginCommand>();
        var request = new CulinaryBlog.Application.Auth.LoginCommand(new CulinaryBlog.Application.Auth.LoginRequest("private@example.test", "DoNotLog#Password1"), null, null);
        var logging = new LoggingBehavior<CulinaryBlog.Application.Auth.LoginCommand, string>(log);
        var performance = new PerformanceBehavior<CulinaryBlog.Application.Auth.LoginCommand, string>(log);
        var response = await logging.Handle(
            request,
            cancellationToken => performance.Handle(
                request,
                async _ =>
                {
                    await Task.Delay(550, cancellationToken);
                    return "ok";
                },
                cancellationToken),
            default);
        Assert.Equal("ok", response);
        Assert.Contains(log.Messages, item => item.Level == Microsoft.Extensions.Logging.LogLevel.Warning && item.Message.Contains("Slow application request", StringComparison.Ordinal));
        Assert.All(log.Messages, item =>
        {
            Assert.DoesNotContain("DoNotLog", item.Message, StringComparison.Ordinal);
            Assert.DoesNotContain("private@example.test", item.Message, StringComparison.Ordinal);
        });
    }

    /// <summary>Verifies that application services register validators and pipeline behaviors.</summary>
    [Fact]
    public void ApplicationServicesRegisterValidatorsAndPipelineBehaviors()
    {
        var services = new ServiceCollection();

        services.AddApplication();

        Assert.Contains(services, descriptor => descriptor.ServiceType == typeof(IPipelineBehavior<,>));
        Assert.Contains(services, descriptor => descriptor.ServiceType.IsGenericType &&
            descriptor.ServiceType.GetGenericTypeDefinition() == typeof(IValidator<>));
    }

    private sealed class CapturingLogger<T> : Microsoft.Extensions.Logging.ILogger<LoggingBehavior<T, string>>, Microsoft.Extensions.Logging.ILogger<PerformanceBehavior<T, string>>
        where T : notnull
    {
        public List<(Microsoft.Extensions.Logging.LogLevel Level, string Message)> Messages { get; } = [];

        public IDisposable? BeginScope<TState>(TState state)
            where TState : notnull
            => null;

        public bool IsEnabled(Microsoft.Extensions.Logging.LogLevel logLevel) => true;

        public void Log<TState>(Microsoft.Extensions.Logging.LogLevel logLevel, Microsoft.Extensions.Logging.EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter) => this.Messages.Add((logLevel, formatter(state, exception)));
    }

    private sealed record TestRequest(string Value);

    private sealed record CacheableTestRequest : ICacheableRequest<string>
    {
        public string CacheKey => "test";

        public TimeSpan CacheDuration => TimeSpan.FromMinutes(1);

        public IReadOnlyCollection<string> CacheTags => ["test-tag"];
    }

    private sealed class TestRequestValidator : AbstractValidator<TestRequest>
    {
        public TestRequestValidator()
        {
            this.RuleFor(request => request.Value).NotEmpty();
        }
    }

    private sealed class StubCache : IApplicationCache
    {
        public object? Value { get; set; }

        public int SetCalls { get; private set; }

        public IReadOnlyCollection<string> LastTags { get; private set; } = [];

        public Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default) =>
            Task.FromResult((T?)this.Value);

        public Task SetAsync<T>(
            string key,
            T value,
            TimeSpan absoluteExpiration,
            IReadOnlyCollection<string> tags,
            CancellationToken cancellationToken = default)
        {
            this.Value = value;
            this.SetCalls++;
            this.LastTags = tags;
            return Task.CompletedTask;
        }

        public Task RemoveAsync(string key, CancellationToken cancellationToken = default)
        {
            this.Value = null;
            return Task.CompletedTask;
        }

        public Task RemoveByTagAsync(string tag, CancellationToken cancellationToken = default)
        {
            this.Value = null;
            return Task.CompletedTask;
        }
    }
}
