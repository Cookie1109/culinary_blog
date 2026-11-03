using CulinaryBlog.Application.Behaviors;
using CulinaryBlog.Application.Content;
using CulinaryBlog.Domain.Common;
using FluentValidation;
using Microsoft.Extensions.Logging;

namespace CulinaryBlog.UnitTests;

public sealed class ApplicationLoggingTests
{
    [Theory]
    [InlineData("validation", LogLevel.Warning)]
    [InlineData("domain", LogLevel.Warning)]
    [InlineData("forbidden", LogLevel.Warning)]
    [InlineData("storage", LogLevel.Error)]
    [InlineData("unexpected", LogLevel.Error)]
    public async Task ExpectedRejectionsAreWarningsAndUnexpectedFailuresAreErrors(string failure, LogLevel expected)
    {
        var capture = new CaptureLogger();
        var behavior = new LoggingBehavior<SecretCommand, string>(capture);
        Exception error = failure switch
        {
            "validation" => new ValidationException("Invalid input"),
            "domain" => new DomainException("RECIPE_INVALID_TRANSITION", "Invalid state"),
            "forbidden" => new ContentProblemException("FORBIDDEN", "Access denied", ContentProblemKind.Forbidden),
            "storage" => new ContentProblemException("FILE_UPLOAD_FAILED", "Storage unavailable", ContentProblemKind.ServiceUnavailable),
            _ => new IOException("Unexpected storage error"),
        };
        var thrown = await Assert.ThrowsAsync(error.GetType(), () => behavior.Handle(
            new SecretCommand("synthetic-password-must-not-log"), _ => Task.FromException<string>(error), default));
        Assert.Same(error, thrown);
        Assert.Equal(expected, capture.Entries[^1].Level);
        Assert.DoesNotContain(capture.Entries, entry => entry.Message.Contains("Handled application", StringComparison.Ordinal));
        Assert.All(capture.Entries, entry => Assert.DoesNotContain("synthetic-password-must-not-log", entry.Message, StringComparison.Ordinal));
    }

    [Fact]
    public async Task SuccessfulCommandLogsItsTypeWithoutPayload()
    {
        var capture = new CaptureLogger();
        var behavior = new LoggingBehavior<SecretCommand, string>(capture);
        Assert.Equal("ok", await behavior.Handle(new SecretCommand("synthetic-password-must-not-log"), _ => Task.FromResult("ok"), default));
        Assert.Equal(2, capture.Entries.Count);
        Assert.All(capture.Entries, entry =>
        {
            Assert.Equal(LogLevel.Information, entry.Level);
            Assert.Contains(nameof(SecretCommand), entry.Message, StringComparison.Ordinal);
            Assert.DoesNotContain("synthetic-password-must-not-log", entry.Message, StringComparison.Ordinal);
        });
    }

    private sealed record SecretCommand(string Password);

    private sealed class CaptureLogger : ILogger<LoggingBehavior<SecretCommand, string>>
    {
        public List<(LogLevel Level, string Message)> Entries { get; } = [];
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
        public bool IsEnabled(LogLevel logLevel) => true;
        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter) =>
            Entries.Add((logLevel, formatter(state, exception)));
    }
}
