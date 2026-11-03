using CulinaryBlog.Application.Auth;
using CulinaryBlog.Application.Content;
using CulinaryBlog.Domain.Common;
using FluentValidation;
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

    private static readonly Action<ILogger, string, Exception?> RequestRejected =
        LoggerMessage.Define<string>(LogLevel.Warning, new EventId(1004, nameof(RequestRejected)), "Application request {ApplicationRequest} rejected");

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
        catch (Exception exception) when (exception is ValidationException or DomainException or UnauthorizedAccessException or KeyNotFoundException
            or ContentProblemException { Kind: not ContentProblemKind.ServiceUnavailable }
            or AuthProblemException { Code: not ("AUTH_GOOGLE_UNAVAILABLE" or "AUTH_EMAIL_UNAVAILABLE") })
        {
            RequestRejected(logger, requestName, exception);
            throw;
        }
        catch (Exception exception)
        {
            RequestFailed(logger, requestName, exception);
            throw;
        }
    }
}
