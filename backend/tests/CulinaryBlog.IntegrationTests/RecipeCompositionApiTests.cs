using System.Collections.Concurrent;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using CulinaryBlog.Application.Media;
using CulinaryBlog.Infrastructure.Jobs;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace CulinaryBlog.IntegrationTests;

public sealed class RecipeCompositionApiTests(AuthApiFactory factory) : IClassFixture<AuthApiFactory>
{
    [Fact]
    public async Task OwnerCanComposeRecipeAndStepReorderStaysContinuous()
    {
        using var client = factory.CreateClient();
        var owner = await RegisterAsync(client, $"compose-{Guid.NewGuid():N}@example.com");
        var other = await RegisterAsync(client, $"compose-other-{Guid.NewGuid():N}@example.com");
        var categoryId = await GetCategoryIdAsync(client);
        SetToken(client, owner.AccessToken);
        var recipe = await CreateRecipeAsync(client, categoryId, "Composition integration recipe");

        using var ingredientResponse = await SendJsonAsync(
            client,
            HttpMethod.Post,
            $"/api/v1/recipes/{recipe.Id}/ingredients",
            1,
            new { name = "Muối", quantity = (decimal?)null, unit = (string?)null, notes = "Vừa đủ", orderIndex = 0 });
        Assert.Equal(HttpStatusCode.Created, ingredientResponse.StatusCode);
        Assert.Equal("\"2\"", ingredientResponse.Headers.ETag?.Tag);

        using var firstStepResponse = await SendJsonAsync(
            client,
            HttpMethod.Post,
            $"/api/v1/recipes/{recipe.Id}/steps",
            2,
            new { title = "Sơ chế", description = "Rửa sạch nguyên liệu", timerMinutes = 5 });
        var firstStep = (await firstStepResponse.Content.ReadFromJsonAsync<MutationEnvelope<StepResponse>>())!.Data;
        Assert.Equal(1, firstStep.StepNumber);

        using var secondStepResponse = await SendJsonAsync(
            client,
            HttpMethod.Post,
            $"/api/v1/recipes/{recipe.Id}/steps",
            3,
            new { title = "Nấu", description = "Nấu đến khi chín", timerMinutes = 20 });
        var secondStep = (await secondStepResponse.Content.ReadFromJsonAsync<MutationEnvelope<StepResponse>>())!.Data;
        Assert.Equal(2, secondStep.StepNumber);

        using var reorderResponse = await SendJsonAsync(
            client,
            HttpMethod.Put,
            $"/api/v1/recipes/{recipe.Id}/steps/{secondStep.Id}",
            4,
            new { title = secondStep.Title, description = secondStep.Description, timerMinutes = 20, stepNumber = 1 });
        Assert.Equal(HttpStatusCode.OK, reorderResponse.StatusCode);

        using var detailResponse = await client.GetAsync($"/api/v1/me/recipes/{recipe.Id}");
        var detail = (await detailResponse.Content.ReadFromJsonAsync<DataEnvelope<RecipeDetailResponse>>())!.Data;
        Assert.Collection(
            detail.Steps,
            step => { Assert.Equal(secondStep.Id, step.Id); Assert.Equal(1, step.StepNumber); },
            step => { Assert.Equal(firstStep.Id, step.Id); Assert.Equal(2, step.StepNumber); });
        Assert.Null(Assert.Single(detail.Ingredients).Quantity);

        SetToken(client, other.AccessToken);
        using var forbidden = await SendJsonAsync(
            client,
            HttpMethod.Post,
            $"/api/v1/recipes/{recipe.Id}/ingredients",
            5,
            new { name = "Không được phép", orderIndex = 1 });
        Assert.Equal(HttpStatusCode.Forbidden, forbidden.StatusCode);
    }

