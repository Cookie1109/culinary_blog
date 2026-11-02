using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using CulinaryBlog.Infrastructure.Identity;
using CulinaryBlog.Infrastructure.Jobs;
using CulinaryBlog.Infrastructure.Persistence;
using Hangfire;
using Hangfire.Common;
using Hangfire.States;
using ImageMagick;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SkiaSharp;

namespace CulinaryBlog.IntegrationTests;

public sealed class RecipeMediaUploadTests(RecipeMediaFixture fixture) : IClassFixture<RecipeMediaFixture>
{
    private const int Limit = 5 * 1024 * 1024;
    private const string Password = "Synthetic#Media123";

    [Theory]
    [InlineData(MagickFormat.Jpeg, "image/jpeg")]
    [InlineData(MagickFormat.Png, "image/png")]
    [InlineData(MagickFormat.WebP, "image/webp")]
    [InlineData(MagickFormat.Avif, "image/avif")]
    public async Task RealStorageAndWorkerAcceptFormatsAndProduceExactVariants(MagickFormat format, string mime)
    {
        using var client = fixture.App.CreateClient();
        await Register(client);
        var recipe = await CreateRecipe(client);
        using var uploaded = await Upload(client, recipe.Id, 1, mime, Image(format));
        Assert.Equal(HttpStatusCode.Created, uploaded.StatusCode);
        var image = await Data(uploaded);
        Assert.True(image.GetProperty("isPrimary").GetBoolean());
        var id = image.GetProperty("id").GetGuid();
        await RunJob(Job.FromExpression<ImageProcessingJob>(job => job.ProcessAsync(Guid.Empty, id, CancellationToken.None)));
        foreach (var (variant, width, height) in new[] { ("medium", 800, 600), ("thumbnail", 300, 300) })
        {
            using var response = await client.GetAsync($"/api/v1/media/{id}/{variant}");
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            using var bitmap = SKBitmap.Decode(await response.Content.ReadAsByteArrayAsync());
            Assert.Equal(width, bitmap.Width);
            Assert.Equal(height, bitmap.Height);
            Assert.True(bitmap.GetPixel(width / 2, height / 2).Red > 200);
        }
        using var guest = new HttpClient();
        var key = $"recipes/{recipe.Id:N}/{id:N}/original.{format.ToString().ToLowerInvariant().Replace("jpeg", "jpg", StringComparison.Ordinal)}";
        Assert.True(await fixture.Storage.ExistsAsync(key, default));
        using var direct = await guest.GetAsync($"http://{fixture.Minio.Hostname}:{fixture.Minio.GetMappedPublicPort(9000)}/{RecipeMediaFixture.Bucket}/{key}");
        Assert.Equal(HttpStatusCode.Forbidden, direct.StatusCode);
        using var anonymous = fixture.App.CreateClient();
        using var privateMedia = await anonymous.GetAsync($"/api/v1/media/{id}/original");
        Assert.Equal(HttpStatusCode.NotFound, privateMedia.StatusCode);
    }

