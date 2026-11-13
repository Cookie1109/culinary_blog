// Culinary Blog. Authorship is recorded in the repository history.

using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Text.Json;
using CulinaryBlog.Application.Abstractions.Caching;
using Microsoft.AspNetCore.OutputCaching;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;

namespace CulinaryBlog.Infrastructure.Caching;

/// <summary>Represents the redis application cache.</summary>
/// <param name="cache">The cache supplied to this operation.</param>
/// <param name="connection">The connection supplied to this operation.</param>
/// <param name="logger">The logger supplied to this operation.</param>
/// <param name="outputCache">The output cache supplied to this operation.</param>
/// <param name="version">The recipe version used to reject concurrent changes.</param>
internal sealed class RedisApplicationCache(
    IDistributedCache cache,
    IConnectionMultiplexer connection,
    ILogger<RedisApplicationCache> logger,
    IOutputCacheStore outputCache,
    IPublicCacheVersion version) : IApplicationCache
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);
    private static readonly Action<ILogger, string, Exception?> CacheUnavailable =
        LoggerMessage.Define<string>(
            LogLevel.Warning,
            new EventId(5001, nameof(CacheUnavailable)),
            "Redis cache operation {Operation} failed; continuing without cache");

    /// <summary>Gets the redis application cache.</summary>
    /// <param name="key">The key supplied to this operation.</param>
    /// <param name="cancellationToken">Cancels the operation when requested by the caller.</param>
    /// <typeparam name="T">The  type.</typeparam>
    /// <returns>A task representing the operation and its result.</returns>
    public async Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        try
        {
            key = DataKey(key, await version.GetAsync(cancellationToken).ConfigureAwait(false));
            if (this.IsDisconnected())
            {
                CacheMetrics.Misses.Add(1);
                CacheMetrics.OperationFailures.Add(1, new KeyValuePair<string, object?>("operation", "get"));
                CacheUnavailable(logger, "get", null);
                return default;
            }

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

    /// <summary>Performs the set operation.</summary>
    /// <param name="key">The key supplied to this operation.</param>
    /// <param name="value">The value supplied to this operation.</param>
    /// <param name="absoluteExpiration">The absolute expiration supplied to this operation.</param>
    /// <param name="tags">The tags supplied to this operation.</param>
    /// <param name="cancellationToken">Cancels the operation when requested by the caller.</param>
    /// <typeparam name="T">The  type.</typeparam>
    /// <returns>A task representing the operation and its result.</returns>
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
            if (this.IsDisconnected())
            {
                CacheMetrics.OperationFailures.Add(1, new KeyValuePair<string, object?>("operation", "set"));
                CacheUnavailable(logger, "set", null);
                return;
            }

            key = DataKey(key, await version.GetAsync(cancellationToken).ConfigureAwait(false));
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
            await this.TryRemoveAfterFailedTagRegistrationAsync(key).ConfigureAwait(false);
        }
        finally
        {
            CacheMetrics.Latency.Record(
                stopwatch.Elapsed.TotalMilliseconds,
                new KeyValuePair<string, object?>("operation", "set"));
        }
    }

    /// <summary>Removes the redis application cache.</summary>
    /// <param name="key">The key supplied to this operation.</param>
    /// <param name="cancellationToken">Cancels the operation when requested by the caller.</param>
    /// <returns>A task representing the operation and its result.</returns>
    public async Task RemoveAsync(string key, CancellationToken cancellationToken = default)
    {
        try
        {
            if (this.IsDisconnected())
            {
                CacheMetrics.InvalidationFailures.Add(1);
                CacheUnavailable(logger, "remove", null);
                return;
            }

            key = DataKey(key, await version.GetAsync(cancellationToken).ConfigureAwait(false));
            await cache.RemoveAsync(key, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception exception) when (IsRedisFailure(exception, cancellationToken))
        {
            CacheMetrics.InvalidationFailures.Add(1);
            CacheUnavailable(logger, "remove", exception);
        }
    }

    /// <summary>Removes by tag.</summary>
    /// <param name="tag">The tag supplied to this operation.</param>
    /// <param name="cancellationToken">Cancels the operation when requested by the caller.</param>
    /// <returns>A task representing the operation and its result.</returns>
    public async Task RemoveByTagAsync(string tag, CancellationToken cancellationToken = default)
    {
        await outputCache.EvictByTagAsync(tag, cancellationToken).ConfigureAwait(false);
        try
        {
            if (this.IsDisconnected())
            {
                CacheMetrics.InvalidationFailures.Add(1);
                CacheUnavailable(logger, "remove-by-tag", null);
                return;
            }

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

    /// <summary>Performs the data key operation.</summary>
    /// <param name="key">The key supplied to this operation.</param>
    /// <param name="generation">The generation supplied to this operation.</param>
    /// <returns>The data key result.</returns>
    internal static string DataKey(string key, long generation) => $"{key}:generation={generation}";

    private static string GetTagKey(string tag) => $"culinary:v1:tag:{tag}";

    private static bool IsRedisFailure(Exception exception, CancellationToken cancellationToken) =>
        !cancellationToken.IsCancellationRequested && exception is RedisException or TimeoutException;

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

    private bool IsDisconnected() => connection is not null && !connection.IsConnected;
}
