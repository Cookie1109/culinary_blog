// Culinary Blog. Authorship is recorded in the repository history.

using CulinaryBlog.Infrastructure.Identity;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;

namespace CulinaryBlog.Api.Presentation;

/// <summary>Represents the jwt problem details events.</summary>
internal static class JwtProblemDetailsEvents
{
    private const string AuthenticationFailureKey = "JwtAuthenticationFailure";

    /// <summary>Creates the jwt problem details events.</summary>
    /// <returns>The create result.</returns>
    public static JwtBearerEvents Create()
    {
        return new JwtBearerEvents
        {
            OnTokenValidated = async context =>
            {
                var userManager = context.HttpContext.RequestServices.GetRequiredService<UserManager<ApplicationUser>>();
                var userId = context.Principal?.FindFirst("sub")?.Value;
                var user = userId is null ? null : await userManager.FindByIdAsync(userId).ConfigureAwait(false);
                var stamp = context.Principal?.FindFirst("security_stamp")?.Value;
                if (user is null || string.IsNullOrEmpty(stamp) || stamp != user.SecurityStamp)
                {
                    context.Fail("The session is no longer valid.");
                }
            },
            OnAuthenticationFailed = context =>
            {
                context.HttpContext.Items[AuthenticationFailureKey] = context.Exception;
                return Task.CompletedTask;
            },
            OnChallenge = async context =>
            {
                context.HandleResponse();
                var failure = context.HttpContext.Items[AuthenticationFailureKey] as Exception;
                var missingTokenCode = string.IsNullOrWhiteSpace(context.Request.Headers.Authorization) ? "AUTH_TOKEN_MISSING" : "AUTH_TOKEN_INVALID";
                var code = failure is SecurityTokenExpiredException ? "AUTH_TOKEN_EXPIRED" : missingTokenCode;
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
        return context.Response.WriteAsJsonAsync(
            new
            {
                type = $"https://culinaryblog.local/problems/{code}",
                title,
                status,
                code,
                correlationId = context.TraceIdentifier,
                traceId = context.TraceIdentifier,
            },
            cancellationToken: context.RequestAborted);
    }
}
