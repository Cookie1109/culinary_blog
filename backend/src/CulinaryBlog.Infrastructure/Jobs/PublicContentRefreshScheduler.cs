// Culinary Blog. Authorship is recorded in the repository history.

using CulinaryBlog.Application.Content;
using CulinaryBlog.Infrastructure.Configuration;
using Hangfire;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using StackExchange.Redis;

namespace CulinaryBlog.Infrastructure.Jobs;

/// <summary>Represents the public content refresh scheduler.</summary>
/// <param name="backgroundJobs">The background jobs supplied to this operation.</param>
/// <param name="redis">The redis supplied to this operation.</param>
/// <param name="options">The options supplied to this operation.</param>
/// <param name="configuration">The configuration supplied to this operation.</param>
/// <param name="logger">The logger supplied to this operation.</param>
public sealed class PublicContentRefreshScheduler(
    IBackgroundJobClient backgroundJobs,
    IConnectionMultiplexer redis,
    IOptions<SitemapOptions> options,
    IConfiguration configuration,
    ILogger<PublicContentRefreshScheduler> logger) : IPublicContentRefresh
{
    /// <summary>The Redis key containing the newest debounced refresh token.</summary>
    internal const string LatestTokenKey = "culinary:v1:public-content-refresh:latest";

    private static readonly Action<ILogger, Exception?> RedisUnavailable =
        LoggerMessage.Define(
            LogLevel.Warning,
            new EventId(4502, nameof(RedisUnavailable)),
            "Redis is unavailable while debouncing public content refresh; scheduling a safe fallback job");

    private static readonly Action<ILogger, Exception?> SchedulingFailed =
        LoggerMessage.Define(
            LogLevel.Error,
            new EventId(4503, nameof(SchedulingFailed)),
            "Could not schedule public content refresh; the daily sitemap job remains the fallback");

    /// <summary>Schedules the public content refresh scheduler.</summary>
    /// <param name="cancellationToken">Cancels the operation when requested by the caller.</param>
    /// <returns>A task representing the operation and its result.</returns>
    public async Task ScheduleAsync(CancellationToken cancellationToken = default)
    {
        if (!configuration.GetValue("BackgroundJobs:Enabled", true))
        {
            return;
        }

        var token = Guid.NewGuid().ToString("N");
        try
        {
            await redis.GetDatabase().StringSetAsync(
                LatestTokenKey,
                token,
                TimeSpan.FromMinutes(10)).ConfigureAwait(false);
        }
        catch (Exception exception) when (IsRedisFailure(exception, cancellationToken))
        {
            token = string.Empty;
            RedisUnavailable(logger, exception);
        }

        try
        {
            backgroundJobs.Schedule<PublicContentRefreshJob>(
                job => job.RunAsync(token, CancellationToken.None),
                TimeSpan.FromSeconds(options.Value.DebounceSeconds));
        }
        catch (Exception exception)
        {
            SchedulingFailed(logger, exception);
        }
    }

    private static bool IsRedisFailure(Exception exception, CancellationToken cancellationToken) =>
        !cancellationToken.IsCancellationRequested && exception is RedisException or TimeoutException;
}
