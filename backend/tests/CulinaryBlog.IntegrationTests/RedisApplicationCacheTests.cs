// Culinary Blog. Authorship is recorded in the repository history.

using CulinaryBlog.Application.Abstractions.Caching;
using CulinaryBlog.Infrastructure.Caching;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging.Abstractions;
using StackExchange.Redis;

namespace CulinaryBlog.IntegrationTests;

/// <summary>Contains regression checks for redis application cache tests.</summary>
public sealed class RedisApplicationCacheTests
{
    /// <summary>Verifies that redis outage is treated as cache miss and does not fail writes.</summary>
    /// <returns>A task representing the operation and its result.</returns>
    [Fact]
    public async Task RedisOutageIsTreatedAsCacheMissAndDoesNotFailWrites()
    {
        var cache = new RedisApplicationCache(
            new UnavailableDistributedCache(),
            null!,
            NullLogger<RedisApplicationCache>.Instance,
            null!,
            new FixedVersion());

        Assert.Null(await cache.GetAsync<string>("cb:v1:test"));
        await cache.SetAsync("cb:v1:test", "value", TimeSpan.FromMinutes(1), ["public:test"]);
        await cache.RemoveAsync("cb:v1:test");
    }

    private sealed class FixedVersion : IPublicCacheVersion
    {
        public Task<long> GetAsync(CancellationToken cancellationToken = default) => Task.FromResult(0L);

        public Task<long> RefreshAsync(CancellationToken cancellationToken = default) => Task.FromResult(0L);
    }

    private sealed class UnavailableDistributedCache : IDistributedCache
    {
        public byte[]? Get(string key) => throw Unavailable();

        public Task<byte[]?> GetAsync(string key, CancellationToken token = default) =>
            Task.FromException<byte[]?>(Unavailable());

        public void Refresh(string key) => throw Unavailable();

        public Task RefreshAsync(string key, CancellationToken token = default) =>
            Task.FromException(Unavailable());

        public void Remove(string key) => throw Unavailable();

        public Task RemoveAsync(string key, CancellationToken token = default) =>
            Task.FromException(Unavailable());

        public void Set(string key, byte[] value, DistributedCacheEntryOptions options) => throw Unavailable();

        public Task SetAsync(
            string key,
            byte[] value,
            DistributedCacheEntryOptions options,
            CancellationToken token = default) =>
            Task.FromException(Unavailable());

        private static RedisConnectionException Unavailable() =>
            new(
                ConnectionFailureType.UnableToConnect,
                CommandFlags.None,
                "Redis is unavailable for this test.",
                new InvalidOperationException("Simulated outage."),
                CommandStatus.Unknown);
    }
}