    [Fact]
    public async Task ImageUploadValidatesContentAndKeepsDraftMediaPrivate()
    {
        var storage = new InMemoryFileStorage();
        using var customFactory = factory.WithWebHostBuilder(builder => builder.ConfigureServices(services =>
        {
            services.RemoveAll<IFileStorageService>();
            services.AddSingleton<IFileStorageService>(storage);
        }));
        using var client = customFactory.CreateClient();
        var owner = await RegisterAsync(client, $"image-{Guid.NewGuid():N}@example.com");
        var categoryId = await GetCategoryIdAsync(client);
        SetToken(client, owner.AccessToken);
        var recipe = await CreateRecipeAsync(client, categoryId, "Secure image integration recipe");

        using var invalidMime = await UploadAsync(client, recipe.Id, 1, "text/plain", "dish.txt", [1, 2, 3, 4]);
        var mimeProblem = await invalidMime.Content.ReadFromJsonAsync<Problem>();
        Assert.Equal(HttpStatusCode.BadRequest, invalidMime.StatusCode);
        Assert.Equal("FILE_MIME_INVALID", mimeProblem?.Code);

        using var spoofed = await UploadAsync(client, recipe.Id, 1, "image/png", "../../spoof.png", [1, 2, 3, 4]);
        var spoofedProblem = await spoofed.Content.ReadFromJsonAsync<Problem>();
        Assert.Equal(HttpStatusCode.BadRequest, spoofed.StatusCode);
        Assert.Equal("FILE_SIGNATURE_INVALID", spoofedProblem?.Code);
        Assert.Empty(storage.Keys);

        using var oversized = await UploadAsync(client, recipe.Id, 1, "image/png", "large.png", new byte[(5 * 1024 * 1024) + 1]);
        var oversizedProblem = await oversized.Content.ReadFromJsonAsync<Problem>();
        Assert.Equal(HttpStatusCode.BadRequest, oversized.StatusCode);
        Assert.Equal("FILE_SIZE_EXCEEDED", oversizedProblem?.Code);
        Assert.Empty(storage.Keys);

        var png = Convert.FromBase64String("iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mNk+A8AAQUBAScY42YAAAAASUVORK5CYII=");
        using var uploaded = await UploadAsync(client, recipe.Id, 1, "image/png", "../../dish.png", png);
        var image = (await uploaded.Content.ReadFromJsonAsync<MutationEnvelope<ImageResponse>>())!;
        Assert.Equal(HttpStatusCode.Created, uploaded.StatusCode);
        Assert.Equal(2, image.Meta.RecipeVersion);
        Assert.All(storage.Keys, key => Assert.StartsWith($"recipes/{recipe.Id:N}/", key, StringComparison.Ordinal));

        using (var scope = customFactory.Services.CreateScope())
        {
            var resizeJob = scope.ServiceProvider.GetRequiredService<ImageProcessingJob>();
            await resizeJob.ProcessAsync(Guid.NewGuid(), image.Data.Id, CancellationToken.None);
            await resizeJob.ProcessAsync(Guid.NewGuid(), image.Data.Id, CancellationToken.None);
        }
        Assert.Equal(3, storage.Keys.Length);

        using var secondUpload = await UploadAsync(client, recipe.Id, 2, "image/png", "second.png", png);
        var secondImage = (await secondUpload.Content.ReadFromJsonAsync<MutationEnvelope<ImageResponse>>())!;
        Assert.Equal(HttpStatusCode.Created, secondUpload.StatusCode);

        using var rateLimited = await UploadAsync(client, recipe.Id, 3, "image/png", "limited.png", png);
        Assert.Equal(HttpStatusCode.TooManyRequests, rateLimited.StatusCode);
        Assert.True(rateLimited.Headers.Contains("Retry-After"));

        using var firstPrimaryRequest = CreateImagePatch(recipe.Id, image.Data.Id, 3);
        using var secondPrimaryRequest = CreateImagePatch(recipe.Id, secondImage.Data.Id, 3);
        var primaryResponses = await Task.WhenAll(client.SendAsync(firstPrimaryRequest), client.SendAsync(secondPrimaryRequest));
        Assert.Single(primaryResponses, response => response.StatusCode == HttpStatusCode.OK);
        Assert.Single(primaryResponses, response => response.StatusCode == HttpStatusCode.Conflict);
        foreach (var response in primaryResponses)
        {
            response.Dispose();
        }

        SetToken(client, null);
        using var anonymousMedia = await client.GetAsync(image.Data.OriginalUrl);
        Assert.Equal(HttpStatusCode.NotFound, anonymousMedia.StatusCode);

        SetToken(client, owner.AccessToken);
        using var ownerMedia = await client.GetAsync(image.Data.OriginalUrl);
        Assert.Equal(HttpStatusCode.OK, ownerMedia.StatusCode);
        Assert.Equal("image/png", ownerMedia.Content.Headers.ContentType?.MediaType);

        using var deleteRequest = new HttpRequestMessage(HttpMethod.Delete, $"/api/v1/recipes/{recipe.Id}/images/{image.Data.Id}");
        deleteRequest.Headers.IfMatch.Add(new EntityTagHeaderValue("\"4\""));
        using var deleted = await client.SendAsync(deleteRequest);
        Assert.Equal(HttpStatusCode.NoContent, deleted.StatusCode);
        Assert.Equal("\"5\"", deleted.Headers.ETag?.Tag);

        using var repeatDeleteRequest = new HttpRequestMessage(HttpMethod.Delete, $"/api/v1/recipes/{recipe.Id}/images/{image.Data.Id}");
        repeatDeleteRequest.Headers.IfMatch.Add(new EntityTagHeaderValue("\"5\""));
        using var repeated = await client.SendAsync(repeatDeleteRequest);
        Assert.Equal(HttpStatusCode.NoContent, repeated.StatusCode);

        using var deletionScope = customFactory.Services.CreateScope();
        var deletionJob = deletionScope.ServiceProvider.GetRequiredService<ObjectDeletionJob>();
        var keys = storage.Keys.ToArray();
        await deletionJob.DeleteAsync(Guid.NewGuid(), keys, CancellationToken.None);
        await deletionJob.DeleteAsync(Guid.NewGuid(), keys, CancellationToken.None);
    }

