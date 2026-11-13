// Culinary Blog. Authorship is recorded in the repository history.

using CulinaryBlog.Application.Media;
using CulinaryBlog.Domain.Recipes;
using CulinaryBlog.Infrastructure.Configuration;
using CulinaryBlog.Infrastructure.Persistence;
using Hangfire;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RedLockNet;

namespace CulinaryBlog.Infrastructure.Jobs;

/// <summary>Represents the sitemap generation job.</summary>
/// <param name="dbContext">The db context supplied to this operation.</param>
/// <param name="fileStorage">The file storage supplied to this operation.</param>
/// <param name="options">The options supplied to this operation.</param>
/// <param name="logger">The logger supplied to this operation.</param>
/// <param name="locks">The locks supplied to this operation.</param>
public sealed class SitemapGenerationJob(
    AppDbContext dbContext,
    IFileStorageService fileStorage,
    IOptions<SitemapOptions> options,
    ILogger<SitemapGenerationJob> logger,
    IDistributedLockFactory locks)
{
    private static readonly Action<ILogger, int, Exception?> Generated =
        LoggerMessage.Define<int>(
            LogLevel.Information,
            new EventId(4501, nameof(Generated)),
            "Sitemap generated with {UrlCount} URLs");

    /// <summary>Generates the sitemap generation job.</summary>
    /// <param name="cancellationToken">Cancels the operation when requested by the caller.</param>
    /// <returns>A task representing the operation and its result.</returns>
    [AutomaticRetry(Attempts = 2, DelaysInSeconds = [60, 300])]
    [DisableConcurrentExecution(timeoutInSeconds: 300)]
    public async Task GenerateAsync(CancellationToken cancellationToken)
    {
        await using var lease = await locks.CreateLockAsync(
            "culinary:sitemap",
            TimeSpan.FromMinutes(5),
            TimeSpan.FromSeconds(5),
            TimeSpan.FromMilliseconds(200),
            cancellationToken).ConfigureAwait(false);
        if (!lease.IsAcquired)
        {
            throw new TimeoutException("The sitemap RedLock could not be acquired.");
        }

        var settings = options.Value;
        var publicBaseUrl = new Uri(AppendTrailingSlash(settings.PublicBaseUrl), UriKind.Absolute);
        var categories = await dbContext.Categories
            .AsNoTracking()
            .OrderBy(category => category.Slug)
            .Select(category => new { category.Slug, category.CreatedAt, category.UpdatedAt })
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
        var recipes = await dbContext.Recipes
            .AsNoTracking()
            .Where(recipe => recipe.Status == RecipeStatus.Published)
            .OrderBy(recipe => recipe.Slug)
            .Select(recipe => new { recipe.Slug, recipe.CreatedAt, recipe.UpdatedAt, recipe.PublishedAt })
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var latestContentChange = categories.Select(category => (DateTimeOffset?)(category.UpdatedAt ?? category.CreatedAt))
            .Concat(recipes.Select(recipe => (DateTimeOffset?)(recipe.UpdatedAt ?? recipe.PublishedAt ?? recipe.CreatedAt)))
            .DefaultIfEmpty().Max();
        var entries = new List<SitemapEntry>(3 + categories.Count + recipes.Count)
        {
            new("/", latestContentChange),
            new("/recipes", latestContentChange),
            new("/categories", latestContentChange),
        };
        entries.AddRange(categories.Select(category => new SitemapEntry(
            $"/categories/{Uri.EscapeDataString(category.Slug)}",
            category.UpdatedAt ?? category.CreatedAt)));
        entries.AddRange(recipes.Select(recipe => new SitemapEntry(
            $"/recipes/{Uri.EscapeDataString(recipe.Slug)}",
            recipe.UpdatedAt ?? recipe.PublishedAt ?? recipe.CreatedAt)));

        var content = SitemapXml.Build(publicBaseUrl, entries);
        cancellationToken.ThrowIfCancellationRequested();
        if (!lease.IsAcquired)
        {
            throw new InvalidOperationException("The sitemap RedLock was lost before upload.");
        }

        await using var stream = new MemoryStream(content, writable: false);
        await fileStorage.UploadAsync(
            settings.ObjectKey,
            stream,
            content.Length,
            "application/xml",
            cancellationToken).ConfigureAwait(false);
        Generated(logger, entries.Count, null);
    }

    private static string AppendTrailingSlash(string value) => value.EndsWith('/') ? value : $"{value}/";
}
