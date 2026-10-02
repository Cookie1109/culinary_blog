using CulinaryBlog.Infrastructure.Caching;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging.Abstractions;
using StackExchange.Redis;

namespace CulinaryBlog.IntegrationTests;

public sealed class RedisApplicationCacheTests
{
    [Fact]
    public async Task RedisOutageIsTreatedAsCacheMissAndDoesNotFailWrites()
    {
        var cache = new RedisApplicationCache(
            new UnavailableDistributedCache(),
            null!,
            NullLogger<RedisApplicationCache>.Instance);

        Assert.Null(await cache.GetAsync<string>("cb:v1:test"));
        await cache.SetAsync("cb:v1:test", "value", TimeSpan.FromMinutes(1), ["public:test"]);
        await cache.RemoveAsync("cb:v1:test");
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
