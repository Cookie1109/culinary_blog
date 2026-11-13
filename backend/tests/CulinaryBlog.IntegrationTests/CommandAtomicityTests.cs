// Culinary Blog. Authorship is recorded in the repository history.

using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using CulinaryBlog.Domain.Recipes;
using CulinaryBlog.Infrastructure.Persistence;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;

namespace CulinaryBlog.IntegrationTests;

/// <summary>Contains regression checks for command atomicity tests.</summary>
/// <param name="factory">The factory supplied to this operation.</param>
public sealed class CommandAtomicityTests(AuthApiFactory factory) : IClassFixture<AuthApiFactory>
{
    /// <summary>Verifies that failure after first save rolls back composition version and outbox.</summary>
    /// <param name="operation">The operation supplied to this operation.</param>
    /// <returns>A task representing the operation and its result.</returns>
    [Theory]
    [InlineData("step")]
    [InlineData("primary")]
    [InlineData("delete-image")]
    public async Task FailureAfterFirstSaveRollsBackCompositionVersionAndOutbox(string operation)
    {
        var fault = new FailAfterSave();
        await using var custom = factory.WithWebHostBuilder(builder => builder.ConfigureTestServices(services =>
            services.AddDbContext<AppDbContext>((_, options) => options.AddInterceptors(fault))));
        using var client = custom.CreateClient();
        var email = $"atomic-{Guid.NewGuid():N}@example.test";
        using var registered = await client.PostAsJsonAsync("/api/v1/auth/register", new { displayName = "Atomic Author", email, password = "Valid#Password1" });
        var session = (await registered.Content.ReadFromJsonAsync<Envelope<Session>>())!.Data;
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", session.AccessToken);
        var recipeId = Guid.NewGuid();
        var firstId = Guid.NewGuid();
        var secondId = Guid.NewGuid();
        long version;
        int outboxCount;
        await using (var scope = custom.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var category = await db.Categories.FirstAsync();
            var recipe = Recipe.Create(recipeId, session.User.Id, $"atomic-{recipeId:N}", "Atomic recipe", "Description", category.Id, 10, 10, 2, RecipeDifficulty.Easy, "Instructions", null);
            db.Recipes.Add(recipe);
            db.RecipeSteps.AddRange(RecipeStep.Create(firstId, recipeId, 1, "First", "First step", null, null), RecipeStep.Create(secondId, recipeId, 2, "Second", "Second step", null, null));
            db.RecipeImages.AddRange(RecipeImage.Create(firstId, recipeId, $"test/{firstId:N}.png", "image/png", "First", true, 0), RecipeImage.Create(secondId, recipeId, $"test/{secondId:N}.png", "image/png", "Second", false, 1));
            await db.SaveChangesAsync();
            version = recipe.Version;
            outboxCount = await db.MediaOutbox.CountAsync();
        }

        var path = operation switch
        {
            "step" => $"/api/v1/recipes/{recipeId}/steps/{secondId}",
            "primary" => $"/api/v1/recipes/{recipeId}/images/{secondId}",
            _ => $"/api/v1/recipes/{recipeId}/images/{firstId}",
        };
        var imageMethod = operation == "primary" ? HttpMethod.Patch : HttpMethod.Delete;
        using var request = new HttpRequestMessage(operation == "step" ? HttpMethod.Put : imageMethod, path);
        request.Headers.IfMatch.Add(new EntityTagHeaderValue($"\"{version}\""));
        if (operation == "step")
        {
            request.Content = JsonContent.Create(new { title = "Changed", description = "Changed step", stepNumber = 1 });
        }

        if (operation == "primary")
        {
            request.Content = JsonContent.Create(new { isPrimary = true });
        }

        fault.Armed = true;
        using var response = await client.SendAsync(request);
        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        Assert.True(fault.Fired);
        await using var verify = custom.Services.CreateAsyncScope();
        var persisted = verify.ServiceProvider.GetRequiredService<AppDbContext>();
        Assert.Equal(version, (await persisted.Recipes.SingleAsync(recipe => recipe.Id == recipeId)).Version);
        var steps = await persisted.RecipeSteps.Where(step => step.RecipeId == recipeId).OrderBy(step => step.StepNumber).ToListAsync();
        Assert.Collection(steps, step => Assert.Equal(1, step.StepNumber), step => Assert.Equal(2, step.StepNumber));
        Assert.Equal("Second", steps[1].Title);
        var images = await persisted.RecipeImages.Where(image => image.RecipeId == recipeId).OrderBy(image => image.OrderIndex).ToListAsync();
        Assert.Equal(2, images.Count);
        Assert.Equal(firstId, Assert.Single(images, image => image.IsPrimary).Id);
        Assert.Equal(outboxCount, await persisted.MediaOutbox.CountAsync());
    }

