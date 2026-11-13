// Culinary Blog. Authorship is recorded in the repository history.

using Microsoft.AspNetCore.OutputCaching;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;

namespace CulinaryBlog.Infrastructure.Caching;

/// <summary>Represents the resilient output cache store.</summary>
/// <param name="store">The store supplied to this operation.</param>
/// <param name="connection">The connection supplied to this operation.</param>
/// <param name="logger">The logger supplied to this operation.</param>
internal sealed class ResilientOutputCacheStore(
    IOutputCacheStore store,
    IConnectionMultiplexer connection,
    ILogger<ResilientOutputCacheStore> logger) : IOutputCacheStore, IDisposable
{
    private static readonly Action<ILogger, string, Exception?> Unavailable = LoggerMessage.Define<string>(
        LogLevel.Warning,
        new EventId(5010, nameof(Unavailable)),
        "Redis shared cache {Operation} failed; continuing without cache");

    /// <summary>Gets the resilient output cache store.</summary>
    /// <param name="key">The key supplied to this operation.</param>
    /// <param name="cancellationToken">Cancels the operation when requested by the caller.</param>
    /// <returns>A task representing the operation and its result.</returns>
    public async ValueTask<byte[]?> GetAsync(string key, CancellationToken cancellationToken)
    {
        try
        {
            if (!connection.IsConnected)
            {
                throw new TimeoutException("Redis is disconnected.");
            }

            var value = await store.GetAsync(key, cancellationToken).ConfigureAwait(false);
            (value is null ? CacheMetrics.OutputMisses : CacheMetrics.OutputHits).Add(1);
            return value;
        }
        catch (Exception exception) when (IsUnavailable(exception, cancellationToken))
        {
            this.RecordFailure("get", exception);
            return null;
        }
    }

    /// <summary>Performs the set operation.</summary>
    /// <param name="key">The key supplied to this operation.</param>
    /// <param name="value">The value supplied to this operation.</param>
    /// <param name="tags">The tags supplied to this operation.</param>
    /// <param name="validFor">The valid for supplied to this operation.</param>
    /// <param name="cancellationToken">Cancels the operation when requested by the caller.</param>
    /// <returns>A task representing the operation and its result.</returns>
    public async ValueTask SetAsync(string key, byte[] value, string[]? tags, TimeSpan validFor, CancellationToken cancellationToken)
    {
        try
        {
            if (!connection.IsConnected)
            {
                throw new TimeoutException("Redis is disconnected.");
            }

            await store.SetAsync(key, value, tags, validFor, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception exception) when (IsUnavailable(exception, cancellationToken))
        {
            this.RecordFailure("set", exception);
        }
    }

    /// <summary>Performs the evict by tag operation.</summary>
    /// <param name="tag">The tag supplied to this operation.</param>
    /// <param name="cancellationToken">Cancels the operation when requested by the caller.</param>
    /// <returns>A task representing the operation and its result.</returns>
    public async ValueTask EvictByTagAsync(string tag, CancellationToken cancellationToken)
    {
        try
        {
            if (!connection.IsConnected)
            {
                throw new TimeoutException("Redis is disconnected.");
            }

            await store.EvictByTagAsync(tag, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception exception) when (IsUnavailable(exception, cancellationToken))
        {
            this.RecordFailure("evict", exception);
        }
    }

    /// <summary>Performs the dispose operation.</summary>
    public void Dispose() => (store as IDisposable)?.Dispose();

    private static bool IsUnavailable(Exception exception, CancellationToken token) =>
        !token.IsCancellationRequested && exception is RedisException or TimeoutException;

    private void RecordFailure(string operation, Exception exception)
    {
        if (operation == "evict")
        {
            CacheMetrics.InvalidationFailures.Add(1);
        }

        CacheMetrics.OperationFailures.Add(1, new KeyValuePair<string, object?>("operation", "output-" + operation));
        Unavailable(logger, operation, exception);
    }
}
