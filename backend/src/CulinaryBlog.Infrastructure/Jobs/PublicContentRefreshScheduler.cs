using CulinaryBlog.Infrastructure.Configuration;
using Hangfire;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using StackExchange.Redis;

namespace CulinaryBlog.Infrastructure.Jobs;

public sealed class PublicContentRefreshScheduler(
    IBackgroundJobClient backgroundJobs,
    IConnectionMultiplexer redis,
    IOptions<SitemapOptions> options,
    IConfiguration configuration,
    ILogger<PublicContentRefreshScheduler> logger)
{
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