    [Fact]
    public async Task InvalidUploadsAndStorageFailureLeaveNoRowsObjectsOrVersionChange()
    {
        using var client = fixture.App.CreateClient();
        await Register(client);
        var recipe = await CreateRecipe(client);
        var png = Image(MagickFormat.Png);
        foreach (var (mime, bytes) in new[] { ("image/png", Array.Empty<byte>()), ("image/png", new byte[Limit + 1]),
                     ("image/png", new byte[] { 1, 2, 3 }), ("image/jpeg", png), ("text/plain", png) })
        {
            using var invalid = await Upload(client, recipe.Id, 1, mime, bytes);
            Assert.Equal(HttpStatusCode.BadRequest, invalid.StatusCode);
        }
        fixture.Storage.FailAfterOriginal = true;
        try
        {
            using var failed = await Upload(client, recipe.Id, 1, "image/png", png);
            Assert.Equal(HttpStatusCode.ServiceUnavailable, failed.StatusCode);
        }
        finally { fixture.Storage.FailAfterOriginal = false; }
        await AssertImageCount(recipe.Id, 0, 1);
        Assert.Empty(await Keys(recipe.Id));
        fixture.Storage.FailAfterOriginal = true;
        fixture.Storage.FailDeletes = true;
        try
        {
            using var failed = await Upload(client, recipe.Id, 1, "image/png", png);
            Assert.Equal(HttpStatusCode.ServiceUnavailable, failed.StatusCode);
        }
        finally { fixture.Storage.FailAfterOriginal = false; fixture.Storage.FailDeletes = false; }
        await AssertImageCount(recipe.Id, 0, 1);
        await using (var cleanup = fixture.App.Services.CreateAsyncScope())
        {
            var messages = await cleanup.ServiceProvider.GetRequiredService<AppDbContext>().MediaOutbox.Where(item =>
                item.Type == "objects.delete").ToListAsync();
            var message = messages.Single(item => item.Payload.Contains(recipe.Id.ToString("N"), StringComparison.Ordinal));
            var keys = JsonSerializer.Deserialize<DeleteObjectsPayload>(message.Payload)!.ObjectKeys;
            await RunJob(Job.FromExpression<ObjectDeletionJob>(job => job.DeleteAsync(Guid.Empty, keys, CancellationToken.None)));
        }
        Assert.Empty(await Keys(recipe.Id));
        await fixture.Minio.StopAsync();
        try
        {
            using var failed = await Upload(client, recipe.Id, 1, "image/png", png);
            Assert.Equal(HttpStatusCode.ServiceUnavailable, failed.StatusCode);
        }
        finally { await fixture.Minio.StartAsync(); }
        await AssertImageCount(recipe.Id, 0, 1);
        Assert.Empty(await Keys(recipe.Id));
        var boundary = new byte[Limit]; png.CopyTo(boundary, 0);
        using var accepted = await Upload(client, recipe.Id, 1, "image/png", boundary);
        Assert.Equal(HttpStatusCode.Created, accepted.StatusCode);
        await AssertImageCount(recipe.Id, 1, 2);
    }

    [Fact]
    public async Task OwnershipMetadataAndConcurrentUploadsPreservePrimaryVersionAndObjects()
    {
        using var owner = fixture.App.CreateClient();
        await Register(owner);
        var recipe = await CreateRecipe(owner);
        using var other = fixture.App.CreateClient(); await Register(other);
        using var guest = fixture.App.CreateClient();
        using var forbidden = await Upload(other, recipe.Id, 1, "image/png", Image(MagickFormat.Png));
        using var unauthenticated = await Upload(guest, recipe.Id, 1, "image/png", Image(MagickFormat.Png));
        Assert.Equal(HttpStatusCode.Forbidden, forbidden.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, unauthenticated.StatusCode);
        await AssertImageCount(recipe.Id, 0, 1);
        var responses = await Task.WhenAll(Upload(owner, recipe.Id, 1, "image/png", Image(MagickFormat.Png)),
            Upload(owner, recipe.Id, 1, "image/png", Image(MagickFormat.Png)));
        Assert.Single(responses, response => response.StatusCode == HttpStatusCode.Created);
        Assert.Single(responses, response => response.StatusCode == HttpStatusCode.Conflict);
        foreach (var response in responses) response.Dispose();
        await AssertImageCount(recipe.Id, 1, 2);
        Assert.Single(await Keys(recipe.Id));
        using var second = await Upload(owner, recipe.Id, 2, "image/png", Image(MagickFormat.Png));
        var secondId = (await Data(second)).GetProperty("id").GetGuid();
        using var patch = await Send(owner, HttpMethod.Patch, $"/api/v1/recipes/{recipe.Id}/images/{secondId}", 3,
            new { altText = "Ảnh đã chỉnh", isPrimary = true, orderIndex = 0 });
        Assert.Equal(HttpStatusCode.OK, patch.StatusCode);
        await using (var verification = fixture.App.Services.CreateAsyncScope())
        {
            var image = await verification.ServiceProvider.GetRequiredService<AppDbContext>().RecipeImages.SingleAsync(item => item.Id == secondId);
            Assert.Equal("Ảnh đã chỉnh", image.AltText); Assert.True(image.IsPrimary); Assert.Equal(0, image.OrderIndex);
        }
        foreach (var variant in new[] { "medium.webp", "thumbnail.webp" })
        {
            await using var content = new MemoryStream(Image(MagickFormat.WebP));
            await fixture.Storage.UploadAsync($"recipes/{recipe.Id:N}/{secondId:N}/{variant}", content, content.Length, "image/webp", default);
        }
        using var stale = await Send(owner, HttpMethod.Delete, $"/api/v1/recipes/{recipe.Id}/images/{secondId}", 3);
        Assert.Equal(HttpStatusCode.Conflict, stale.StatusCode);
        await AssertImageCount(recipe.Id, 2, 4);
        using var deleted = await Send(owner, HttpMethod.Delete, $"/api/v1/recipes/{recipe.Id}/images/{secondId}", 4);
        Assert.Equal(HttpStatusCode.NoContent, deleted.StatusCode);
        await AssertImageCount(recipe.Id, 1, 5);
        await using var scope = fixture.App.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var replacement = await db.RecipeImages.SingleAsync(image => image.RecipeId == recipe.Id);
        Assert.True(replacement.IsPrimary); Assert.Equal(0, replacement.OrderIndex);
        var deletions = await db.MediaOutbox.Where(message => message.Type == "objects.delete").ToListAsync();
        var payload = JsonSerializer.Deserialize<DeleteObjectsPayload>(deletions.Single(message =>
            message.Payload.Contains(secondId.ToString("N"), StringComparison.Ordinal)).Payload)!;
        await RunJob(Job.FromExpression<ObjectDeletionJob>(job => job.DeleteAsync(Guid.Empty, payload.ObjectKeys, CancellationToken.None)));
        Assert.Single(await Keys(recipe.Id));
    }