    /// <summary>Verifies that public profile managed audit and sitemap reads do not save business state.</summary>
    /// <returns>A task representing the operation and its result.</returns>
    [Fact]
    public async Task PublicProfileManagedAuditAndSitemapReadsDoNotSaveBusinessState()
    {
        var fault = new FailAfterSave();
        await using var custom = factory.WithWebHostBuilder(builder => builder.ConfigureTestServices(services =>
            services.AddDbContext<AppDbContext>((_, options) => options.AddInterceptors(fault))));
        using var client = custom.CreateClient();
        var email = $"read-only-{Guid.NewGuid():N}@example.test";
        using var registered = await client.PostAsJsonAsync("/api/v1/auth/register", new { displayName = "Read Author", email, password = "Valid#Password1" });
        var userId = (await registered.Content.ReadFromJsonAsync<Envelope<Session>>())!.Data.User.Id;
        string categorySlug;
        await using (var scope = custom.Services.CreateAsyncScope())
        {
            var users = scope.ServiceProvider.GetRequiredService<Microsoft.AspNetCore.Identity.UserManager<CulinaryBlog.Infrastructure.Identity.ApplicationUser>>();
            var user = (await users.FindByIdAsync(userId))!;
            Assert.True((await users.AddToRoleAsync(user, "Admin")).Succeeded);
            categorySlug = await scope.ServiceProvider.GetRequiredService<AppDbContext>().Categories.Select(category => category.Slug).FirstAsync();
        }

        using var login = await client.PostAsJsonAsync("/api/v1/auth/login", new { email, password = "Valid#Password1" });
        var session = (await login.Content.ReadFromJsonAsync<Envelope<Session>>())!.Data;
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", session.AccessToken);
        fault.Armed = true;
        foreach (var path in new[] { "/api/v1/categories", $"/api/v1/categories/{categorySlug}", "/api/v1/recipes", "/api/v1/recipes/search?q=pho", "/api/v1/auth/me", "/api/v1/me/recipes", "/api/v1/admin/recipes", "/api/v1/admin/audit-logs" })
        {
            using var response = await client.GetAsync(path);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        using var sitemap = await client.GetAsync("/api/v1/sitemap.xml");
        Assert.Equal(HttpStatusCode.NotFound, sitemap.StatusCode);
        Assert.False(fault.Fired);
    }

    private sealed class FailAfterSave : SaveChangesInterceptor
    {
        public bool Armed { get; set; }
        public bool Fired { get; private set; }

        public override ValueTask<int> SavedChangesAsync(SaveChangesCompletedEventData eventData, int result, CancellationToken cancellationToken = default)
        {
            if (this.Armed)
            {
                this.Armed = false;
                this.Fired = true;
                throw new InvalidOperationException("Injected failure after SQL write, before command commit");
            }

            return ValueTask.FromResult(result);
        }
    }

    private sealed record Envelope<T>(T Data);

    private sealed record Session(string AccessToken, User User);

    private sealed record User(string Id);
}
