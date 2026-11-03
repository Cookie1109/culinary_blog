using CulinaryBlog.Application.Abstractions.Persistence;
using CulinaryBlog.Application.Behaviors;
using CulinaryBlog.Application.Content;
using CulinaryBlog.Application.Features.Recipes.Queries;
using CulinaryBlog.Domain.Recipes;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace CulinaryBlog.UnitTests;

public sealed class CompletionRegressionTests
{
    private static readonly string[] StepFields = ["Description", "StepNumber", "TimerMinutes", "Title"];
    private static readonly string[] ImageFields = ["AltText", "OrderIndex"];
    [Fact]
    public async Task PublicListPreservesAllFiltersPaginationAndCancellation()
    {
        var repository = Substitute.For<IRecipeSearchRepository>();
        var request = new ListPublishedRecipesQuery(Guid.NewGuid(), RecipeDifficulty.Hard, 45, 2, 5, 30, "-prepTime", 3, 20);
        using var cancellation = new CancellationTokenSource();
        var expected = new PageEnvelope<RecipeDto>([], new PageMeta(3, 20, 0, 1, false, true));
        repository.ListPublishedRecipesAsync(request.CategoryId, RecipeDifficulty.Hard, 45, 2, 5, 30, "-prepTime", 3, 20, cancellation.Token).Returns(expected);
        Assert.Same(expected, await new ListPublishedRecipesQueryHandler(repository).Handle(request, cancellation.Token));
        await repository.Received(1).ListPublishedRecipesAsync(request.CategoryId, RecipeDifficulty.Hard, 45, 2, 5, 30, "-prepTime", 3, 20, cancellation.Token);
    }

    [Theory]
    [InlineData(null, true)]
    [InlineData("https://example.test/step.png", true)]
    [InlineData("http://example.test/step.png", true)]
    [InlineData("javascript:alert(1)", false)]
    [InlineData("/private/step.png", false)]
    public async Task StepValidationAllowsOptionalSafeImagesAndRejectsExecutableUrls(string? imageUrl, bool valid)
    {
        var result = await new StepWriteRequestValidator().ValidateAsync(new StepWriteRequest("Cook", "Cook rice", 0, imageUrl, 1));
        Assert.Equal(valid, result.IsValid);
        if (!valid) Assert.Contains(result.Errors, error => error.PropertyName == "ImageUrl");
    }

    [Fact]
    public async Task CompositionRejectsInvalidFieldsAndAcceptsOptionalMetadata()
    {
        var steps = new StepWriteRequestValidator();
        var invalid = await steps.ValidateAsync(new StepWriteRequest("", "", -1, null, 0));
        Assert.Equal(StepFields, invalid.Errors.Select(error => error.PropertyName).Order().ToArray());
        Assert.True((await steps.ValidateAsync(new StepWriteRequest("Cook", "Cook rice", null, null))).IsValid);
        var images = new ImageMetadataRequestValidator();
        Assert.True((await images.ValidateAsync(new ImageMetadataRequest(null, null, null))).IsValid);
        Assert.True((await images.ValidateAsync(new ImageMetadataRequest("Rice", true, 0))).IsValid);
        var imageErrors = await images.ValidateAsync(new ImageMetadataRequest(new string('x', 201), false, -1));
        Assert.Equal(ImageFields, imageErrors.Errors.Select(error => error.PropertyName).Order().ToArray());
    }

    [Fact]
    public async Task SlowFailureStillWarnsWithoutLoggingRequestPayload()
    {
        var logger = new CaptureLogger();
        var behavior = new PerformanceBehavior<PrivateRequest, string>(logger);
        var error = new IOException("Synthetic failure");
        var thrown = await Assert.ThrowsAsync<IOException>(() => behavior.Handle(new PrivateRequest("synthetic-private-value"), async cancellationToken =>
        {
            await Task.Delay(550, cancellationToken);
            throw error;
        }, default));
        Assert.Same(error, thrown);
        Assert.Equal(LogLevel.Warning, logger.Level);
        Assert.Contains(nameof(PrivateRequest), logger.Message, StringComparison.Ordinal);
        Assert.DoesNotContain("synthetic-private-value", logger.Message, StringComparison.Ordinal);
    }

    private sealed record PrivateRequest(string Password);
    private sealed class CaptureLogger : ILogger<PerformanceBehavior<PrivateRequest, string>>
    {
        public LogLevel Level { get; private set; }
        public string Message { get; private set; } = string.Empty;
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
        public bool IsEnabled(LogLevel logLevel) => true;
        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            Level = logLevel;
            Message = formatter(state, exception);
        }
    }
}
