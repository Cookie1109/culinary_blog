// Culinary Blog. Authorship is recorded in the repository history.

using CulinaryBlog.Application;
using CulinaryBlog.Application.Abstractions.Caching;
using CulinaryBlog.Application.Abstractions.Persistence;
using CulinaryBlog.Application.Auth;
using CulinaryBlog.Application.Content;
using CulinaryBlog.Application.Features.Recipes.Commands;
using CulinaryBlog.Domain.Recipes;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;

namespace CulinaryBlog.UnitTests;

/// <summary>Contains regression checks for authorization pipeline tests.</summary>
public sealed class AuthorizationPipelineTests
{
    /// <summary>Verifies that application checks trusted actor before handler commit and invalidation.</summary>
    /// <param name="scenario">The scenario supplied to this operation.</param>
    /// <param name="allowed">The allowed supplied to this operation.</param>
    /// <returns>A task representing the operation and its result.</returns>
    [Theory]
    [InlineData("guest", false)]
    [InlineData("owner", true)]
    [InlineData("other", false)]
    [InlineData("admin", true)]
    [InlineData("inactive", false)]
    [InlineData("spoofed-owner", false)]
    [InlineData("spoofed-admin", false)]
    public async Task ApplicationChecksTrustedActorBeforeHandlerCommitAndInvalidation(string scenario, bool allowed)
    {
        var owner = Guid.NewGuid().ToString();
        var other = Guid.NewGuid().ToString();
        var actorId = scenario is "owner" or "inactive" ? owner : other;
        var claimedId = scenario == "spoofed-owner" ? owner : actorId;
        var claimedAdmin = scenario is "admin" or "spoofed-admin";
        var recipe = Recipe.Create(Guid.NewGuid(), owner, "owned-recipe", "Owned recipe", "Description", Guid.NewGuid(), 10, 10, 2, RecipeDifficulty.Easy, "Instructions", null);
        var actor = Substitute.For<IApplicationActor>();
        string[] roles = scenario == "admin" ? ["Admin"] : ["Author"];
        actor.GetAsync(Arg.Any<CancellationToken>()).Returns(scenario == "guest" ? null : new ApplicationActor(actorId.ToString(), scenario != "inactive", roles));
        var work = Substitute.For<IUnitOfWork>();
        work.Recipes.GetByIdAsync(recipe.Id, Arg.Any<CancellationToken>()).Returns(recipe);
        var content = Substitute.For<IContentService>();
        var cache = Substitute.For<IApplicationCache>();
        var refresh = Substitute.For<IPublicContentRefresh>();
        var services = new ServiceCollection().AddApplication();
        services.AddSingleton(typeof(ILogger<>), typeof(NullLogger<>));
        services.AddSingleton(actor);
        services.AddSingleton(work);
        services.AddSingleton(content);
        services.AddSingleton(cache);
        services.AddSingleton(refresh);
        var version = Substitute.For<IPublicCacheVersion>();
        version.RefreshAsync(Arg.Any<CancellationToken>()).Returns(1L);
        services.AddSingleton(version);
        using var provider = services.BuildServiceProvider();
        var command = new PublishRecipeCommand(recipe.Id, claimedId, claimedAdmin, recipe.Version);
        var sender = provider.GetRequiredService<ISender>();
        if (allowed)
        {
            await sender.Send(command);
            await content.Received(1).PublishRecipeAsync(recipe.Id, claimedId, claimedAdmin, recipe.Version, Arg.Any<CancellationToken>());
        }
        else
        {
            var exception = await Record.ExceptionAsync(() => sender.Send(command));
            Assert.True(exception is AuthProblemException or ContentProblemException);
            Assert.Empty(content.ReceivedCalls());
            await work.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
            Assert.Empty(cache.ReceivedCalls());
            Assert.Empty(refresh.ReceivedCalls());
        }
    }
}