    [Fact]
    public async Task AdminCanUploadForAnotherAuthorAndGuestCannotReadArchivedOrDeletedMedia()
    {
        using var owner = fixture.App.CreateClient(); await Register(owner);
        var recipe = await CreateRecipe(owner);
        using var admin = fixture.App.CreateClient(); var email = await Register(admin);
        await using (var scope = fixture.App.Services.CreateAsyncScope())
        {
            var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            Assert.True((await users.AddToRoleAsync((await users.FindByEmailAsync(email))!, "Admin")).Succeeded);
        }
        using var login = await admin.PostAsJsonAsync("/api/v1/auth/login", new { email, password = Password });
        admin.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", (await Data(login)).GetProperty("accessToken").GetString());
        using var uploaded = await Upload(admin, recipe.Id, 1, "image/png", Image(MagickFormat.Png));
        Assert.Equal(HttpStatusCode.Created, uploaded.StatusCode);
        var id = (await Data(uploaded)).GetProperty("id").GetGuid();
        await RunJob(Job.FromExpression<ImageProcessingJob>(job => job.ProcessAsync(Guid.Empty, id, CancellationToken.None)));
        using var ingredient = await Send(owner, HttpMethod.Post, $"/api/v1/recipes/{recipe.Id}/ingredients", 2, new { name = "Gạo" });
        Assert.Equal(HttpStatusCode.Created, ingredient.StatusCode);
        using var step = await Send(owner, HttpMethod.Post, $"/api/v1/recipes/{recipe.Id}/steps", 3, new { title = "Nấu", description = "Nấu chín" });
        Assert.Equal(HttpStatusCode.Created, step.StatusCode);
        using var published = await Send(owner, HttpMethod.Patch, $"/api/v1/recipes/{recipe.Id}/publish", 4);
        Assert.Equal(HttpStatusCode.OK, published.StatusCode);
        using var guest = fixture.App.CreateClient();
        using var visible = await guest.GetAsync($"/api/v1/media/{id}/original");
        Assert.Equal(HttpStatusCode.OK, visible.StatusCode); Assert.True(visible.Headers.CacheControl?.NoStore);
        using var archived = await Send(owner, HttpMethod.Patch, $"/api/v1/recipes/{recipe.Id}/archive", 5);
        Assert.Equal(HttpStatusCode.OK, archived.StatusCode);
        foreach (var variant in new[] { "original", "medium", "thumbnail" })
            Assert.Equal(HttpStatusCode.NotFound, (await guest.GetAsync($"/api/v1/media/{id}/{variant}")).StatusCode);
        using var deleted = await Send(owner, HttpMethod.Delete, $"/api/v1/recipes/{recipe.Id}", 6);
        Assert.Equal(HttpStatusCode.NoContent, deleted.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await owner.GetAsync($"/api/v1/media/{id}/original")).StatusCode);
    }

    [Fact]
    public async Task FailedResizeRetriesInHangfireAndPreservesOriginal()
    {
        using var client = fixture.App.CreateClient(); await Register(client);
        var recipe = await CreateRecipe(client);
        using var upload = await Upload(client, recipe.Id, 1, "image/png", Image(MagickFormat.Png));
        var id = (await Data(upload)).GetProperty("id").GetGuid();
        fixture.Storage.FailVariants = true;
        try
        {
            await RunJob(Job.FromExpression<ImageProcessingJob>(job => job.ProcessAsync(Guid.Empty, id, CancellationToken.None)), failThenRecover: true,
                recover: async () =>
                {
                    using var original = await client.GetAsync($"/api/v1/media/{id}/original");
                    Assert.Equal(HttpStatusCode.OK, original.StatusCode);
                    fixture.Storage.FailVariants = false;
                });
        }
        finally { fixture.Storage.FailVariants = false; }
        using var thumbnail = await client.GetAsync($"/api/v1/media/{id}/thumbnail");
        Assert.Equal(HttpStatusCode.OK, thumbnail.StatusCode);
    }

    [Fact]
    public async Task DeletionDuringResizeCannotResurrectVariantObjects()
    {
        using var client = fixture.App.CreateClient(); await Register(client);
        var recipe = await CreateRecipe(client);
        using var uploaded = await Upload(client, recipe.Id, 1, "image/png", Image(MagickFormat.Png));
        var id = (await Data(uploaded)).GetProperty("id").GetGuid();
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        fixture.Storage.AfterUpload = async key =>
        {
            if (key.EndsWith("medium.webp", StringComparison.Ordinal)) { entered.SetResult(); await release.Task; }
        };
        await using var scope = fixture.App.Services.CreateAsyncScope();
        var resize = scope.ServiceProvider.GetRequiredService<ImageProcessingJob>().ProcessAsync(Guid.Empty, id, default);
        try
        {
            await entered.Task.WaitAsync(TimeSpan.FromSeconds(15));
            using var deleted = await Send(client, HttpMethod.Delete, $"/api/v1/recipes/{recipe.Id}/images/{id}", 2);
            Assert.Equal(HttpStatusCode.NoContent, deleted.StatusCode);
            var keys = await Keys(recipe.Id);
            await RunJob(Job.FromExpression<ObjectDeletionJob>(job => job.DeleteAsync(Guid.Empty, keys, CancellationToken.None)));
        }
        finally { fixture.Storage.AfterUpload = null; release.TrySetResult(); await resize; }
        Assert.Empty(await Keys(recipe.Id));
        using var guest = fixture.App.CreateClient();
        Assert.Equal(HttpStatusCode.NotFound, (await guest.GetAsync($"/api/v1/media/{id}/original")).StatusCode);
    }

    private async Task RunJob(Job job, bool failThenRecover = false, Func<Task>? recover = null)
    {
        var storage = fixture.App.Services.GetRequiredService<JobStorage>();
        var jobs = fixture.App.Services.GetRequiredService<IBackgroundJobClient>();
        var queue = "media-" + Guid.NewGuid().ToString("N");
        var id = jobs.Create(job, new EnqueuedState(queue));
        using var worker = new BackgroundJobServer(new BackgroundJobServerOptions
        { WorkerCount = 1, Queues = [queue], Activator = new ServiceActivator(fixture.App.Services.GetRequiredService<IServiceScopeFactory>()) }, storage);
        using var connection = storage.GetConnection();
        async Task WaitFor(string expected)
        {
            var deadline = DateTimeOffset.UtcNow.AddSeconds(30);
            while (connection.GetStateData(id)?.Name != expected && DateTimeOffset.UtcNow < deadline) await Task.Delay(50);
            Assert.Equal(expected, connection.GetStateData(id)?.Name);
        }
        if (failThenRecover)
        {
            foreach (var delay in new[] { 60, 300, 1800 })
            {
                await WaitFor("Scheduled");
                var state = connection.GetStateData(id)!;
                var enqueueAt = DateTimeOffset.FromUnixTimeMilliseconds(long.Parse(state.Data["EnqueueAt"], System.Globalization.CultureInfo.InvariantCulture));
                Assert.InRange((enqueueAt - DateTimeOffset.UtcNow).TotalSeconds, delay - 10, delay + 2);
                // Promote persisted retries without claiming the wall-clock backoff elapsed.
                Assert.True(jobs.ChangeState(id, new EnqueuedState(queue), "Scheduled"));
            }
            await WaitFor("Failed");
            await recover!();
            Assert.True(jobs.ChangeState(id, new EnqueuedState(queue), "Failed"));
        }
        await WaitFor("Succeeded");
    }

    private sealed class ServiceActivator(IServiceScopeFactory scopes) : JobActivator
    {
        public override JobActivatorScope BeginScope(JobActivatorContext context) => new Scope(scopes.CreateScope());
        private sealed class Scope(IServiceScope scope) : JobActivatorScope
        {
            public override object Resolve(Type type) => scope.ServiceProvider.GetRequiredService(type);
            public override void DisposeScope() => scope.Dispose();
        }
    }

    private async Task AssertImageCount(Guid recipeId, int count, long version)
    {
        await using var scope = fixture.App.Services.CreateAsyncScope(); var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        Assert.Equal(count, await db.RecipeImages.CountAsync(image => image.RecipeId == recipeId));
        Assert.Equal(version, (await db.Recipes.SingleAsync(recipe => recipe.Id == recipeId)).Version);
    }
    private async Task<string[]> Keys(Guid id)
    {
        var keys = new List<string>(); await foreach (var key in fixture.Storage.ListKeysAsync($"recipes/{id:N}/", default)) keys.Add(key);
        return keys.ToArray();
    }
    private static byte[] Image(MagickFormat format) { using var image = new MagickImage(MagickColors.Red, 32, 16); return image.ToByteArray(format); }
    private static async Task<JsonElement> Data(HttpResponseMessage response) => (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("data");
    private static async Task<string> Register(HttpClient client)
    {
        var email = $"media-{Guid.NewGuid():N}@media.test";
        using var registration = await client.PostAsJsonAsync("/api/v1/auth/register", new { email, displayName = "Media Author", password = Password });
        registration.EnsureSuccessStatusCode(); client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", (await Data(registration)).GetProperty("accessToken").GetString());
        return email;
    }
    private static async Task<(Guid Id, string Slug)> CreateRecipe(HttpClient client)
    {
        var categories = await client.GetFromJsonAsync<JsonElement>("/api/v1/categories");
        using var response = await client.PostAsJsonAsync("/api/v1/recipes", new { title = $"Media {Guid.NewGuid():N}", description = "Synthetic media fixture", categoryId = categories.GetProperty("data")[0].GetProperty("id").GetGuid(), prepTime = 1, cookTime = 0, servings = 1, difficulty = "easy" });
        response.EnsureSuccessStatusCode(); var data = await Data(response); return (data.GetProperty("id").GetGuid(), data.GetProperty("slug").GetString()!);
    }
    private static Task<HttpResponseMessage> Send(HttpClient client, HttpMethod method, string url, long version, object? body = null)
    {
        var request = new HttpRequestMessage(method, url); request.Headers.IfMatch.Add(new EntityTagHeaderValue($"\"{version}\""));
        if (body is not null) request.Content = JsonContent.Create(body);
        return client.SendAsync(request);
    }
    private static async Task<HttpResponseMessage> Upload(HttpClient client, Guid id, long version, string mime, byte[] bytes)
    {
        using var form = new MultipartFormDataContent(); var file = new ByteArrayContent(bytes); file.Headers.ContentType = new MediaTypeHeaderValue(mime); form.Add(file, "file", "../../unsafe.name");
        using var request = new HttpRequestMessage(HttpMethod.Post, $"/api/v1/recipes/{id}/images") { Content = form }; request.Headers.IfMatch.Add(new EntityTagHeaderValue($"\"{version}\""));
        return await client.SendAsync(request);
    }
}
