// Culinary Blog. Authorship is recorded in the repository history.

using CulinaryBlog.Application;
using CulinaryBlog.Application.Abstractions.Caching;
using CulinaryBlog.Application.Abstractions.Persistence;
using CulinaryBlog.Application.Audit;
using CulinaryBlog.Application.Auth;
using CulinaryBlog.Application.Content;
using CulinaryBlog.Application.Features.Recipes.Commands;
using CulinaryBlog.Application.Features.Recipes.Queries;
using CulinaryBlog.Application.Media;
using CulinaryBlog.Domain.Recipes;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;

namespace CulinaryBlog.UnitTests;

/// <summary>Contains regression checks for application request tests.</summary>
public sealed class ApplicationRequestTests : IDisposable
{
    private readonly IAuthService auth = Substitute.For<IAuthService>();
    private readonly IContentService content = Substitute.For<IContentService>();
    private readonly IApplicationCache cache = Substitute.For<IApplicationCache>();
    private readonly IApplicationActor actor = Substitute.For<IApplicationActor>();
    private readonly IUnitOfWork unit = Substitute.For<IUnitOfWork>();
    private readonly IFileStorageService storage = Substitute.For<IFileStorageService>();
    private readonly IAuditLogService audit = Substitute.For<IAuditLogService>();
    private readonly IRecipeSearchRepository search = Substitute.For<IRecipeSearchRepository>();
    private readonly ServiceProvider provider;
    private readonly string userId = Guid.NewGuid().ToString();
    private readonly Guid recipeId = Guid.NewGuid();
    private readonly Guid childId = Guid.NewGuid();

