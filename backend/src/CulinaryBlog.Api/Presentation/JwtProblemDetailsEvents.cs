using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

namespace CulinaryBlog.Api.Presentation;

internal static class JwtProblemDetailsEvents
{
    private const string AuthenticationFailureKey = "JwtAuthenticationFailure";

    public static JwtBearerEvents Create()
    {
        return new JwtBearerEvents
        {
            OnAuthenticationFailed = context =>
            {
                context.HttpContext.Items[AuthenticationFailureKey] = context.Exception;
                return Task.CompletedTask;
            },
            OnChallenge = async context =>
            {
                context.HandleResponse();
                var failure = context.HttpContext.Items[AuthenticationFailureKey] as Exception;
                var code = failure is SecurityTokenExpiredException ? "AUTH_TOKEN_EXPIRED" :
                    string.IsNullOrWhiteSpace(context.Request.Headers.Authorization) ? "AUTH_TOKEN_MISSING" : "AUTH_TOKEN_INVALID";
                await WriteAsync(context.HttpContext, StatusCodes.Status401Unauthorized, code, "Authentication required")
                    .ConfigureAwait(false);
            },
            OnForbidden = context => WriteAsync(
                context.HttpContext,
                StatusCodes.Status403Forbidden,
                "FORBIDDEN",
                "Access denied"),
        };
    }

    private static Task WriteAsync(HttpContext context, int status, string code, string title)
    {
        context.Response.StatusCode = status;
        context.Response.ContentType = "application/problem+json";
        return context.Response.WriteAsJsonAsync(new
        {
            type = $"https://culinaryblog.local/problems/{code}",
            title,
            status,
            code,
            correlationId = context.TraceIdentifier,
            traceId = context.TraceIdentifier,
        });
    }
}
