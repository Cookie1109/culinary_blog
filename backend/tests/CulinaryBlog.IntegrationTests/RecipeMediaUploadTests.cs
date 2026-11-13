// Culinary Blog. Authorship is recorded in the repository history.

using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using CulinaryBlog.Application.Audit;
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

/// <summary>Contains regression checks for recipe media upload tests.</summary>
/// <param name="fixture">The fixture supplied to this operation.</param>
public sealed class RecipeMediaUploadTests(RecipeMediaFixture fixture) : IClassFixture<RecipeMediaFixture>
{
    private const int Limit = 5 * 1024 * 1024;
    private const string Password = "Synthetic#Media123";

    /// <summary>Verifies that real storage and worker accept formats and produce exact variants.</summary>
    /// <param name="format">The format supplied to this operation.</param>
    /// <param name="mime">The mime supplied to this operation.</param>
    /// <returns>A task representing the operation and its result.</returns>
    [Theory]
    [InlineData(MagickFormat.Jpeg, "image/jpeg")]
    [InlineData(MagickFormat.Png, "image/png")]
    [InlineData(MagickFormat.WebP, "image/webp")]
    [InlineData(MagickFormat.Avif, "image/avif")]
    public async Task RealStorageAndWorkerAcceptFormatsAndProduceExactVariants(MagickFormat format, string mime)
    {
        using var client = fixture.App.CreateClient();
        await Register(client);
        var (id1, _) = await CreateRecipe(client);
        using var uploaded = await Upload(client, id1, 1, mime, Image(format));
        Assert.Equal(HttpStatusCode.Created, uploaded.StatusCode);
        var image = await Data(uploaded);
        Assert.True(image.GetProperty("isPrimary").GetBoolean());
        var id = image.GetProperty("id").GetGuid();
        await this.RunJob(Job.FromExpression<ImageProcessingJob>(job => job.ProcessAsync(Guid.Empty, id, CancellationToken.None)));
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
        string key = $"recipes/{id1:N}/{id:N}/original.{format.ToString().ToLowerInvariant().Replace("jpeg", "jpg", StringComparison.Ordinal)}";
        Assert.True(await fixture.Storage.ExistsAsync(key, default));
        using var direct = await guest.GetAsync($"http://{fixture.Minio.Hostname}:{fixture.Minio.GetMappedPublicPort(9000)}/{RecipeMediaFixture.Bucket}/{key}");
        Assert.Equal(HttpStatusCode.Forbidden, direct.StatusCode);
        using var anonymous = fixture.App.CreateClient();
        using var privateMedia = await anonymous.GetAsync($"/api/v1/media/{id}/original");
        Assert.Equal(HttpStatusCode.NotFound, privateMedia.StatusCode);
    }

