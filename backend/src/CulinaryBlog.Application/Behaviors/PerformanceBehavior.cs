using System.Diagnostics;
using MediatR;
using Microsoft.Extensions.Logging;

namespace CulinaryBlog.Application.Behaviors;

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
