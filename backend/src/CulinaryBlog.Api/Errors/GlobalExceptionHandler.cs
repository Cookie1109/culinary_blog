using CulinaryBlog.Application.Auth;
using CulinaryBlog.Domain.Common;
using FluentValidation;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace CulinaryBlog.Api.Errors;

internal sealed class GlobalExceptionHandler(
    IProblemDetailsService problemDetailsService,
    ILogger<GlobalExceptionHandler> logger) : IExceptionHandler
{
    private static readonly Action<ILogger, Exception?> UnhandledException =
        LoggerMessage.Define(LogLevel.Error, new EventId(2001, nameof(UnhandledException)), "Unhandled exception");

    private static readonly Action<ILogger, string, Exception?> RequestFailed =
        LoggerMessage.Define<string>(
            LogLevel.Warning,
            new EventId(2002, nameof(RequestFailed)),
            "Request failed with {ErrorCode}");

    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        var (status, code, title) = exception switch
        {
            ValidationException => (StatusCodes.Status400BadRequest, "VALIDATION_ERROR", "Validation failed"),
            AuthProblemException authException => MapAuthProblem(authException),
            DomainException domainException =>
                (StatusCodes.Status400BadRequest, domainException.Code, "Business rule violation"),
            KeyNotFoundException => (StatusCodes.Status404NotFound, "RESOURCE_NOT_FOUND", "Resource not found"),
            UnauthorizedAccessException => (StatusCodes.Status403Forbidden, "FORBIDDEN", "Access denied"),
            _ => (StatusCodes.Status500InternalServerError, "INTERNAL_ERROR", "Unexpected server error"),
        };

        if (status >= StatusCodes.Status500InternalServerError)
        {
            UnhandledException(logger, exception);
        }
        else
        {
            RequestFailed(logger, code, exception);
        }

        var problem = new ProblemDetails
        {
            Status = status,
            Title = title,
            Detail = status >= StatusCodes.Status500InternalServerError
                ? "An unexpected error occurred."
                : exception.Message,
            Type = $"https://culinaryblog.local/problems/{code}",
            Instance = httpContext.Request.Path,
        };
        problem.Extensions["code"] = code;
        problem.Extensions["traceId"] = httpContext.TraceIdentifier;
        problem.Extensions["correlationId"] = httpContext.TraceIdentifier;

        if (exception is ValidationException validationException)
        {
            problem.Extensions["errors"] = validationException.Errors
                .GroupBy(failure => failure.PropertyName)
                .ToDictionary(
                    group => group.Key,
                    group => group.Select(failure => failure.ErrorMessage).Distinct().ToArray());
        }

        httpContext.Response.StatusCode = status;
        return await problemDetailsService.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            ProblemDetails = problem,
            Exception = exception,
        }).ConfigureAwait(false);
    }

    private static (int Status, string Code, string Title) MapAuthProblem(AuthProblemException exception)
    {
        var status = exception.Code switch
        {
            "AUTH_EMAIL_EXISTS" or "AUTH_EXTERNAL_ACCOUNT_CONFLICT" => StatusCodes.Status409Conflict,
            "AUTH_ACCOUNT_LOCKED" => StatusCodes.Status423Locked,
            "AUTH_ACCOUNT_DISABLED" => StatusCodes.Status403Forbidden,
            "USER_NOT_FOUND" => StatusCodes.Status404NotFound,
            "AUTH_GOOGLE_TOKEN_INVALID" or "AUTH_GOOGLE_EMAIL_UNVERIFIED" => StatusCodes.Status400BadRequest,
            _ => StatusCodes.Status401Unauthorized,
        };
        return (status, exception.Code, status == StatusCodes.Status401Unauthorized ? "Authentication failed" : "Account request failed");
    }
}