    /// <summary>Verifies that deleting recipe atomically hides images and queues real storage cleanup.</summary>
    /// <returns>A task representing the operation and its result.</returns>
    [Fact]
    public async Task DeletingRecipeAtomicallyHidesImagesAndQueuesRealStorageCleanup()
    {
        using var owner = fixture.App.CreateClient();
        await Register(owner);
        var (recipeId, _) = await CreateRecipe(owner);
        using var uploaded = await Upload(owner, recipeId, 1, "image/png", Image(MagickFormat.Png));
        var imageId = (await Data(uploaded)).GetProperty("id").GetGuid();
        await this.RunJob(Job.FromExpression<ImageProcessingJob>(job => job.ProcessAsync(Guid.Empty, imageId, CancellationToken.None)));
        Assert.Equal(3, (await this.Keys(recipeId)).Length);
        using var stale = await Send(owner, HttpMethod.Delete, $"/api/v1/recipes/{recipeId}", 1);
        Assert.Equal(HttpStatusCode.Conflict, stale.StatusCode);
        await this.AssertImageCount(recipeId, 1, 2);
        await using (var unchanged = fixture.App.Services.CreateAsyncScope())
        {
            var messages = await unchanged.ServiceProvider.GetRequiredService<AppDbContext>().MediaOutbox
                .Where(item => item.Type == "objects.delete").ToListAsync();
            Assert.DoesNotContain(messages, item => item.Payload.Contains(recipeId.ToString("N"), StringComparison.Ordinal));
        }

        using var deleted = await Send(owner, HttpMethod.Delete, $"/api/v1/recipes/{recipeId}", 2);
        Assert.Equal(HttpStatusCode.NoContent, deleted.StatusCode);
        using var hidden = await owner.GetAsync($"/api/v1/media/{imageId}/original");
        Assert.Equal(HttpStatusCode.NotFound, hidden.StatusCode);
        await using var scope = fixture.App.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var storedRecipe = await db.Recipes.IgnoreQueryFilters().SingleAsync(item => item.Id == recipeId);
        var storedImage = await db.RecipeImages.IgnoreQueryFilters().SingleAsync(item => item.Id == imageId);
        Assert.True(storedRecipe.IsDeleted);
        Assert.NotNull(storedRecipe.DeletedAt);
        Assert.True(storedImage.IsDeleted);
        Assert.NotNull(storedImage.DeletedAt);
        Assert.Empty(await db.RecipeImages.Where(item => item.RecipeId == recipeId).ToListAsync());
        var deletions = await db.MediaOutbox.Where(item => item.Type == "objects.delete").ToListAsync();
        var message = Assert.Single(deletions, item => item.Payload.Contains(recipeId.ToString("N"), StringComparison.Ordinal));
        string[] keys = JsonSerializer.Deserialize<DeleteObjectsPayload>(message.Payload)!.ObjectKeys;
        Assert.Equal(3, keys.Length);
        await this.RunJob(Job.FromExpression<ObjectDeletionJob>(job => job.DeleteAsync(Guid.Empty, keys, CancellationToken.None)));
        Assert.Empty(await this.Keys(recipeId));

        // A retry is idempotent even after all objects have gone.
        await this.RunJob(Job.FromExpression<ObjectDeletionJob>(job => job.DeleteAsync(Guid.Empty, keys, CancellationToken.None)));
    }

    /// <summary>Verifies that invalid uploads and storage failure leave no rows objects or version change.</summary>
    /// <returns>A task representing the operation and its result.</returns>
    [Fact]
    public async Task InvalidUploadsAndStorageFailureLeaveNoRowsObjectsOrVersionChange()
    {
        using var client = fixture.App.CreateClient();
        await Register(client);
        var (recipeId, _) = await CreateRecipe(client);
        byte[] png = Image(MagickFormat.Png);
        foreach (var (mime, bytes) in new[]
        {
            ("image/png", []), ("image/png", new byte[Limit + 1]),
            ("image/png", [1, 2, 3]), ("image/jpeg", png), ("text/plain", png),
        })
        {
            using var invalid = await Upload(client, recipeId, 1, mime, bytes);
            Assert.Equal(HttpStatusCode.BadRequest, invalid.StatusCode);
        }

        fixture.Storage.FailAfterOriginal = true;
        try
        {
            using var failed = await Upload(client, recipeId, 1, "image/png", png);
            Assert.Equal(HttpStatusCode.ServiceUnavailable, failed.StatusCode);
        }
        finally
        {
            fixture.Storage.FailAfterOriginal = false;
        }

        await this.AssertImageCount(recipeId, 0, 1);
        Assert.Empty(await this.Keys(recipeId));
        fixture.Storage.FailAfterOriginal = true;
        fixture.Storage.FailDeletes = true;
        try
        {
            using var failed = await Upload(client, recipeId, 1, "image/png", png);
            Assert.Equal(HttpStatusCode.ServiceUnavailable, failed.StatusCode);
        }
        finally
        {
            fixture.Storage.FailAfterOriginal = false;
            fixture.Storage.FailDeletes = false;
        }

        await this.AssertImageCount(recipeId, 0, 1);
        await using (var cleanup = fixture.App.Services.CreateAsyncScope())
        {
            var messages = await cleanup.ServiceProvider.GetRequiredService<AppDbContext>().MediaOutbox.Where(item =>
                item.Type == "objects.delete").ToListAsync();
            var message = messages.Single(item => item.Payload.Contains(recipeId.ToString("N"), StringComparison.Ordinal));
            string[] keys = JsonSerializer.Deserialize<DeleteObjectsPayload>(message.Payload)!.ObjectKeys;
            await this.RunJob(Job.FromExpression<ObjectDeletionJob>(job => job.DeleteAsync(Guid.Empty, keys, CancellationToken.None)));
        }

        Assert.Empty(await this.Keys(recipeId));
        await fixture.Minio.StopAsync();
        try
        {
            using var failed = await Upload(client, recipeId, 1, "image/png", png);
            Assert.Equal(HttpStatusCode.ServiceUnavailable, failed.StatusCode);
        }
        finally
        {
            await fixture.Minio.StartAsync();
        }

        await this.AssertImageCount(recipeId, 0, 1);
        Assert.Empty(await this.Keys(recipeId));
        byte[] boundary = new byte[Limit];
        png.CopyTo(boundary, 0);
        using var accepted = await Upload(client, recipeId, 1, "image/png", boundary);
        Assert.Equal(HttpStatusCode.Created, accepted.StatusCode);
        await this.AssertImageCount(recipeId, 1, 2);
    }

