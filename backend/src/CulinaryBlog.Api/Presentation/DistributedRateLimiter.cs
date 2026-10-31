// Culinary Blog. Authorship is recorded in the repository history.

using System.Globalization;
using System.Net;
using System.Threading.RateLimiting;
using CulinaryBlog.Infrastructure.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;
using StackExchange.Redis;

namespace CulinaryBlog.Api.Presentation;

/// <summary>Represents the distributed rate limiter.</summary>
/// <param name="redis">The redis supplied to this operation.</param>
/// <param name="fallback">The fallback supplied to this operation.</param>
/// <param name="configuration">The configuration supplied to this operation.</param>
/// <param name="logger">The logger supplied to this operation.</param>
internal sealed class DistributedRateLimiter(
    IConnectionMultiplexer redis,
    PostgresRateLimitCounter fallback,
    IConfiguration configuration,
    ILogger<DistributedRateLimiter> logger) : PartitionedRateLimiter<HttpContext>
{
    // One atomic operation consumes the global fixed window and the endpoint window.
    // PostgreSQL's shared clock/ledger protects failover; Redis mirrors the same admissions.
    private const string AcquireScript = """
        local now = tonumber(ARGV[5])
        local window = math.floor(now / 60000)
        local resetMs = (window + 1) * 60000
        local function fixed(key, limit)
          local count = 0
          if tonumber(redis.call('HGET', key, 'window')) == window then
            count = tonumber(redis.call('HGET', key, 'count')) or 0
          end
          count = math.min(count + 1, limit + 1)
          redis.call('HSET', key, 'window', window, 'count', count)
          redis.call('PEXPIRE', key, 120000)
          return count
        end
        local limit = tonumber(ARGV[1])
        local count = fixed(KEYS[1], limit)
        local remaining = math.max(0, limit - count)
        local allowed = count <= limit
        if ARGV[2] ~= '' and allowed then
          local endpointLimit = tonumber(ARGV[3])
          local endpointCount
          local endpointResetMs = resetMs
          if ARGV[2] == 'auth' then
            redis.call('ZREMRANGEBYSCORE', KEYS[2], '-inf', now - 60000)
            endpointCount = redis.call('ZCARD', KEYS[2])
            if endpointCount < endpointLimit then
              redis.call('ZADD', KEYS[2], now, ARGV[4])
              endpointCount = endpointCount + 1
            else
              allowed = false
            end
            redis.call('PEXPIRE', KEYS[2], 60000)
            local first = redis.call('ZRANGE', KEYS[2], 0, 0, 'WITHSCORES')
            endpointResetMs = tonumber(first[2]) + 60000
          else
            endpointCount = fixed(KEYS[2], endpointLimit)
            allowed = endpointCount <= endpointLimit
          end
          local endpointRemaining = math.max(0, endpointLimit - endpointCount)
          if endpointRemaining <= remaining then
            limit = endpointLimit
            remaining = endpointRemaining
            resetMs = endpointResetMs
          end
        end
        return {allowed and 1 or 0, limit, remaining, math.ceil(resetMs / 1000), math.max(1, math.ceil((resetMs - now) / 1000))}
        """;

    private static readonly Action<ILogger, Exception?> Unavailable = LoggerMessage.Define(
        LogLevel.Warning,
        new EventId(5020, nameof(Unavailable)),
        "Redis rate counter unavailable; the shared PostgreSQL quota remains enforced");

    /// <summary>Gets statistics.</summary>
    /// <param name="resource">The resource supplied to this operation.</param>
    /// <returns>The get statistics result.</returns>
    public override RateLimiterStatistics? GetStatistics(HttpContext resource) => null;

    /// <summary>Performs the attempt acquire core operation.</summary>
    /// <param name="resource">The resource supplied to this operation.</param>
    /// <param name="permitCount">The permit count supplied to this operation.</param>
    /// <returns>The attempt acquire core result.</returns>
    protected override RateLimitLease AttemptAcquireCore(HttpContext resource, int permitCount) =>
        // ASP.NET retries an unsuccessful immediate acquisition using AcquireAsync.
        // Defer Redis I/O to that asynchronous path; there is no request queue.
        new Lease(false);

    /// <summary>Performs the acquire core operation.</summary>
    /// <param name="resource">The resource supplied to this operation.</param>
    /// <param name="permitCount">The permit count supplied to this operation.</param>
    /// <param name="cancellationToken">Cancels the operation when requested by the caller.</param>
    /// <returns>A task representing the operation and its result.</returns>
    protected override async ValueTask<RateLimitLease> AcquireAsyncCore(HttpContext resource, int permitCount, CancellationToken cancellationToken)
    {
        if (resource.Request.Path.StartsWithSegments("/health", StringComparison.OrdinalIgnoreCase))
        {
            return new Lease(true);
        }

        var policy = resource.GetEndpoint()?.Metadata.GetMetadata<EnableRateLimitingAttribute>()?.PolicyName ?? string.Empty;
        var globalLimit = configuration.GetValue("RateLimiting:GlobalPermitLimit", 100);
        var endpointLimit = policy == "auth"
            ? configuration.GetValue("RateLimiting:AuthPermitLimit", 10)
            : configuration.GetValue("RateLimiting:UploadPermitLimit", 5);
        var ip = (resource.Connection.RemoteIpAddress ?? IPAddress.None).MapToIPv6().ToString();
        var prefix = $"culinary:rate:v1:{{{ip}}}";

        // Reserve before touching Redis so a timeout or recovery cannot grant a second quota.
        var reservation = await fallback.ReserveAsync(ip, policy, globalLimit, endpointLimit, cancellationToken).ConfigureAwait(false);
        try
        {
            if (reservation.Allowed)
            {
                if (!redis.IsConnected)
                {
                    throw new TimeoutException("Redis is disconnected.");
                }

                await redis.GetDatabase().ScriptEvaluateAsync(
                AcquireScript,
                [$"{prefix}:global", $"{prefix}:{policy}"],
                [globalLimit, policy, endpointLimit, Guid.NewGuid().ToString("N"), reservation.NowMilliseconds])
                    .WaitAsync(cancellationToken).ConfigureAwait(false);
            }
        }
        catch (Exception exception) when (!cancellationToken.IsCancellationRequested && exception is RedisException or TimeoutException)
        {
            Unavailable(logger, exception);
        }

        resource.Response.OnStarting(() =>
        {
            resource.Response.Headers["X-RateLimit-Limit"] = reservation.Limit.ToString(CultureInfo.InvariantCulture);
            resource.Response.Headers["X-RateLimit-Remaining"] = reservation.Remaining.ToString(CultureInfo.InvariantCulture);
            resource.Response.Headers["X-RateLimit-Reset"] = reservation.Reset.ToString(CultureInfo.InvariantCulture);
            if (!reservation.Allowed)
            {
                resource.Response.Headers.RetryAfter = reservation.RetryAfter.ToString(CultureInfo.InvariantCulture);
            }

            return Task.CompletedTask;
        });
        return new Lease(reservation.Allowed);
    }

    private sealed class Lease(bool acquired) : RateLimitLease
    {
        public override bool IsAcquired => acquired;
        public override IEnumerable<string> MetadataNames => [];

        public override bool TryGetMetadata(string metadataName, out object? metadata)
        {
            metadata = null;
            return false;
        }
    }
}
