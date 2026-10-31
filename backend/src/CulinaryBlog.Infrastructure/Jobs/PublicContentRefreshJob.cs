// Culinary Blog. Authorship is recorded in the repository history.

using System.Net.Http.Json;
using CulinaryBlog.Infrastructure.Configuration;
using Hangfire;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using StackExchange.Redis;

namespace CulinaryBlog.Infrastructure.Jobs;

/// <summary>Represents the public content refresh job.</summary>
/// <param name="sitemapGeneration">The sitemap generation supplied to this operation.</param>
/// <param name="redis">The redis supplied to this operation.</param>
/// <param name="httpClientFactory">The http client factory supplied to this operation.</param>
/// <param name="options">The options supplied to this operation.</param>
/// <param name="logger">The logger supplied to this operation.</param>
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

    /// <summary>Performs the run operation.</summary>
    /// <param name="debounceToken">The debounce token supplied to this operation.</param>
    /// <param name="cancellationToken">Cancels the operation when requested by the caller.</param>
    /// <returns>A task representing the operation and its result.</returns>
    [AutomaticRetry(Attempts = 2, DelaysInSeconds = [60, 300])]
    [DisableConcurrentExecution(timeoutInSeconds: 300)]
    public async Task RunAsync(string debounceToken, CancellationToken cancellationToken)
    {
        if (!await this.IsLatestAsync(debounceToken, cancellationToken).ConfigureAwait(false))
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
