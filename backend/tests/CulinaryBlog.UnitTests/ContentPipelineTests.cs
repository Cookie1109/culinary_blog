// Culinary Blog. Authorship is recorded in the repository history.

using CulinaryBlog.Application;
using CulinaryBlog.Application.Abstractions.Caching;
using CulinaryBlog.Application.Abstractions.Persistence;
using CulinaryBlog.Application.Auth;
using CulinaryBlog.Application.Content;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;

namespace CulinaryBlog.UnitTests;

/// <summary>Contains regression checks for content pipeline tests.</summary>
public sealed class ContentPipelineTests
{
    private readonly IAuthService auth = Substitute.For<IAuthService>();
    private readonly IContentService content = Substitute.For<IContentService>();
    private readonly IApplicationCache cache = Substitute.For<IApplicationCache>();
    private readonly IPublicContentRefresh refresh = Substitute.For<IPublicContentRefresh>();

    /// <summary>Verifies that invalid registration is rejected before calling service and keeps public field names.</summary>
    /// <returns>A task representing the operation and its result.</returns>
    [Fact]
    public async Task InvalidRegistrationIsRejectedBeforeCallingServiceAndKeepsPublicFieldNames()
    {
        using var provider = this.CreateProvider();
        var sender = provider.GetRequiredService<ISender>();
        var exception = await Assert.ThrowsAsync<ValidationException>(() => sender.Send(
            new RegisterCommand(new RegisterRequest(string.Empty, "invalid", "weak"), "127.0.0.1", "unit-test")));

        Assert.Contains(exception.Errors, error => error.PropertyName == "DisplayName");
        Assert.Contains(exception.Errors, error => error.PropertyName == "Email");
        Assert.Contains(exception.Errors, error => error.PropertyName == "Password");
        Assert.DoesNotContain(exception.Errors, error => error.PropertyName.StartsWith("Body.", StringComparison.Ordinal));
        Assert.Empty(this.auth.ReceivedCalls());
        Assert.Empty(this.cache.ReceivedCalls());
    }

    /// <summary>Verifies that missing command body returns validation error instead of dereferencing null.</summary>
    /// <returns>A task representing the operation and its result.</returns>
    [Fact]
    public async Task MissingCommandBodyReturnsValidationErrorInsteadOfDereferencingNull()
    {
        using var provider = this.CreateProvider();
        await Assert.ThrowsAsync<ValidationException>(() => provider.GetRequiredService<ISender>()
            .Send(new LoginCommand(null!, null, null)));
        Assert.Empty(this.auth.ReceivedCalls());
    }

    /// <summary>Verifies that category write checks admin inside application before mutation or invalidation.</summary>
    /// <returns>A task representing the operation and its result.</returns>
    [Fact]
    public async Task CategoryWriteChecksAdminInsideApplicationBeforeMutationOrInvalidation()
    {
        using var provider = this.CreateProvider();
        var exception = await Assert.ThrowsAsync<ContentProblemException>(() => provider.GetRequiredService<ISender>()
            .Send(new CreateCategoryCommand(new CategoryWriteRequest("MÃ³n chay", null, null))));

        Assert.Equal("FORBIDDEN", exception.Code);
        Assert.Empty(this.content.ReceivedCalls());
        Assert.Empty(this.cache.ReceivedCalls());
        Assert.Empty(this.refresh.ReceivedCalls());
    }

    /// <summary>Verifies that committed mutation invalidates both public tags and schedules refresh despite browser disconnect.</summary>
    /// <returns>A task representing the operation and its result.</returns>
    [Fact]
    public async Task CommittedMutationInvalidatesBothPublicTagsAndSchedulesRefreshDespiteBrowserDisconnect()
    {
        var events = new List<string>();
        using var cancellation = new CancellationTokenSource();
        var id = Guid.NewGuid();
        var body = new CategoryWriteRequest("MÃ³n chay", null, null);
        var result = new CategoryDto(id, body.Name, "mon-chay", null, null, 0, 0);
        this.content.UpdateCategoryAsync(id, body, cancellation.Token).Returns(_ =>
        {
            events.Add("commit");
            cancellation.Cancel();
            return Task.FromResult(result);
        });
        this.cache.RemoveByTagAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(call =>
        {
            Assert.False(call.Arg<CancellationToken>().IsCancellationRequested);
            events.Add(call.Arg<string>());
            return Task.CompletedTask;
        });
        this.refresh.ScheduleAsync(Arg.Any<CancellationToken>()).Returns(call =>
        {
            Assert.False(call.Arg<CancellationToken>().IsCancellationRequested);
            events.Add("refresh");
            return Task.CompletedTask;
        });
        using var provider = this.CreateProvider();

        Assert.Equal(result, await provider.GetRequiredService<ISender>().Send(new UpdateCategoryCommand(id, body, true), cancellation.Token));
        Assert.Equal(["commit", PublicCacheKeys.RecipesTag, PublicCacheKeys.CategoriesTag, "refresh"], events);
    }

