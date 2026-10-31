// Culinary Blog. Authorship is recorded in the repository history.

using MediatR;
using Microsoft.Extensions.Logging;

namespace CulinaryBlog.Application.Behaviors;

/// <summary>Represents the logging behavior.</summary>
/// <param name="logger">The logger supplied to this operation.</param>
/// <typeparam name="TRequest">The request type.</typeparam>
/// <typeparam name="TResponse">The response type.</typeparam>
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

    /// <summary>Handles the logging behavior.</summary>
    /// <param name="request">The request supplied to this operation.</param>
    /// <param name="next">The next supplied to this operation.</param>
    /// <param name="cancellationToken">Cancels the operation when requested by the caller.</param>
    /// <returns>A task representing the operation and its result.</returns>
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
