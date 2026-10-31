// Culinary Blog. Authorship is recorded in the repository history.

using System.Diagnostics;
using MediatR;
using Microsoft.Extensions.Logging;

namespace CulinaryBlog.Application.Behaviors;

/// <summary>Represents the performance behavior.</summary>
/// <param name="logger">The logger supplied to this operation.</param>
/// <typeparam name="TRequest">The request type.</typeparam>
/// <typeparam name="TResponse">The response type.</typeparam>
internal sealed class PerformanceBehavior<TRequest, TResponse>(ILogger<PerformanceBehavior<TRequest, TResponse>> logger)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private const long WarningThresholdMilliseconds = 500;
    private static readonly Action<ILogger, string, long, Exception?> SlowRequest =
        LoggerMessage.Define<string, long>(
            LogLevel.Warning,
            new EventId(1004, nameof(SlowRequest)),
            "Slow application request {ApplicationRequest} took {ElapsedMilliseconds} ms");

    /// <summary>Handles the performance behavior.</summary>
    /// <param name="request">The request supplied to this operation.</param>
    /// <param name="next">The next supplied to this operation.</param>
    /// <param name="cancellationToken">Cancels the operation when requested by the caller.</param>
    /// <returns>A task representing the operation and its result.</returns>
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        var stopwatch = Stopwatch.StartNew();

        try
        {
            return await next(cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            stopwatch.Stop();
            if (stopwatch.ElapsedMilliseconds > WarningThresholdMilliseconds)
            {
                SlowRequest(logger, typeof(TRequest).Name, stopwatch.ElapsedMilliseconds, null);
            }
        }
    }
}