    /// <summary>Verifies that failed mutation does not invalidate caches or schedule refresh.</summary>
    /// <returns>A task representing the operation and its result.</returns>
    [Fact]
    public async Task FailedMutationDoesNotInvalidateCachesOrScheduleRefresh()
    {
        this.content.CreateCategoryAsync(Arg.Any<CategoryWriteRequest>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromException<CategoryDto>(new InvalidOperationException("Database rejected the write")));
        using var provider = this.CreateProvider();
        await Assert.ThrowsAsync<InvalidOperationException>(() => provider.GetRequiredService<ISender>()
            .Send(new CreateCategoryCommand(new CategoryWriteRequest("MÃ³n chay", null, null), true)));

        Assert.Empty(this.cache.ReceivedCalls());
        Assert.Empty(this.refresh.ReceivedCalls());
    }

    /// <summary>Verifies that public category query uses cache and never runs mutation invalidation.</summary>
    /// <returns>A task representing the operation and its result.</returns>
    [Fact]
    public async Task PublicCategoryQueryUsesCacheAndNeverRunsMutationInvalidation()
    {
        IReadOnlyCollection<CategoryDto> cached = [new(Guid.NewGuid(), "MÃ³n chay", "mon-chay", null, null, 0, 2)];
        this.cache.GetAsync<IReadOnlyCollection<CategoryDto>>(PublicCacheKeys.Categories, Arg.Any<CancellationToken>()).Returns(cached);
        using var provider = this.CreateProvider();

        Assert.Same(cached, await provider.GetRequiredService<ISender>().Send(new GetCategoriesQuery()));
        Assert.Empty(this.content.ReceivedCalls());
        await this.cache.DidNotReceive().RemoveByTagAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
        Assert.Empty(this.refresh.ReceivedCalls());
    }

    /// <summary>Verifies that invalid query and upload metadata never reach service cache or scheduler.</summary>
    /// <returns>A task representing the operation and its result.</returns>
    [Fact]
    public async Task InvalidQueryAndUploadMetadataNeverReachServiceCacheOrScheduler()
    {
        using var provider = this.CreateProvider();
        var sender = provider.GetRequiredService<ISender>();
        await Assert.ThrowsAsync<ValidationException>(() => sender.Send(new ListMyRecipesQuery(Guid.NewGuid().ToString(), null, null, 0, 20)));
        await Assert.ThrowsAsync<ValidationException>(() => sender.Send(new UpdateCategoryCommand(Guid.Empty, new CategoryWriteRequest("Valid category", null, null), true)));
        await Assert.ThrowsAsync<ContentProblemException>(() => sender.Send(new UploadImageCommand(Guid.NewGuid(), Guid.NewGuid().ToString(), false, 1, Stream.Null, 6 * 1024 * 1024, "image/png", null, false)));
        Assert.Empty(this.content.ReceivedCalls());
        Assert.Empty(this.cache.ReceivedCalls());
        Assert.Empty(this.refresh.ReceivedCalls());
    }

    /// <summary>Verifies that post commit cache and scheduler failures do not turn committed mutation into failed command.</summary>
    /// <returns>A task representing the operation and its result.</returns>
    [Fact]
    public async Task PostCommitCacheAndSchedulerFailuresDoNotTurnCommittedMutationIntoFailedCommand()
    {
        this.cache.RemoveByTagAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromException(new InvalidOperationException("cache unavailable")));
        this.refresh.ScheduleAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromException(new InvalidOperationException("scheduler unavailable")));
        using var provider = this.CreateProvider();
        await provider.GetRequiredService<ISender>().Send(new CreateCategoryCommand(new CategoryWriteRequest("Món chay", null, null), true));
        await this.content.Received(1).CreateCategoryAsync(Arg.Any<CategoryWriteRequest>(), Arg.Any<CancellationToken>());
        await this.cache.Received(2).RemoveByTagAsync(Arg.Any<string>(), CancellationToken.None);
        await this.refresh.Received(1).ScheduleAsync(CancellationToken.None);
    }

    private ServiceProvider CreateProvider()
    {
        var services = new ServiceCollection();
        services.AddApplication();
        services.AddSingleton(typeof(ILogger<>), typeof(NullLogger<>));
        services.AddSingleton(this.auth);
        services.AddSingleton(this.content);
        services.AddSingleton(this.cache);
        services.AddSingleton(this.refresh);
        var version = Substitute.For<IPublicCacheVersion>();
        version.RefreshAsync(Arg.Any<CancellationToken>()).Returns(1L);
        services.AddSingleton(version);
        var actor = Substitute.For<IApplicationActor>();
        actor.GetAsync(Arg.Any<CancellationToken>()).Returns(new ApplicationActor(Guid.NewGuid().ToString(), true, ["Admin"]));
        services.AddSingleton(actor);
        services.AddSingleton(Substitute.For<IUnitOfWork>());
        return services.BuildServiceProvider();
    }
}
