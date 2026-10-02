using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Text.Json;
using CulinaryBlog.Application.Abstractions.Caching;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;

namespace CulinaryBlog.Infrastructure.Caching;

internal sealed class RedisApplicationCache(
    IDistributedCache cache,
    IConnectionMultiplexer connection,
    ILogger<RedisApplicationCache> logger) : IApplicationCache
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);
    private static readonly Action<ILogger, string, Exception?> CacheUnavailable =
        LoggerMessage.Define<string>(
            LogLevel.Warning,
            new EventId(5001, nameof(CacheUnavailable)),
            "Redis cache operation {Operation} failed; continuing without cache");

    public async Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        try
        {
            var payload = await cache.GetStringAsync(key, cancellationToken).ConfigureAwait(false);
            if (payload is null)
            {
                CacheMetrics.Misses.Add(1);
                return default;
            }

            CacheMetrics.Hits.Add(1);
            return JsonSerializer.Deserialize<T>(payload, SerializerOptions);
        }
        catch (Exception exception) when (IsRedisFailure(exception, cancellationToken))
        {
            CacheMetrics.Misses.Add(1);
            CacheMetrics.OperationFailures.Add(1, new KeyValuePair<string, object?>("operation", "get"));
            CacheUnavailable(logger, "get", exception);
            return default;
        }
        finally
        {
            CacheMetrics.Latency.Record(
                stopwatch.Elapsed.TotalMilliseconds,
                new KeyValuePair<string, object?>("operation", "get"));
        }
    }

    public async Task SetAsync<T>(
        string key,
        T value,
        TimeSpan absoluteExpiration,
        IReadOnlyCollection<string> tags,
        CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        try
        {
            var payload = JsonSerializer.Serialize(value, SerializerOptions);
            await cache.SetStringAsync(
                key,
                payload,
                new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = absoluteExpiration },
                cancellationToken).ConfigureAwait(false);

            var database = connection.GetDatabase();
            foreach (var tag in tags.Distinct(StringComparer.Ordinal))
            {
                var tagKey = GetTagKey(tag);
                await database.SetAddAsync(tagKey, key).ConfigureAwait(false);
                await database.KeyExpireAsync(tagKey, TimeSpan.FromHours(1)).ConfigureAwait(false);
            }
        }
        catch (Exception exception) when (IsRedisFailure(exception, cancellationToken))
        {
            CacheMetrics.OperationFailures.Add(1, new KeyValuePair<string, object?>("operation", "set"));
            CacheUnavailable(logger, "set", exception);
            await TryRemoveAfterFailedTagRegistrationAsync(key).ConfigureAwait(false);
        }
        finally
        {
            CacheMetrics.Latency.Record(
                stopwatch.Elapsed.TotalMilliseconds,
                new KeyValuePair<string, object?>("operation", "set"));
        }
    }

    public async Task RemoveAsync(string key, CancellationToken cancellationToken = default)
    {
        try
        {
            await cache.RemoveAsync(key, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception exception) when (IsRedisFailure(exception, cancellationToken))
        {
            CacheMetrics.InvalidationFailures.Add(1);
            CacheUnavailable(logger, "remove", exception);
        }
    }

    public async Task RemoveByTagAsync(string tag, CancellationToken cancellationToken = default)
    {
        try
        {
            var database = connection.GetDatabase();
            var tagKey = GetTagKey(tag);
            var members = await database.SetMembersAsync(tagKey).ConfigureAwait(false);
            foreach (var member in members)
            {
                await cache.RemoveAsync(member.ToString(), cancellationToken).ConfigureAwait(false);
            }

            await database.KeyDeleteAsync(tagKey).ConfigureAwait(false);
        }
        catch (Exception exception) when (IsRedisFailure(exception, cancellationToken))
        {
            CacheMetrics.InvalidationFailures.Add(1);
            CacheUnavailable(logger, "remove-by-tag", exception);
        }
    }

    private async Task TryRemoveAfterFailedTagRegistrationAsync(string key)
    {
        try
        {
            await cache.RemoveAsync(key).ConfigureAwait(false);
        }
        catch (Exception)
        {
            // The cache is already unavailable. TTL remains the final safety net.
        }
    }

    private static string GetTagKey(string tag) => $"culinary:v1:tag:{tag}";

    private static bool IsRedisFailure(Exception exception, CancellationToken cancellationToken) =>
        !cancellationToken.IsCancellationRequested && exception is RedisException or TimeoutException;
}

public static class CacheMetrics
{
    public const string MeterName = "CulinaryBlog.Cache";

    private static readonly Meter Meter = new(MeterName);

    internal static readonly Counter<long> Hits = Meter.CreateCounter<long>("cache.hits");
    internal static readonly Counter<long> Misses = Meter.CreateCounter<long>("cache.misses");
    internal static readonly Counter<long> OperationFailures = Meter.CreateCounter<long>("cache.operation.failures");
    internal static readonly Counter<long> InvalidationFailures = Meter.CreateCounter<long>("cache.invalidation.failures");
    internal static readonly Histogram<double> Latency = Meter.CreateHistogram<double>("cache.operation.duration", "ms");
}
