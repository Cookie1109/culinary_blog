// Culinary Blog. Authorship is recorded in the repository history.

using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using CulinaryBlog.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using SkiaSharp;

namespace CulinaryBlog.IntegrationTests;

/// <summary>Contains regression checks for category image upload and cleanup.</summary>
/// <param name="factory">The factory supplied to this operation.</param>
public sealed class CategoryImageApiTests(AuthApiFactory factory) : IClassFixture<AuthApiFactory>
{
    private const string Password = "Synthetic#Category123";

    private readonly WebApplicationFactory<Program> app =
        factory.WithWebHostBuilder(builder => builder.UseSetting("RateLimiting:UploadPermitLimit", "100"));

    /// <summary>Verifies that replacing or clearing an image deletes the file that is no longer used.</summary>
    /// <returns>A task representing the operation and its result.</returns>
    [Fact]
    public async Task ReplacingOrClearingImageDeletesUnusedFile()
    {
        using var client = await this.AdminClientAsync();
        string first = await UploadAsync(client);
        var id = await CreateCategoryAsync(client, first);
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync(first)).StatusCode);

        string second = await UploadAsync(client);
        await UpdateCategoryAsync(client, id, second);
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync(first)).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync(second)).StatusCode);

        await UpdateCategoryAsync(client, id, null);
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync(second)).StatusCode);
    }

    /// <summary>Verifies that an image shared by two categories survives when one stops using it.</summary>
    /// <returns>A task representing the operation and its result.</returns>
    [Fact]
    public async Task SharedImageIsKeptWhileAnotherCategoryUsesIt()
    {
        using var client = await this.AdminClientAsync();
        string shared = await UploadAsync(client);
        var firstId = await CreateCategoryAsync(client, shared);
        await CreateCategoryAsync(client, shared);

        await UpdateCategoryAsync(client, firstId, null);
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync(shared)).StatusCode);
    }

    /// <summary>Verifies that a malformed multipart body is a client error rather than a server error.</summary>
    /// <returns>A task representing the operation and its result.</returns>
    [Fact]
    public async Task MalformedMultipartIsRejectedAsBadRequest()
    {
        using var client = await this.AdminClientAsync();
        using var content = new ByteArrayContent(Encoding.ASCII.GetBytes("--x\r\ngarbage"));
        content.Headers.ContentType = MediaTypeHeaderValue.Parse("multipart/form-data; boundary=x");
        using var response = await client.PostAsync("/api/v1/categories/image", content);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    /// <summary>Verifies that only administrators can upload category images.</summary>
    /// <returns>A task representing the operation and its result.</returns>
    [Fact]
    public async Task OnlyAdministratorsCanUploadCategoryImages()
    {
        using var guest = this.app.CreateClient();
        using var anonymous = await guest.PostAsync("/api/v1/categories/image", ImageForm());
        Assert.Equal(HttpStatusCode.Unauthorized, anonymous.StatusCode);

        using var author = this.app.CreateClient();
        await RegisterAsync(author, $"category-author-{Guid.NewGuid():N}@example.test");
        using var forbidden = await author.PostAsync("/api/v1/categories/image", ImageForm());
        Assert.Equal(HttpStatusCode.Forbidden, forbidden.StatusCode);

        using var missing = await guest.GetAsync($"/api/v1/media/category/{Guid.NewGuid():N}.png");
        Assert.Equal(HttpStatusCode.NotFound, missing.StatusCode);
    }

    private static MultipartFormDataContent ImageForm()
    {
        using var bitmap = new SKBitmap(64, 48);
        bitmap.Erase(SKColors.OrangeRed);
        using var image = SKImage.FromBitmap(bitmap);
        using var data = image.Encode(SKEncodedImageFormat.Png, 100);
        var file = new ByteArrayContent(data.ToArray());
        file.Headers.ContentType = new MediaTypeHeaderValue("image/png");
        return new MultipartFormDataContent { { file, "file", "category.png" } };
    }

    private static async Task<string> UploadAsync(HttpClient client)
    {
        using var form = ImageForm();
        using var response = await client.PostAsync("/api/v1/categories/image", form);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        return body.GetProperty("data").GetProperty("url").GetString()!;
    }

    private static async Task<Guid> CreateCategoryAsync(HttpClient client, string? imageUrl)
    {
        using var response = await client.PostAsJsonAsync("/api/v1/categories", new
        {
            name = $"Ảnh {Guid.NewGuid():N}"[..20],
            description = (string?)null,
            imageUrl,
            orderIndex = 0,
        });
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        return body.GetProperty("data").GetProperty("id").GetGuid();
    }

    private static async Task UpdateCategoryAsync(HttpClient client, Guid id, string? imageUrl)
    {
        using var response = await client.PutAsJsonAsync($"/api/v1/categories/{id}", new
        {
            name = $"Đổi {Guid.NewGuid():N}"[..20],
            description = (string?)null,
            imageUrl,
            orderIndex = 0,
        });
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    private static async Task<string> RegisterAsync(HttpClient client, string email)
    {
        using var response = await client.PostAsJsonAsync("/api/v1/auth/register", new
        {
            email,
            password = Password,
            displayName = "Category Image Tester",
        });
        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        string token = body.GetProperty("data").GetProperty("accessToken").GetString()!;
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return token;
    }

    private async Task<HttpClient> AdminClientAsync()
    {
        var client = this.app.CreateClient();
        string email = $"category-admin-{Guid.NewGuid():N}@example.test";
        await RegisterAsync(client, email);
        await using (var scope = this.app.Services.CreateAsyncScope())
        {
            var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            Assert.True((await users.AddToRoleAsync((await users.FindByEmailAsync(email))!, "Admin")).Succeeded);
        }

        using var login = await client.PostAsJsonAsync("/api/v1/auth/login", new { email, password = Password });
        login.EnsureSuccessStatusCode();
        var body = await login.Content.ReadFromJsonAsync<JsonElement>();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer",
            body.GetProperty("data").GetProperty("accessToken").GetString());
        return client;
    }
}
