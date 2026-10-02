using System.Text;
using CulinaryBlog.Application.Media;
using CulinaryBlog.Domain.Categories;
using CulinaryBlog.Domain.Recipes;
using CulinaryBlog.Infrastructure.Configuration;
using CulinaryBlog.Infrastructure.Identity;
using CulinaryBlog.Infrastructure.Jobs;
using CulinaryBlog.Infrastructure.Persistence;
using Hangfire;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace CulinaryBlog.IntegrationTests;

public sealed class SitemapGenerationJobTests(AuthApiFactory factory) : IClassFixture<AuthApiFactory>
{
    [Fact]
    public async Task GenerateIncludesOnlyPublicContentAndRetainsPreviousArtifactWhenUploadFails()
    {
        await using var scope = factory.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var suffix = Guid.NewGuid().ToString("N");
        var author = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            UserName = $"sitemap-{suffix}@example.com",
            Email = $"sitemap-{suffix}@example.com",
            DisplayName = "Sitemap Author",
            IsActive = true,
        };
        Assert.True((await userManager.CreateAsync(author)).Succeeded);

        var category = Category.Create(
            Guid.NewGuid(),
            $"Danh mục {suffix}",
            $"danh-muc-{suffix}",
            null,
            null,
            0);
        var published = CreateRecipe(author.Id, category.Id, $"published-{suffix}");
        published.Publish(DateTimeOffset.UtcNow, true, 1, [1]);
        var draft = CreateRecipe(author.Id, category.Id, $"draft-{suffix}");
        dbContext.AddRange(category, published, draft);
        await dbContext.SaveChangesAsync();

        var storage = new RecordingFileStorage();
        var options = Options.Create(new SitemapOptions { PublicBaseUrl = "https://food.example" });
        var job = new SitemapGenerationJob(
            dbContext,
            storage,
            options,
            NullLogger<SitemapGenerationJob>.Instance);

        await job.GenerateAsync(CancellationToken.None);

        var xml = Encoding.UTF8.GetString(Assert.IsType<byte[]>(storage.Content));
        Assert.Equal("system/sitemap.xml", storage.ObjectKey);
        Assert.Contains("https://food.example/recipes", xml, StringComparison.Ordinal);
        Assert.Contains($"https://food.example/recipes?category=danh-muc-{suffix}", xml, StringComparison.Ordinal);
        Assert.Contains($"https://food.example/recipes/published-{suffix}", xml, StringComparison.Ordinal);
        Assert.DoesNotContain($"draft-{suffix}", xml, StringComparison.Ordinal);

        var retained = storage.Content;
        storage.FailUploads = true;
        await Assert.ThrowsAsync<IOException>(() => job.GenerateAsync(CancellationToken.None));
        Assert.Same(retained, storage.Content);
    }

    [Fact]
    public void GenerateRetriesExactlyTwice()
    {
        var retry = typeof(SitemapGenerationJob)
            .GetMethod(nameof(SitemapGenerationJob.GenerateAsync))!
            .GetCustomAttributes(typeof(AutomaticRetryAttribute), inherit: false)
            .Cast<AutomaticRetryAttribute>()
            .Single();

        Assert.Equal(2, retry.Attempts);
    }

    private static Recipe CreateRecipe(Guid authorId, Guid categoryId, string slug) =>
        Recipe.Create(
            Guid.NewGuid(),
            authorId,
            slug,
            "Công thức sitemap",
            "Mô tả dùng để kiểm thử sitemap.",
            categoryId,
            10,
            20,
            4,
            RecipeDifficulty.Easy,
            "Hướng dẫn",
            null);

    private sealed class RecordingFileStorage : IFileStorageService
    {
        public byte[]? Content { get; private set; }

        public string? ObjectKey { get; private set; }

        public bool FailUploads { get; set; }

        public async Task UploadAsync(
            string objectKey,
            Stream content,
            long length,
            string contentType,
            CancellationToken cancellationToken)
        {
            if (FailUploads)
            {
                throw new IOException("Simulated upload failure.");
            }

            using var copy = new MemoryStream();
            await content.CopyToAsync(copy, cancellationToken);
            ObjectKey = objectKey;
            Content = copy.ToArray();
        }

        public Task<Stream> OpenReadAsync(string objectKey, CancellationToken cancellationToken) =>
            Task.FromResult<Stream>(new MemoryStream(Content ?? []));

        public Task DeleteAsync(string objectKey, CancellationToken cancellationToken) => Task.CompletedTask;

        public Task<bool> ExistsAsync(string objectKey, CancellationToken cancellationToken) =>
            Task.FromResult(Content is not null);

        public async IAsyncEnumerable<string> ListKeysAsync(
            string prefix,
            [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken)
        {
            await Task.CompletedTask;
            yield break;
        }
    }
}
