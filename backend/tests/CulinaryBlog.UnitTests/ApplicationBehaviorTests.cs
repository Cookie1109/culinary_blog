using CulinaryBlog.Application;
using CulinaryBlog.Application.Abstractions.Caching;
using CulinaryBlog.Application.Behaviors;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;

namespace CulinaryBlog.UnitTests;

public sealed class ApplicationBehaviorTests
{
    [Fact]
    public async Task ValidationBehaviorRunsNextWhenValidAndThrowsWhenInvalid()
    {
        var valid = new ValidationBehavior<TestRequest, string>([new TestRequestValidator()]);
        var withoutValidators = new ValidationBehavior<TestRequest, string>([]);

        Assert.Equal("ok", await valid.Handle(new TestRequest("value"), _ => Task.FromResult("ok"), default));
        Assert.Equal("ok", await withoutValidators.Handle(new TestRequest(""), _ => Task.FromResult("ok"), default));
        await Assert.ThrowsAsync<ValidationException>(() =>
            valid.Handle(new TestRequest(string.Empty), _ => Task.FromResult("unreachable"), default));
    }

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
    }

    [Fact]
    public async Task CachingBehaviorBypassesNonCacheableRequests()
    {
        var cache = new StubCache { Value = "cached" };
        var behavior = new CachingBehavior<TestRequest, string>(cache);

        Assert.Equal("fresh", await behavior.Handle(
            new TestRequest("value"),
            _ => Task.FromResult("fresh"),
            default));
    }

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

    [Fact]
    public async Task PerformanceBehaviorReturnsHandlerResponse()
    {
        var behavior = new PerformanceBehavior<TestRequest, string>(
            NullLogger<PerformanceBehavior<TestRequest, string>>.Instance);

        Assert.Equal("ok", await behavior.Handle(new TestRequest("value"), _ => Task.FromResult("ok"), default));
    }

    [Fact]
    public void ApplicationServicesRegisterValidatorsAndPipelineBehaviors()
    {
        var services = new ServiceCollection();

        services.AddApplication();

        Assert.Contains(services, descriptor => descriptor.ServiceType == typeof(IPipelineBehavior<,>));
        Assert.Contains(services, descriptor => descriptor.ServiceType.IsGenericType &&
            descriptor.ServiceType.GetGenericTypeDefinition() == typeof(IValidator<>));
    }

    private sealed record TestRequest(string Value);

    private sealed record CacheableTestRequest : ICacheableRequest<string>
    {
        public string CacheKey => "test";

        public TimeSpan CacheDuration => TimeSpan.FromMinutes(1);
    }

    private sealed class TestRequestValidator : AbstractValidator<TestRequest>
    {
        public TestRequestValidator()
        {
            RuleFor(request => request.Value).NotEmpty();
        }
    }

    private sealed class StubCache : IApplicationCache
    {
        public object? Value { get; set; }

        public int SetCalls { get; private set; }

        public Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default) =>
            Task.FromResult((T?)Value);

        public Task SetAsync<T>(
            string key,
            T value,
            TimeSpan absoluteExpiration,
            CancellationToken cancellationToken = default)
        {
            Value = value;
            SetCalls++;
            return Task.CompletedTask;
        }

        public Task RemoveAsync(string key, CancellationToken cancellationToken = default)
        {
            Value = null;
            return Task.CompletedTask;
        }
    }
}