    /// <summary>Verifies that ownership metadata and concurrent uploads preserve primary version and objects.</summary>
    /// <returns>A task representing the operation and its result.</returns>
    [Fact]
    public async Task OwnershipMetadataAndConcurrentUploadsPreservePrimaryVersionAndObjects()
    {
        using var owner = fixture.App.CreateClient();
        await Register(owner);
        var (recipeId, _) = await CreateRecipe(owner);
        using var other = fixture.App.CreateClient();
        await Register(other);
        using var guest = fixture.App.CreateClient();
        using var forbidden = await Upload(other, recipeId, 1, "image/png", Image(MagickFormat.Png));
        using var unauthenticated = await Upload(guest, recipeId, 1, "image/png", Image(MagickFormat.Png));
        Assert.Equal(HttpStatusCode.Forbidden, forbidden.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, unauthenticated.StatusCode);
        await this.AssertImageCount(recipeId, 0, 1);
        var responses = await Task.WhenAll(
            Upload(owner, recipeId, 1, "image/png", Image(MagickFormat.Png)),
            Upload(owner, recipeId, 1, "image/png", Image(MagickFormat.Png)));
        Assert.Single(responses, response => response.StatusCode == HttpStatusCode.Created);
        Assert.Single(responses, response => response.StatusCode == HttpStatusCode.Conflict);
        foreach (var response in responses)
        {
            response.Dispose();
        }

        await this.AssertImageCount(recipeId, 1, 2);
        Assert.Single(await this.Keys(recipeId));
        using var second = await Upload(owner, recipeId, 2, "image/png", Image(MagickFormat.Png));
        var secondId = (await Data(second)).GetProperty("id").GetGuid();
        using var patch = await Send(
            owner,
            HttpMethod.Patch,
            $"/api/v1/recipes/{recipeId}/images/{secondId}",
            3,
            new { altText = "Ảnh đã chỉnh", isPrimary = true, orderIndex = 0 });
        Assert.Equal(HttpStatusCode.OK, patch.StatusCode);
        await using (var verification = fixture.App.Services.CreateAsyncScope())
        {
            var image = await verification.ServiceProvider.GetRequiredService<AppDbContext>().RecipeImages.SingleAsync(item => item.Id == secondId);
            Assert.Equal("Ảnh đã chỉnh", image.AltText);
            Assert.True(image.IsPrimary);
            Assert.Equal(0, image.OrderIndex);
        }

        foreach (string? variant in new[] { "medium.webp", "thumbnail.webp" })
        {
            await using var content = new MemoryStream(Image(MagickFormat.WebP));
            await fixture.Storage.UploadAsync($"recipes/{recipeId:N}/{secondId:N}/{variant}", content, content.Length, "image/webp", default);
        }

        using var stale = await Send(owner, HttpMethod.Delete, $"/api/v1/recipes/{recipeId}/images/{secondId}", 3);
        Assert.Equal(HttpStatusCode.Conflict, stale.StatusCode);
        await this.AssertImageCount(recipeId, 2, 4);
        using var deleted = await Send(owner, HttpMethod.Delete, $"/api/v1/recipes/{recipeId}/images/{secondId}", 4);
        Assert.Equal(HttpStatusCode.NoContent, deleted.StatusCode);
        await this.AssertImageCount(recipeId, 1, 5);
        await using var scope = fixture.App.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var replacement = await db.RecipeImages.SingleAsync(image => image.RecipeId == recipeId);
        Assert.True(replacement.IsPrimary);
        Assert.Equal(0, replacement.OrderIndex);
        var deletions = await db.MediaOutbox.Where(message => message.Type == "objects.delete").ToListAsync();
        var payload = JsonSerializer.Deserialize<DeleteObjectsPayload>(deletions.Single(message =>
            message.Payload.Contains(secondId.ToString("N"), StringComparison.Ordinal)).Payload)!;
        await this.RunJob(Job.FromExpression<ObjectDeletionJob>(job => job.DeleteAsync(Guid.Empty, payload.ObjectKeys, CancellationToken.None)));
        Assert.Single(await this.Keys(recipeId));
    }