    /// <summary>Initializes a new instance of the <see cref="ApplicationRequestTests"/> class.</summary>
    public ApplicationRequestTests()
    {
        var services = new ServiceCollection().AddApplication();
        services.AddSingleton(typeof(ILogger<>), typeof(NullLogger<>));
        services.AddSingleton(this.auth).AddSingleton(this.content).AddSingleton(this.cache).AddSingleton(this.actor)
            .AddSingleton(this.unit).AddSingleton(this.storage).AddSingleton(this.audit).AddSingleton(this.search);
        services.AddSingleton(Substitute.For<IPublicContentRefresh>());
        var location = Substitute.For<ISitemapLocation>();
        location.ObjectKey.Returns("sitemap.xml");
        services.AddSingleton(location);
        this.provider = services.BuildServiceProvider();
        this.cache.GetAsync<IReadOnlyCollection<CategoryDto>>(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(Task.FromResult<IReadOnlyCollection<CategoryDto>?>(null));
        this.actor.GetAsync(Arg.Any<CancellationToken>()).Returns(new ApplicationActor(this.userId, true, ["Admin", "Author"]));
        this.unit.Recipes.GetByIdAsync(this.recipeId, Arg.Any<CancellationToken>()).Returns(
            Recipe.Create(this.recipeId, this.userId, "fresh-soup", "Fresh soup", "Warm soup", Guid.NewGuid(), 1, 0, 1, RecipeDifficulty.Easy, null, null));
    }

    private ISender Sender => this.provider.GetRequiredService<ISender>();

    /// <summary>Verifies that auth commands return service session and preserve cancellation and client metadata.</summary>
    /// <returns>A task representing the operation and its result.</returns>
    [Fact]
    public async Task AuthCommandsReturnServiceSessionAndPreserveCancellationAndClientMetadata()
    {
        using var cancellation = new CancellationTokenSource();
        var token = cancellation.Token;
        var user = new UserDto(this.userId, "author@example.test", "Author", null, null, ["Author"], false, true, DateTimeOffset.UtcNow);
        var session = new AuthSessionDto("access", "refresh", DateTimeOffset.UtcNow.AddMinutes(5), user);
        var registration = new RegisterRequest("Author", user.Email, "Password#123");
        var login = new LoginRequest(user.Email, registration.Password);
        this.auth.RegisterAsync(registration, "127.0.0.1", "browser", token).Returns(session);
        this.auth.LoginAsync(login, "127.0.0.1", "browser", token).Returns(session);
        this.auth.LoginWithGoogleAsync("provider-id", "127.0.0.1", "browser", token).Returns(session);
        this.auth.RefreshAsync("refresh", "127.0.0.1", "browser", token).Returns(session);
        Assert.Same(session, await this.Sender.Send(new RegisterCommand(registration, "127.0.0.1", "browser"), token));
        Assert.Same(session, await this.Sender.Send(new LoginCommand(login, "127.0.0.1", "browser"), token));
        Assert.Same(session, await this.Sender.Send(new GoogleLoginCommand(new("provider-id"), "127.0.0.1", "browser"), token));
        Assert.Same(session, await this.Sender.Send(new RefreshSessionCommand(new("refresh"), "127.0.0.1", "browser"), token));
        Assert.Equal(this.userId, session.User.Id);
        Assert.True(session.ExpiresAt > DateTimeOffset.UtcNow);
        this.auth.GetCurrentUserAsync(this.userId, token).Returns(user);
        Assert.Same(user, await this.Sender.Send(new GetCurrentUserQuery(this.userId), token));
        var profile = new UpdateProfileRequest("Updated", null, "Bio", true, true, true);
        this.auth.UpdateCurrentUserAsync(this.userId, profile, token).Returns(user with { DisplayName = "Updated", Bio = "Bio" });
        Assert.Equal("Updated", (await this.Sender.Send(new UpdateProfileCommand(this.userId, profile), token)).DisplayName);
        await this.Sender.Send(new ChangePasswordCommand(this.userId, new("Password#123", "Changed#123")), token);
        await this.Sender.Send(new ForgotPasswordCommand(new(user.Email)), token);
        await this.Sender.Send(new ResetPasswordCommand(new(user.Email, "reset-token", "Changed#123")), token);
        await this.auth.Received(1).ChangePasswordAsync(this.userId, new("Password#123", "Changed#123"), token);
        await this.auth.Received(1).ForgotPasswordAsync(new(user.Email), token);
        await this.auth.Received(1).ResetPasswordAsync(new(user.Email, "reset-token", "Changed#123"), token);
    }

    /// <summary>Verifies that logout rejects another owner before revocation.</summary>
    /// <param name="owner">The owner supplied to this operation.</param>
    /// <param name="caller">The caller supplied to this operation.</param>
    /// <param name="allowed">The allowed supplied to this operation.</param>
    /// <returns>A task representing the operation and its result.</returns>
    [Theory]
    [InlineData(null, "caller", true)]
    [InlineData("owner", null, true)]
    [InlineData("owner", "owner", true)]
    [InlineData("owner", "other", false)]
    public async Task LogoutRejectsAnotherOwnerBeforeRevocation(string? owner, string? caller, bool allowed)
    {
        this.auth.FindRefreshTokenOwnerAsync("refresh", Arg.Any<CancellationToken>()).Returns(owner);
        var command = new LogoutCommand(new("refresh"), "127.0.0.1", caller);
        if (allowed)
        {
            await this.Sender.Send(command);
            await this.auth.Received(1).LogoutAsync("refresh", "127.0.0.1", Arg.Any<CancellationToken>());
        }
        else
        {
            var error = await Assert.ThrowsAsync<ContentProblemException>(() => this.Sender.Send(command));
            Assert.Equal("FORBIDDEN", error.Code);
            await this.auth.DidNotReceive().LogoutAsync(Arg.Any<string>(), Arg.Any<string?>(), Arg.Any<CancellationToken>());
        }
    }

    /// <summary>Verifies that composition mutations pass aggregate version and never invalidate after conflict.</summary>
    /// <returns>A task representing the operation and its result.</returns>
    [Fact]
    public async Task CompositionMutationsPassAggregateVersionAndNeverInvalidateAfterConflict()
    {
        var ingredient = new IngredientWriteRequest("Salt", 1, "g");
        var ingredientUpdate = new IngredientUpdateRequest { Quantity = new(2) };
        var step = new StepWriteRequest("Boil", "Boil water", 0, "https://example.test/step.jpg", 1);
        var stepUpdate = new StepUpdateRequest { TimerMinutes = new(1) };
        var imageUpdate = new ImageMetadataRequest("Soup", true, 0);
        var error = new ContentProblemException("VERSION_CONFLICT", "Reload the recipe.", ContentProblemKind.Conflict);
        this.content.CreateIngredientAsync(this.recipeId, this.userId, true, 7, ingredient, Arg.Any<CancellationToken>()).Returns(Task.FromException<ChildMutationDto<IngredientDto>>(error));
        this.content.UpdateIngredientAsync(this.recipeId, this.childId, this.userId, true, 7, ingredientUpdate, Arg.Any<CancellationToken>()).Returns(Task.FromException<ChildMutationDto<IngredientDto>>(error));
        this.content.CreateStepAsync(this.recipeId, this.userId, true, 7, step, Arg.Any<CancellationToken>()).Returns(Task.FromException<ChildMutationDto<StepDto>>(error));
        this.content.UpdateStepAsync(this.recipeId, this.childId, this.userId, true, 7, stepUpdate, Arg.Any<CancellationToken>()).Returns(Task.FromException<ChildMutationDto<StepDto>>(error));
        this.content.UploadImageAsync(this.recipeId, this.userId, true, 7, Stream.Null, 1, "image/png", "Soup", true, Arg.Any<CancellationToken>()).Returns(Task.FromException<ChildMutationDto<RecipeImageDto>>(error));
        this.content.UpdateImageAsync(this.recipeId, this.childId, this.userId, true, 7, imageUpdate, Arg.Any<CancellationToken>()).Returns(Task.FromException<ChildMutationDto<RecipeImageDto>>(error));
        object[] commands = [
            new CreateIngredientCommand(this.recipeId, this.userId, true, 7, ingredient),
            new UpdateIngredientCommand(this.recipeId, this.childId, this.userId, true, 7, ingredientUpdate),
            new CreateStepCommand(this.recipeId, this.userId, true, 7, step),
            new UpdateStepCommand(this.recipeId, this.childId, this.userId, true, 7, stepUpdate),
            new UploadImageCommand(this.recipeId, this.userId, true, 7, Stream.Null, 1, "image/png", "Soup", true),
            new UpdateImageCommand(this.recipeId, this.childId, this.userId, true, 7, imageUpdate),];
        foreach (var command in commands)
        {
            Assert.Same(error, await Assert.ThrowsAsync<ContentProblemException>(() => this.Sender.Send(command)));
        }

        Assert.Empty(this.cache.ReceivedCalls());
        this.content.DeleteIngredientAsync(this.recipeId, this.childId, this.userId, true, 7, Arg.Any<CancellationToken>()).Returns(8L);
        this.content.DeleteStepAsync(this.recipeId, this.childId, this.userId, true, 8, Arg.Any<CancellationToken>()).Returns(9L);
        this.content.DeleteImageAsync(this.recipeId, this.childId, this.userId, true, 9, Arg.Any<CancellationToken>()).Returns(10L);
        Assert.Equal(8, await this.Sender.Send(new DeleteIngredientCommand(this.recipeId, this.childId, this.userId, true, 7)));
        Assert.Equal(9, await this.Sender.Send(new DeleteStepCommand(this.recipeId, this.childId, this.userId, true, 8)));
        Assert.Equal(10, await this.Sender.Send(new DeleteImageCommand(this.recipeId, this.childId, this.userId, true, 9)));
    }

    /// <summary>Verifies that recipe lifecycle preserves service errors and does not refresh uncommitted data.</summary>
    /// <returns>A task representing the operation and its result.</returns>
    [Fact]
    public async Task RecipeLifecyclePreservesServiceErrorsAndDoesNotRefreshUncommittedData()
    {
        var create = new RecipeCreateRequest(
            "Fresh soup",
            "Warm soup",
            Guid.NewGuid(),
            1,
            0,
            1,
            RecipeDifficulty.Easy,
            Ingredients: [new("Salt")],
            Steps: [new("Boil", "Boil water")]);
        var update = new RecipeUpdateRequest { Title = new("New title") };
        var error = new ContentProblemException("RECIPE_NOT_FOUND", "Missing recipe.", ContentProblemKind.NotFound);
        this.content.CreateRecipeAsync(this.userId, create, Arg.Any<CancellationToken>()).Returns(Task.FromException<RecipeDto>(error));
        this.content.UpdateRecipeAsync(this.recipeId, this.userId, true, 1, update, Arg.Any<CancellationToken>()).Returns(Task.FromException<RecipeDto>(error));
        this.content.UnpublishRecipeAsync(this.recipeId, this.userId, true, 1, Arg.Any<CancellationToken>()).Returns(Task.FromException<RecipeDto>(error));
        this.content.ArchiveRecipeAsync(this.recipeId, this.userId, true, 1, Arg.Any<CancellationToken>()).Returns(Task.FromException<RecipeDto>(error));
        this.content.UnarchiveRecipeAsync(this.recipeId, this.userId, true, 1, Arg.Any<CancellationToken>()).Returns(Task.FromException<RecipeDto>(error));
        object[] commands = [new CreateRecipeCommand(this.userId, create), new UpdateRecipeCommand(this.recipeId, this.userId, true, 1, update),
            new UnpublishRecipeCommand(this.recipeId, this.userId, true, 1), new ArchiveRecipeCommand(this.recipeId, this.userId, true, 1),
            new UnarchiveRecipeCommand(this.recipeId, this.userId, true, 1),];
        foreach (var command in commands)
        {
            Assert.Same(error, await Assert.ThrowsAsync<ContentProblemException>(() => this.Sender.Send(command)));
        }

        Assert.Empty(this.cache.ReceivedCalls());
    }

    /// <summary>Verifies that public and managed queries keep filters and sort while private reads bypass cache.</summary>
    /// <returns>A task representing the operation and its result.</returns>
    [Fact]
    public async Task PublicAndManagedQueriesKeepFiltersAndSortWhilePrivateReadsBypassCache()
    {
        var page = new PageEnvelope<RecipeDto>([], new(2, 10, 11, 2, false, true));
        this.search.ListPublishedRecipesAsync(null, null, null, null, null, null, "-createdAt", 2, 10, null, null, Arg.Any<CancellationToken>()).Returns(page);
        Assert.Same(page, await this.Sender.Send(new ListPublishedRecipesQuery(null, null, null, null, null, null, null, 2, 10)));
        var category = new CategoryDto(Guid.NewGuid(), "Soups", "soups", null, null, 0, 11);
        var detail = new CategoryDetailEnvelope(new(category, []), page.Meta);
        this.content.GetCategoryAsync("soups", 2, 10, Arg.Any<CancellationToken>(), "-title").Returns(detail);
        Assert.Same(detail, await this.Sender.Send(new GetCategoryQuery("soups", 2, 10, SortBy: "title", SortOrder: "desc")));
        this.content.ListCategoriesAsync(Arg.Any<CancellationToken>()).Returns([category]);
        Assert.Single(await this.Sender.Send(new GetCategoriesQuery()));
        this.cache.ClearReceivedCalls();
        this.content.ListMyRecipesAsync(this.userId, RecipeStatus.Draft, "soup", 2, 10, Arg.Any<CancellationToken>()).Returns(page);
        this.content.ListAdminRecipesAsync(RecipeStatus.Draft, this.userId, "soup", 2, 10, Arg.Any<CancellationToken>()).Returns(page);
        Assert.Same(page, await this.Sender.Send(new ListMyRecipesQuery(this.userId, RecipeStatus.Draft, "soup", 2, 10)));
        Assert.Same(page, await this.Sender.Send(new ListAdminRecipesQuery(RecipeStatus.Draft, this.userId, "soup", 2, 10)));
        var missing = new ContentProblemException("RECIPE_NOT_FOUND", "Missing recipe.", ContentProblemKind.NotFound);
        this.content.GetPublishedRecipeAsync("missing", Arg.Any<CancellationToken>()).Returns(Task.FromException<RecipeDto>(missing));
        Assert.Same(missing, await Assert.ThrowsAsync<ContentProblemException>(() => this.Sender.Send(new GetPublishedRecipeQuery("missing"))));
        this.cache.ClearReceivedCalls();
        this.content.GetMyRecipeAsync(this.recipeId, this.userId, Arg.Any<CancellationToken>()).Returns(Task.FromException<RecipeDto>(missing));
        this.content.GetAdminRecipeAsync(this.recipeId, Arg.Any<CancellationToken>()).Returns(Task.FromException<RecipeDto>(missing));
        Assert.Same(missing, await Assert.ThrowsAsync<ContentProblemException>(() => this.Sender.Send(new GetMyRecipeQuery(this.recipeId, this.userId))));
        Assert.Same(missing, await Assert.ThrowsAsync<ContentProblemException>(() => this.Sender.Send(new GetAdminRecipeQuery(this.recipeId))));
        Assert.Empty(this.cache.ReceivedCalls());
        await this.Sender.Send(new DeleteCategoryCommand(category.Id, true));
        await this.content.Received(1).DeleteCategoryAsync(category.Id, Arg.Any<CancellationToken>());
        await this.Sender.Send(new ListAuditLogsQuery("Warning", "write", 1, 10));
        await this.audit.Received(1).ListAuditLogsAsync("Warning", "write", 1, 10, Arg.Any<CancellationToken>());
    }

    /// <summary>Verifies that sitemap reads only existing artifact.</summary>
    /// <param name="exists">The exists supplied to this operation.</param>
    /// <returns>A task representing the operation and its result.</returns>
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task SitemapReadsOnlyExistingArtifact(bool exists)
    {
        this.storage.ExistsAsync("sitemap.xml", Arg.Any<CancellationToken>()).Returns(exists);
        this.storage.OpenReadAsync("sitemap.xml", Arg.Any<CancellationToken>()).Returns(Stream.Null);
        Assert.Same(exists ? Stream.Null : null, await this.Sender.Send(new GetSitemapQuery()));
        await this.storage.Received(exists ? 1 : 0).OpenReadAsync("sitemap.xml", Arg.Any<CancellationToken>());
    }

    /// <summary>Verifies that media uses authenticated actor instead of caller claims.</summary>
    /// <param name="active">The active supplied to this operation.</param>
    /// <param name="admin">The admin supplied to this operation.</param>
    /// <returns>A task representing the operation and its result.</returns>
    [Theory]
    [InlineData(false, false)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public async Task MediaUsesAuthenticatedActorInsteadOfCallerClaims(bool active, bool admin)
    {
        this.actor.GetAsync(Arg.Any<CancellationToken>()).Returns(new ApplicationActor(this.userId, active, admin ? ["Admin"] : ["Author"]));
        var file = new MediaFileDto(Stream.Null, "image/png");
        this.content.OpenMediaAsync(this.childId, "original", active ? this.userId : null, active && admin, Arg.Any<CancellationToken>()).Returns(file);
        Assert.Same(file, await this.Sender.Send(new OpenMediaQuery(this.childId, "original", "forged", true)));
    }

    /// <summary>Verifies that dispose.</summary>
    public void Dispose() => this.provider.Dispose();
}