    private static async Task<HttpResponseMessage> UploadAsync(
        HttpClient client,
        Guid recipeId,
        long version,
        string contentType,
        string fileName,
        byte[] bytes)
    {
        var file = new ByteArrayContent(bytes);
        file.Headers.ContentType = new MediaTypeHeaderValue(contentType);
        var form = new MultipartFormDataContent();
        form.Add(file, "file", fileName);
        form.Add(new StringContent("Món ăn hoàn thiện"), "altText");
        form.Add(new StringContent("true"), "isPrimary");
        var request = new HttpRequestMessage(HttpMethod.Post, $"/api/v1/recipes/{recipeId}/images") { Content = form };
        request.Headers.IfMatch.Add(new EntityTagHeaderValue($"\"{version}\""));
        return await client.SendAsync(request);
    }

    private static async Task<HttpResponseMessage> SendJsonAsync(
        HttpClient client,
        HttpMethod method,
        string path,
        long version,
        object body)
    {
        var request = new HttpRequestMessage(method, path) { Content = JsonContent.Create(body) };
        request.Headers.IfMatch.Add(new EntityTagHeaderValue($"\"{version}\""));
        return await client.SendAsync(request);
    }

    private static HttpRequestMessage CreateImagePatch(Guid recipeId, Guid imageId, long version)
    {
        var request = new HttpRequestMessage(HttpMethod.Patch, $"/api/v1/recipes/{recipeId}/images/{imageId}")
        {
            Content = JsonContent.Create(new { altText = "Ảnh cạnh tranh", isPrimary = true, orderIndex = 0 }),
        };
        request.Headers.IfMatch.Add(new EntityTagHeaderValue($"\"{version}\""));
        return request;
    }

    private static async Task<RecipeResponse> CreateRecipeAsync(HttpClient client, Guid categoryId, string title)
    {
        using var response = await client.PostAsJsonAsync("/api/v1/recipes", new
        {
            title,
            description = "Recipe composition integration test.",
            categoryId,
            prepTime = 10,
            cookTime = 15,
            servings = 2,
            difficulty = "easy",
        });
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<DataEnvelope<RecipeResponse>>())!.Data;
    }

    private static async Task<Guid> GetCategoryIdAsync(HttpClient client)
    {
        SetToken(client, null);
        var response = await client.GetFromJsonAsync<DataEnvelope<CategoryResponse[]>>("/api/v1/categories");
        return response!.Data[0].Id;
    }

    private static async Task<AuthSession> RegisterAsync(HttpClient client, string email)
    {
        SetToken(client, null);
        using var response = await client.PostAsJsonAsync(
            "/api/v1/auth/register",
            new { displayName = "Composition Test", email, password = "Valid#Password1" });
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<DataEnvelope<AuthSession>>())!.Data;
    }

    private static void SetToken(HttpClient client, string? token) => client.DefaultRequestHeaders.Authorization = token is null
        ? null
        : new AuthenticationHeaderValue("Bearer", token);

    private sealed record DataEnvelope<T>(T Data);
    private sealed record MutationEnvelope<T>(T Data, MutationMeta Meta);
    private sealed record MutationMeta(long RecipeVersion);
    private sealed record AuthSession(string AccessToken);
    private sealed record RecipeResponse(Guid Id);
    private sealed record CategoryResponse(Guid Id);
    private sealed record StepResponse(Guid Id, int StepNumber, string Title, string Description);
    private sealed record IngredientResponse(decimal? Quantity);
    private sealed record RecipeDetailResponse(IReadOnlyList<StepResponse> Steps, IReadOnlyList<IngredientResponse> Ingredients);
    private sealed record ImageResponse(Guid Id, string OriginalUrl);
    private sealed record Problem(string Code);

    private sealed class InMemoryFileStorage : IFileStorageService
    {
        private readonly ConcurrentDictionary<string, byte[]> _files = new(StringComparer.Ordinal);

        public string[] Keys => _files.Keys.ToArray();

        public async Task UploadAsync(string objectKey, Stream content, long length, string contentType, CancellationToken cancellationToken)
        {
            using var output = new MemoryStream();
            await content.CopyToAsync(output, cancellationToken);
            _files[objectKey] = output.ToArray();
        }

        public Task<Stream> OpenReadAsync(string objectKey, CancellationToken cancellationToken) =>
            Task.FromResult<Stream>(new MemoryStream(_files[objectKey], writable: false));

        public Task DeleteAsync(string objectKey, CancellationToken cancellationToken)
        {
            _files.TryRemove(objectKey, out _);
            return Task.CompletedTask;
        }

        public Task<bool> ExistsAsync(string objectKey, CancellationToken cancellationToken) =>
            Task.FromResult(_files.ContainsKey(objectKey));

        public async IAsyncEnumerable<string> ListKeysAsync(
            string prefix,
            [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken)
        {
            foreach (var key in _files.Keys.Where(key => key.StartsWith(prefix, StringComparison.Ordinal)))
            {
                cancellationToken.ThrowIfCancellationRequested();
                yield return key;
            }
            await Task.CompletedTask;
        }
    }
}
