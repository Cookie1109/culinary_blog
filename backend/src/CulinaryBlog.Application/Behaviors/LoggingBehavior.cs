using MediatR;
using Microsoft.Extensions.Logging;

namespace CulinaryBlog.Application.Behaviors;

internal sealed class LoggingBehavior<TRequest, TResponse>(ILogger<LoggingBehavior<TRequest, TResponse>> logger)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private static readonly Action<ILogger, string, Exception?> HandlingRequest =
        LoggerMessage.Define<string>(LogLevel.Information, new EventId(1001, nameof(HandlingRequest)), "Handling application request {ApplicationRequest}");

    private static readonly Action<ILogger, string, Exception?> HandledRequest =
        LoggerMessage.Define<string>(LogLevel.Information, new EventId(1002, nameof(HandledRequest)), "Handled application request {ApplicationRequest}");

    private static readonly Action<ILogger, string, Exception?> RequestFailed =
        LoggerMessage.Define<string>(LogLevel.Error, new EventId(1003, nameof(RequestFailed)), "Application request {ApplicationRequest} failed");

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        var requestName = typeof(TRequest).Name;

        using var scope = logger.BeginScope(new Dictionary<string, object>
        {
            ["ApplicationRequest"] = requestName,
        });

        HandlingRequest(logger, requestName, null);

        try
        {
            var response = await next(cancellationToken).ConfigureAwait(false);
            HandledRequest(logger, requestName, null);
            return response;
        }
        catch (Exception exception)
        {
            RequestFailed(logger, requestName, exception);
            throw;
        }
    }
}
