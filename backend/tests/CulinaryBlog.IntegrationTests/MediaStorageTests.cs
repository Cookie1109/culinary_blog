// Culinary Blog. Authorship is recorded in the repository history.

using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using CulinaryBlog.Application.Media;
using CulinaryBlog.Infrastructure.Configuration;
using CulinaryBlog.Infrastructure.Jobs;
using CulinaryBlog.Infrastructure.Media;
using CulinaryBlog.Infrastructure.Persistence;
using DotNet.Testcontainers.Builders;
using ImageMagick;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using Minio;
using Minio.DataModel.Args;
using SkiaSharp;
using Testcontainers.Minio;

namespace CulinaryBlog.IntegrationTests;

/// <summary>Contains regression checks for media storage tests.</summary>
/// <param name="factory">The factory supplied to this operation.</param>
public sealed class MediaStorageTests(AuthApiFactory factory) : IClassFixture<AuthApiFactory>
{
    /// <summary>Verifies that real private minio accepts all formats and preserves original across resize and deletion failures.</summary>
    /// <returns>A task representing the operation and its result.</returns>
    [Fact]
    public async Task RealPrivateMinioAcceptsAllFormatsAndPreservesOriginalAcrossResizeAndDeletionFailures()
    {
        await using var minio = new MinioBuilder("minio/minio:RELEASE.2025-09-07T16-13-09Z@sha256:14cea493d9a34af32f524e538b8346cf79f3321eff8e708c1e2960462bd8936e")
            .WithUsername("g04-test").WithPassword("integration-test-only")
            .WithWaitStrategy(Wait.ForUnixContainer().UntilHttpRequestIsSucceeded(request => request.ForPort(9000).ForPath("/minio/health/ready")))
            .Build();
        await minio.StartAsync();
        using var minioClient = new MinioClient().WithEndpoint(minio.Hostname, minio.GetMappedPublicPort(9000))
            .WithCredentials("g04-test", "integration-test-only").Build();
        await minioClient.MakeBucketAsync(new MakeBucketArgs().WithBucket("g04-media"));
        var storage = new FailingStorage(new MinioFileStorageService(minioClient, Options.Create(new ObjectStorageOptions { BucketName = "g04-media" })));
        await using var custom = factory.WithWebHostBuilder(builder =>
        {
            builder.UseSetting("RateLimiting:UploadPermitLimit", "100");
            builder.ConfigureTestServices(services =>
            {
                services.RemoveAll<IFileStorageService>();
                services.AddSingleton<IFileStorageService>(storage);
            });
        });
        using var client = custom.CreateClient();
        using var registered = await client.PostAsJsonAsync("/api/v1/auth/register", new
        {
            email = $"media-{Guid.NewGuid():N}@example.test",
            password = "Valid#Password1",
            displayName = "Media Author",
        });
        registered.EnsureSuccessStatusCode();
        var session = (await registered.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("data");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", session.GetProperty("accessToken").GetString());
        var categories = await client.GetFromJsonAsync<JsonElement>("/api/v1/categories");
        using var created = await client.PostAsJsonAsync("/api/v1/recipes", new
        {
            title = "Real media " + Guid.NewGuid().ToString("N"),
            description = "Synthetic storage fixture.",
            categoryId = categories.GetProperty("data")[0].GetProperty("id").GetGuid(),
            prepTime = 1,
            cookTime = 0,
            servings = 1,
            difficulty = "easy",
        });
        created.EnsureSuccessStatusCode();
        var recipeId = (await created.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("data").GetProperty("id").GetGuid();
        JsonElement avif = default;
        string? preservedKey = null;
        long version = 1;
        foreach (var (format, mime) in new[] { (MagickFormat.Jpeg, "image/jpeg"), (MagickFormat.Png, "image/png"), (MagickFormat.WebP, "image/webp"), (MagickFormat.Avif, "image/avif") })
        {
            using var image = new MagickImage(MagickColors.Red, 32, 16);
            var bytes = image.ToByteArray(format);
            using var upload = await Upload(client, recipeId, version++, mime, bytes);
            Assert.Equal(HttpStatusCode.Created, upload.StatusCode);
            avif = (await upload.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("data");
            var keys = new List<string>();
            await foreach (var key in storage.ListKeysAsync($"recipes/{recipeId:N}/", CancellationToken.None))
            {
                keys.Add(key);
            }

            Assert.All(keys, key => Assert.DoesNotContain("..", key, StringComparison.Ordinal));
            preservedKey ??= Assert.Single(keys);
        }

        var imageId = avif.GetProperty("id").GetGuid();
        var originalUrl = avif.GetProperty("originalUrl").GetString()!;
        var originalKey = $"recipes/{recipeId:N}/{imageId:N}/original.avif";
        using (var anonymous = new HttpClient())
        {
            using var direct = await anonymous.GetAsync($"http://{minio.Hostname}:{minio.GetMappedPublicPort(9000)}/g04-media/{originalKey}");
            Assert.Equal(HttpStatusCode.Forbidden, direct.StatusCode);
        }

        storage.FailVariants = true;
        var failedJob = await HangfireRetryProbe.FailAsync(
            custom.Services,
            Hangfire.Common.Job.FromExpression<ImageProcessingJob>(job => job.ProcessAsync(Guid.Empty, imageId, CancellationToken.None)),
            "g04-media");
        await using (var scope = custom.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var image = await db.RecipeImages.AsNoTracking().SingleAsync(item => item.Id == imageId);
            Assert.Equal(CulinaryBlog.Domain.Recipes.ImageProcessingStatus.Failed, image.ProcessingStatus);
            Assert.Equal(originalUrl, image.OriginalUrl);
            Assert.Null(image.MediumUrl);
        }

        using var fallback = await client.GetAsync(originalUrl);
        Assert.Equal(HttpStatusCode.OK, fallback.StatusCode);
        Assert.Equal("image/avif", fallback.Content.Headers.ContentType?.MediaType);
        Assert.True(await storage.ExistsAsync(originalKey, CancellationToken.None));
        storage.FailVariants = false;
        await HangfireRetryProbe.RecoverAsync(custom.Services, failedJob, "g04-media");
        await using (var restart = custom.Services.CreateAsyncScope())
        {
            var job = restart.ServiceProvider.GetRequiredService<ImageProcessingJob>();
            await job.ProcessAsync(Guid.NewGuid(), imageId, CancellationToken.None);
            await job.ProcessAsync(Guid.NewGuid(), imageId, CancellationToken.None);
        }

        foreach (var (name, width, height) in new[] { ("medium.webp", 800, 600), ("thumbnail.webp", 300, 300) })
        {
            await using var content = await storage.OpenReadAsync($"recipes/{recipeId:N}/{imageId:N}/{name}", CancellationToken.None);
            using var decoded = SKBitmap.Decode(content);
            Assert.Equal(width, decoded.Width);
            Assert.Equal(height, decoded.Height);
            Assert.Equal(SKColors.White, decoded.GetPixel(0, 0));
            Assert.True(decoded.GetPixel(width / 2, height / 2).Red > 200);
        }

        using var deletion = new HttpRequestMessage(HttpMethod.Delete, $"/api/v1/recipes/{recipeId}/images/{imageId}");
        deletion.Headers.IfMatch.Add(new EntityTagHeaderValue($"\"{version}\""));
        using var deleted = await client.SendAsync(deletion);
        Assert.Equal(HttpStatusCode.NoContent, deleted.StatusCode);
        var objectKeys = new[] { originalKey, $"recipes/{recipeId:N}/{imageId:N}/medium.webp", $"recipes/{recipeId:N}/{imageId:N}/thumbnail.webp" };
        await using var deleteScope = custom.Services.CreateAsyncScope();
        var deletionJob = deleteScope.ServiceProvider.GetRequiredService<ObjectDeletionJob>();
        storage.FailDeletes = true;
        var failedDeletion = await HangfireRetryProbe.FailAsync(
            custom.Services,
            Hangfire.Common.Job.FromExpression<ObjectDeletionJob>(job => job.DeleteAsync(Guid.Empty, objectKeys, CancellationToken.None)),
            "g04-delete");
        Assert.True(await storage.ExistsAsync(originalKey, CancellationToken.None));
        var context = deleteScope.ServiceProvider.GetRequiredService<AppDbContext>();
        Assert.True((await context.RecipeImages.IgnoreQueryFilters().SingleAsync(item => item.Id == imageId)).IsDeleted);
        Assert.Contains(await context.MediaOutbox.ToListAsync(), message => message.Type == "objects.delete" && message.Payload.Contains(originalKey, StringComparison.Ordinal));
        storage.FailDeletes = false;
        await HangfireRetryProbe.RecoverAsync(custom.Services, failedDeletion, "g04-delete");
        await deletionJob.DeleteAsync(Guid.NewGuid(), objectKeys, CancellationToken.None);
        foreach (var key in objectKeys)
        {
            Assert.False(await storage.ExistsAsync(key, CancellationToken.None));
        }

        Assert.True(await storage.ExistsAsync(preservedKey!, CancellationToken.None));
    }

    private static async Task<HttpResponseMessage> Upload(HttpClient client, Guid recipeId, long version, string mime, byte[] bytes)
    {
        using var form = new MultipartFormDataContent();
        var file = new ByteArrayContent(bytes);
        file.Headers.ContentType = new MediaTypeHeaderValue(mime);
        form.Add(file, "file", "../../unsafe.name");
        using var request = new HttpRequestMessage(HttpMethod.Post, $"/api/v1/recipes/{recipeId}/images") { Content = form };
        request.Headers.IfMatch.Add(new EntityTagHeaderValue($"\"{version}\""));
        return await client.SendAsync(request);
    }

    private sealed class FailingStorage(IFileStorageService inner) : IFileStorageService
    {
        public bool FailVariants { get; set; }
        public bool FailDeletes { get; set; }

        public Task UploadAsync(string objectKey, Stream content, long length, string contentType, CancellationToken cancellationToken) =>
            this.FailVariants && objectKey.EndsWith(".webp", StringComparison.Ordinal) ? throw new IOException("Synthetic resize failure") : inner.UploadAsync(objectKey, content, length, contentType, cancellationToken);

        public Task DeleteAsync(string objectKey, CancellationToken cancellationToken) => this.FailDeletes ? throw new IOException("Synthetic delete failure") : inner.DeleteAsync(objectKey, cancellationToken);

        public Task<Stream> OpenReadAsync(string objectKey, CancellationToken cancellationToken) => inner.OpenReadAsync(objectKey, cancellationToken);

        public Task<bool> ExistsAsync(string objectKey, CancellationToken cancellationToken) => inner.ExistsAsync(objectKey, cancellationToken);

        public IAsyncEnumerable<string> ListKeysAsync(string prefix, CancellationToken cancellationToken) => inner.ListKeysAsync(prefix, cancellationToken);
    }
}
