using System.Net.Http.Json;
using CulinaryBlog.Infrastructure.Configuration;
using Hangfire;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using StackExchange.Redis;

namespace CulinaryBlog.Infrastructure.Jobs;

public sealed class PublicContentRefreshJob(
    SitemapGenerationJob sitemapGeneration,
    IConnectionMultiplexer redis,
    IHttpClientFactory httpClientFactory,
    IOptions<SitemapOptions> options,
    ILogger<PublicContentRefreshJob> logger)
{
    private static readonly Action<ILogger, Exception?> RedisUnavailable =
        LoggerMessage.Define(
            LogLevel.Warning,
            new EventId(4504, nameof(RedisUnavailable)),
            "Redis is unavailable while checking the public content refresh token; running the job as a safe fallback");

    private static readonly Action<ILogger, Exception?> RevalidationSkipped =
        LoggerMessage.Define(
            LogLevel.Information,
            new EventId(4505, nameof(RevalidationSkipped)),
            "Frontend revalidation is not configured; sitemap generation completed without a callback");

    [AutomaticRetry(Attempts = 2, DelaysInSeconds = [60, 300])]
    [DisableConcurrentExecution(timeoutInSeconds: 300)]
    public async Task RunAsync(string debounceToken, CancellationToken cancellationToken)
    {
        if (!await IsLatestAsync(debounceToken, cancellationToken).ConfigureAwait(false))
        {
            return;
        }

        await sitemapGeneration.GenerateAsync(cancellationToken).ConfigureAwait(false);

        var settings = options.Value;
        if (string.IsNullOrWhiteSpace(settings.RevalidationEndpoint) ||
            string.IsNullOrWhiteSpace(settings.RevalidationSecret))
        {
            RevalidationSkipped(logger, null);
            return;
        }

        using var request = new HttpRequestMessage(HttpMethod.Post, settings.RevalidationEndpoint)
        {
            Content = JsonContent.Create(new { source = "content-mutation" }),
        };
        request.Headers.Add("X-Revalidation-Secret", settings.RevalidationSecret);
        using var response = await httpClientFactory.CreateClient("default")
            .SendAsync(request, cancellationToken).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();
    }

    private async Task<bool> IsLatestAsync(string debounceToken, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(debounceToken))
        {
            return true;
        }

        try
        {
            var latest = await redis.GetDatabase()
                .StringGetAsync(PublicContentRefreshScheduler.LatestTokenKey)
                .ConfigureAwait(false);
            return latest.IsNullOrEmpty || latest == debounceToken;
        }
        catch (Exception exception) when (
            !cancellationToken.IsCancellationRequested && exception is RedisException or TimeoutException)
        {
            RedisUnavailable(logger, exception);
            return true;
        }
    }
}
