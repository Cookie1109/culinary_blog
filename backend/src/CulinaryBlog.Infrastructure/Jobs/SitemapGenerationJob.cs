using CulinaryBlog.Application.Media;
using CulinaryBlog.Domain.Recipes;
using CulinaryBlog.Infrastructure.Configuration;
using CulinaryBlog.Infrastructure.Persistence;
using Hangfire;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CulinaryBlog.Infrastructure.Jobs;

public sealed class SitemapGenerationJob(
    AppDbContext dbContext,
    IFileStorageService fileStorage,
    IOptions<SitemapOptions> options,
    ILogger<SitemapGenerationJob> logger)
{
    private static readonly Action<ILogger, int, Exception?> Generated =
        LoggerMessage.Define<int>(
            LogLevel.Information,
            new EventId(4501, nameof(Generated)),
            "Sitemap generated with {UrlCount} URLs");

    [AutomaticRetry(Attempts = 2, DelaysInSeconds = [60, 300])]
    [DisableConcurrentExecution(timeoutInSeconds: 300)]
    public async Task GenerateAsync(CancellationToken cancellationToken)
    {
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

        var entries = new List<SitemapEntry>(3 + categories.Count + recipes.Count)
        {
            new("/"),
            new("/recipes"),
            new("/categories"),
        };
        entries.AddRange(categories.Select(category => new SitemapEntry(
            $"/recipes?category={Uri.EscapeDataString(category.Slug)}",
            category.UpdatedAt ?? category.CreatedAt)));
        entries.AddRange(recipes.Select(recipe => new SitemapEntry(
            $"/recipes/{Uri.EscapeDataString(recipe.Slug)}",
            recipe.UpdatedAt ?? recipe.PublishedAt ?? recipe.CreatedAt)));

        var content = SitemapXml.Build(publicBaseUrl, entries);
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