    /// <summary>Verifies that admin can upload for another author and guest cannot read archived or deleted media.</summary>
    /// <returns>A task representing the operation and its result.</returns>
    [Fact]
    public async Task AdminCanUploadForAnotherAuthorAndGuestCannotReadArchivedOrDeletedMedia()
    {
        using var owner = fixture.App.CreateClient();
        await Register(owner);
        var (id1, _) = await CreateRecipe(owner);
        using var admin = fixture.App.CreateClient();
        string email = await Register(admin);
        await using (var scope = fixture.App.Services.CreateAsyncScope())
        {
            var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            Assert.True((await users.AddToRoleAsync((await users.FindByEmailAsync(email))!, "Admin")).Succeeded);
        }

        using var login = await admin.PostAsJsonAsync("/api/v1/auth/login", new { email, password = Password });
        admin.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", (await Data(login)).GetProperty("accessToken").GetString());
        using var uploaded = await Upload(admin, id1, 1, "image/png", Image(MagickFormat.Png));
        Assert.Equal(HttpStatusCode.Created, uploaded.StatusCode);
        var id = (await Data(uploaded)).GetProperty("id").GetGuid();
        await this.RunJob(Job.FromExpression<ImageProcessingJob>(job => job.ProcessAsync(Guid.Empty, id, CancellationToken.None)));
        using var ingredient = await Send(owner, HttpMethod.Post, $"/api/v1/recipes/{id1}/ingredients", 2, new { name = "Gạo" });
        Assert.Equal(HttpStatusCode.Created, ingredient.StatusCode);
        using var step = await Send(owner, HttpMethod.Post, $"/api/v1/recipes/{id1}/steps", 3, new { title = "Nấu", description = "Nấu chín" });
        Assert.Equal(HttpStatusCode.Created, step.StatusCode);
        using var published = await Send(owner, HttpMethod.Patch, $"/api/v1/recipes/{id1}/publish", 4);
        Assert.Equal(HttpStatusCode.OK, published.StatusCode);
        using var guest = fixture.App.CreateClient();
        using var visible = await guest.GetAsync($"/api/v1/media/{id}/original");
        Assert.Equal(HttpStatusCode.OK, visible.StatusCode);
        Assert.True(visible.Headers.CacheControl?.NoStore);
        using var archived = await Send(owner, HttpMethod.Patch, $"/api/v1/recipes/{id1}/archive", 5);
        Assert.Equal(HttpStatusCode.OK, archived.StatusCode);
        foreach (string? variant in new[] { "original", "medium", "thumbnail" })
        {
            Assert.Equal(HttpStatusCode.NotFound, (await guest.GetAsync($"/api/v1/media/{id}/{variant}")).StatusCode);
        }

        using var deleted = await Send(owner, HttpMethod.Delete, $"/api/v1/recipes/{id1}", 6);
        Assert.Equal(HttpStatusCode.NoContent, deleted.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await owner.GetAsync($"/api/v1/media/{id}/original")).StatusCode);
    }

    /// <summary>Verifies that failed resize retries in hangfire and preserves original.</summary>
    /// <returns>A task representing the operation and its result.</returns>
    [Fact]
    public async Task FailedResizeRetriesInHangfireAndPreservesOriginal()
    {
        using var client = fixture.App.CreateClient();
        await Register(client);
        var (id1, _) = await CreateRecipe(client);
        string correlation = "resize-origin-" + Guid.NewGuid().ToString("N");
        client.DefaultRequestHeaders.Add("X-Correlation-ID", correlation);
        using var upload = await Upload(client, id1, 1, "image/png", Image(MagickFormat.Png));
        var id = (await Data(upload)).GetProperty("id").GetGuid();
        await using var scope = fixture.App.Services.CreateAsyncScope();
        var messages = await scope.ServiceProvider.GetRequiredService<AppDbContext>().MediaOutbox
            .Where(item => item.Type == "image.resize").ToListAsync();
        var outboxId = messages.Single(item => item.Payload.Contains(id.ToString(), StringComparison.Ordinal)).Id;
        fixture.Storage.FailVariants = true;
        try
        {
            await this.RunJob(
                Job.FromExpression<ImageProcessingJob>(job => job.ProcessAsync(outboxId, id, CancellationToken.None)),
                failThenRecover: true,
                recover: async () =>
                {
                    using var original = await client.GetAsync($"/api/v1/media/{id}/original");
                    Assert.Equal(HttpStatusCode.OK, original.StatusCode);
                    fixture.Storage.FailVariants = false;
                });
        }
        finally
        {
            fixture.Storage.FailVariants = false;
        }

        using var thumbnail = await client.GetAsync($"/api/v1/media/{id}/thumbnail");
        Assert.Equal(HttpStatusCode.OK, thumbnail.StatusCode);
        await this.AssertJobErrors(correlation, "ProcessingFailed", outboxId, $"/api/v1/recipes/{id1}/images");
    }

    /// <summary>Verifies that failed deletion retries keep the origin of the delete request.</summary>
    /// <returns>A task representing the operation and its result.</returns>
    [Fact]
    public async Task FailedDeletionRetriesKeepTheOriginOfTheDeleteRequest()
    {
        using var client = fixture.App.CreateClient();
        await Register(client);
        var (recipeId, _) = await CreateRecipe(client);
        using var upload = await Upload(client, recipeId, 1, "image/png", Image(MagickFormat.Png));
        var imageId = (await Data(upload)).GetProperty("id").GetGuid();
        string correlation = "delete-origin-" + Guid.NewGuid().ToString("N");
        client.DefaultRequestHeaders.Add("X-Correlation-ID", correlation);
        using var deleted = await Send(client, HttpMethod.Delete, $"/api/v1/recipes/{recipeId}/images/{imageId}", 2);
        Assert.Equal(HttpStatusCode.NoContent, deleted.StatusCode);
        await using var scope = fixture.App.Services.CreateAsyncScope();
        var messages = await scope.ServiceProvider.GetRequiredService<AppDbContext>().MediaOutbox
            .Where(item => item.Type == "objects.delete").ToListAsync();
        var message = messages.Single(item => item.Payload.Contains(imageId.ToString("N"), StringComparison.Ordinal));
        string[] keys = JsonSerializer.Deserialize<DeleteObjectsPayload>(message.Payload)!.ObjectKeys;
        fixture.Storage.FailDeletes = true;
        try
        {
            await this.RunJob(
                Job.FromExpression<ObjectDeletionJob>(job => job.DeleteAsync(message.Id, keys, CancellationToken.None)),
                failThenRecover: true,
                recover: () =>
                {
                    fixture.Storage.FailDeletes = false;
                    return Task.CompletedTask;
                });
        }
        finally
        {
            fixture.Storage.FailDeletes = false;
        }

        Assert.Empty(await this.Keys(recipeId));
        await this.AssertJobErrors(correlation, "DeletionFailed", message.Id, $"/api/v1/recipes/{recipeId}/images/{imageId}");
    }

    /// <summary>Verifies that deletion during resize cannot resurrect variant objects.</summary>
    /// <returns>A task representing the operation and its result.</returns>
    [Fact]
    public async Task DeletionDuringResizeCannotResurrectVariantObjects()
    {
        using var client = fixture.App.CreateClient();
        await Register(client);
        var (id1, _) = await CreateRecipe(client);
        using var uploaded = await Upload(client, id1, 1, "image/png", Image(MagickFormat.Png));
        var id = (await Data(uploaded)).GetProperty("id").GetGuid();
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        fixture.Storage.AfterUpload = async key =>
        {
            if (key.EndsWith("medium.webp", StringComparison.Ordinal))
            {
                entered.SetResult();
                await release.Task;
            }
        };
        await using var scope = fixture.App.Services.CreateAsyncScope();
        var resize = scope.ServiceProvider.GetRequiredService<ImageProcessingJob>().ProcessAsync(Guid.Empty, id, default);
        try
        {
            await entered.Task.WaitAsync(TimeSpan.FromSeconds(15));
            using var deleted = await Send(client, HttpMethod.Delete, $"/api/v1/recipes/{id1}/images/{id}", 2);
            Assert.Equal(HttpStatusCode.NoContent, deleted.StatusCode);
            string[] keys = await this.Keys(id1);
            await this.RunJob(Job.FromExpression<ObjectDeletionJob>(job => job.DeleteAsync(Guid.Empty, keys, CancellationToken.None)));
        }
        finally
        {
            fixture.Storage.AfterUpload = null;
            release.TrySetResult();
            await resize;
        }

        Assert.Empty(await this.Keys(id1));
        using var guest = fixture.App.CreateClient();
        Assert.Equal(HttpStatusCode.NotFound, (await guest.GetAsync($"/api/v1/media/{id}/original")).StatusCode);
    }

    /// <summary>Verifies profile uploads persist and only the current avatar remains publicly readable.</summary>
    /// <returns>A task representing the avatar lifecycle test.</returns>
    [Fact]
    public async Task ProfileAvatarUploadPersistsPublicImageAndReplacementHidesOldImage()
    {
        using var owner = fixture.App.CreateClient();
        await Register(owner);
        byte[] bytes = Image(MagickFormat.Png);
        using var uploaded = await UploadAvatar(owner, bytes);
        Assert.Equal(HttpStatusCode.OK, uploaded.StatusCode);
        var profile = await Data(uploaded);
        string avatar = profile.GetProperty("avatarUrl").GetString()!;
        Assert.Equal("Avatar Author", profile.GetProperty("displayName").GetString());
        using var guest = fixture.App.CreateClient();
        string path = new Uri(avatar).AbsolutePath;
        using var image = await guest.GetAsync(path);
        Assert.Equal(HttpStatusCode.OK, image.StatusCode);
        Assert.Equal("image/png", image.Content.Headers.ContentType!.MediaType);
        Assert.Equal(bytes, await image.Content.ReadAsByteArrayAsync());
        using var replaced = await UploadAvatar(owner, bytes);
        Assert.Equal(HttpStatusCode.OK, replaced.StatusCode);
        string replacement = (await Data(replaced)).GetProperty("avatarUrl").GetString()!;
        Assert.NotEqual(avatar, replacement);
        using var obsolete = await guest.GetAsync(path);
        Assert.Equal(HttpStatusCode.NotFound, obsolete.StatusCode);
        string current = new Uri(replacement).AbsolutePath;
        using var currentImage = await guest.GetAsync(current);
        Assert.Equal(HttpStatusCode.OK, currentImage.StatusCode);
        using var unknown = await guest.GetAsync($"/api/v1/avatars/unknown/{Path.GetFileName(current)}");
        Assert.Equal(HttpStatusCode.NotFound, unknown.StatusCode);
        using var cleared = await owner.PatchAsJsonAsync("/api/v1/auth/me", new { avatarUrl = (string?)null });
        Assert.Equal(HttpStatusCode.OK, cleared.StatusCode);
        using var removed = await guest.GetAsync(current);
        Assert.Equal(HttpStatusCode.NotFound, removed.StatusCode);
    }

    /// <summary>Verifies guests and invalid image signatures cannot modify a profile avatar.</summary>
    /// <returns>A task representing the avatar validation and authorization test.</returns>
    [Fact]
    public async Task ProfileAvatarUploadRejectsGuestsAndInvalidFilesWithoutChangingProfile()
    {
        using var guest = fixture.App.CreateClient();
        byte[] bytes = Image(MagickFormat.Png);
        using var forbidden = await UploadAvatar(guest, bytes);
        Assert.Equal(HttpStatusCode.Unauthorized, forbidden.StatusCode);
        using var owner = fixture.App.CreateClient();
        await Register(owner);
        using var uploaded = await UploadAvatar(owner, bytes);
        Assert.Equal(HttpStatusCode.OK, uploaded.StatusCode);
        string avatar = (await Data(uploaded)).GetProperty("avatarUrl").GetString()!;
        using var invalid = await UploadAvatar(owner, [1, 2, 3]);
        Assert.Equal(HttpStatusCode.BadRequest, invalid.StatusCode);
        using var mismatched = await UploadAvatar(owner, bytes, "image/jpeg");
        Assert.Equal(HttpStatusCode.BadRequest, mismatched.StatusCode);
        using var profile = await owner.GetAsync("/api/v1/auth/me");
        Assert.Equal(avatar, (await Data(profile)).GetProperty("avatarUrl").GetString());
    }

    private static async Task<HttpResponseMessage> UploadAvatar(HttpClient client, byte[] bytes, string mime = "image/png")
    {
        using var form = new MultipartFormDataContent();
        var file = new ByteArrayContent(bytes);
        file.Headers.ContentType = new MediaTypeHeaderValue(mime);
        form.Add(file, "file", "avatar.png");
        form.Add(new StringContent("Avatar Author"), "displayName");
        return await client.PostAsync("/api/v1/auth/me/avatar", form);
    }

    private static byte[] Image(MagickFormat format)
    {
        using var image = new MagickImage(MagickColors.Red, 32, 16);
        return image.ToByteArray(format);
    }

    private static async Task<JsonElement> Data(HttpResponseMessage response) => (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("data");

    private static async Task<string> Register(HttpClient client)
    {
        string email = $"media-{Guid.NewGuid():N}@media.test";
        using var registration = await client.PostAsJsonAsync("/api/v1/auth/register", new { email, displayName = "Media Author", password = Password });
        registration.EnsureSuccessStatusCode();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", (await Data(registration)).GetProperty("accessToken").GetString());
        return email;
    }

    private static async Task<(Guid Id, string Slug)> CreateRecipe(HttpClient client)
    {
        var categories = await client.GetFromJsonAsync<JsonElement>("/api/v1/categories");
        using var response = await client.PostAsJsonAsync("/api/v1/recipes", new { title = $"Media {Guid.NewGuid():N}", description = "Synthetic media fixture", categoryId = categories.GetProperty("data")[0].GetProperty("id").GetGuid(), prepTime = 1, cookTime = 0, servings = 1, difficulty = "easy" });
        response.EnsureSuccessStatusCode();
        var data = await Data(response);
        return (data.GetProperty("id").GetGuid(), data.GetProperty("slug").GetString()!);
    }

    private static Task<HttpResponseMessage> Send(HttpClient client, HttpMethod method, string url, long version, object? body = null)
    {
        var request = new HttpRequestMessage(method, url);
        request.Headers.IfMatch.Add(new EntityTagHeaderValue($"\"{version}\""));
        if (body is not null)
        {
            request.Content = JsonContent.Create(body);
        }

        return client.SendAsync(request);
    }

    private static async Task<HttpResponseMessage> Upload(HttpClient client, Guid id, long version, string mime, byte[] bytes)
    {
        using var form = new MultipartFormDataContent();
        var file = new ByteArrayContent(bytes);
        file.Headers.ContentType = new MediaTypeHeaderValue(mime);
        form.Add(file, "file", "../../unsafe.name");
        using var request = new HttpRequestMessage(HttpMethod.Post, $"/api/v1/recipes/{id}/images") { Content = form };
        request.Headers.IfMatch.Add(new EntityTagHeaderValue($"\"{version}\""));
        return await client.SendAsync(request);
    }

    private async Task AssertJobErrors(string correlation, string eventName, Guid outboxId, string requestPath)
    {
        await using var scope = fixture.App.Services.CreateAsyncScope();
        var history = await scope.ServiceProvider.GetRequiredService<IAuditLogService>()
            .ListAuditLogsAsync("Error", correlation, 1, 100, default);
        var errors = history.Data.Where(entry => entry.EventName == eventName).ToArray();
        Assert.Equal(4, errors.Length);
        Assert.All(errors, entry =>
        {
            Assert.Equal(correlation, entry.CorrelationId);
            Assert.True(Guid.TryParse(entry.UserId, out _));
            Assert.Equal(requestPath, entry.RequestPath);
            Assert.Equal(outboxId.ToString(), entry.Properties!["OutboxId"]);
        });
    }

    private async Task RunJob(Job job, bool failThenRecover = false, Func<Task>? recover = null)
    {
        var storage = fixture.App.Services.GetRequiredService<JobStorage>();
        var jobs = fixture.App.Services.GetRequiredService<IBackgroundJobClient>();
        string queue = "media-" + Guid.NewGuid().ToString("N");
        string id = jobs.Create(job, new EnqueuedState(queue));
        using var worker = new BackgroundJobServer(
            new BackgroundJobServerOptions
            { WorkerCount = 1, Queues = [queue], Activator = new ServiceActivator(fixture.App.Services.GetRequiredService<IServiceScopeFactory>()) },
            storage);
        using var connection = storage.GetConnection();
        async Task WaitFor(string expected)
        {
            var deadline = DateTimeOffset.UtcNow.AddSeconds(30);
            while (connection.GetStateData(id)?.Name != expected && DateTimeOffset.UtcNow < deadline)
            {
                await Task.Delay(50);
            }

            Assert.Equal(expected, connection.GetStateData(id)?.Name);
        }

        if (failThenRecover)
        {
            foreach (int delay in new[] { 60, 300, 1800 })
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

    private async Task AssertImageCount(Guid recipeId, int count, long version)
    {
        await using var scope = fixture.App.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        Assert.Equal(count, await db.RecipeImages.CountAsync(image => image.RecipeId == recipeId));
        Assert.Equal(version, (await db.Recipes.SingleAsync(recipe => recipe.Id == recipeId)).Version);
    }

    private async Task<string[]> Keys(Guid id)
    {
        var keys = new List<string>();
        await foreach (string key in fixture.Storage.ListKeysAsync($"recipes/{id:N}/", default))
        {
            keys.Add(key);
        }

        return [.. keys];
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
}
