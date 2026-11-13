// Culinary Blog. Authorship is recorded in the repository history.

using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text.Json;
using CulinaryBlog.Application.Auth;
using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using ValidationFailure = FluentValidation.Results.ValidationFailure;

namespace CulinaryBlog.Api.Presentation;

/// <summary>Represents the auth endpoints.</summary>
internal static class AuthEndpoints
{
    /// <summary>Maps auth endpoints.</summary>
    /// <param name="endpoints">The endpoints supplied to this operation.</param>
    /// <returns>The map auth endpoints result.</returns>
    public static IEndpointRouteBuilder MapAuthEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var auth = endpoints.MapGroup("/api/v1/auth")
            .WithTags("Auth")
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status429TooManyRequests)
            .RequireRateLimiting("auth")
            .AddEndpointFilter(async (context, next) =>
            {
                context.HttpContext.Response.Headers.CacheControl = "no-store";
                context.HttpContext.Response.Headers.Pragma = "no-cache";
                return await next(context).ConfigureAwait(false);
            });

        auth.MapPost("/register", RegisterAsync).AllowAnonymous()
            .WithSummary("Register an Author account")
            .Produces<DataEnvelope<AuthSessionDto>>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status409Conflict);
        auth.MapPost("/login", LoginAsync).AllowAnonymous()
            .WithSummary("Sign in with email and password")
            .Produces<DataEnvelope<AuthSessionDto>>()
            .ProducesProblem(StatusCodes.Status401Unauthorized);
        auth.MapPost("/google", GoogleLoginAsync).AllowAnonymous()
            .WithSummary("Exchange a Google ID token for a local session")
            .Produces<DataEnvelope<AuthSessionDto>>()
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesProblem(StatusCodes.Status503ServiceUnavailable);
        auth.MapPost("/refresh", RefreshAsync).AllowAnonymous()
            .WithSummary("Rotate the refresh token and issue a new session")
            .Produces<DataEnvelope<AuthSessionDto>>()
            .ProducesProblem(StatusCodes.Status401Unauthorized);
        auth.MapPost("/logout", LogoutAsync).AllowAnonymous()
            .WithSummary("Revoke a refresh token")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden);
        auth.MapPost("/change-password", ChangePasswordAsync).RequireAuthorization("AuthorPolicy")
            .WithSummary("Change the password and revoke all sessions")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status401Unauthorized);
        auth.MapPost("/forgot-password", ForgotPasswordAsync).AllowAnonymous()
            .WithSummary("Request a password reset email without disclosing account existence")
            .Produces(StatusCodes.Status202Accepted)
            .ProducesProblem(StatusCodes.Status503ServiceUnavailable);
        auth.MapPost("/reset-password", ResetPasswordAsync).AllowAnonymous()
            .WithSummary("Reset the password with a single-use token valid for 30 minutes and revoke all sessions")
            .Produces(StatusCodes.Status204NoContent);
        auth.MapGet("/me", GetMeAsync).RequireAuthorization("AuthorPolicy")
            .WithSummary("View the current user profile")
            .Produces<DataEnvelope<UserDto>>()
            .ProducesProblem(StatusCodes.Status401Unauthorized);
        auth.MapPatch("/me", UpdateMeAsync).RequireAuthorization("AuthorPolicy")
            .WithSummary("Update the current user profile")
            .AddOpenApiOperationTransformer(async (operation, context, cancellationToken) =>
            {
                operation.RequestBody!.Content!["application/json"].Schema =
                    await context.GetOrCreateSchemaAsync(typeof(ProfileUpdateBody), cancellationToken: cancellationToken)
                        .ConfigureAwait(false);
            })
            .Produces<DataEnvelope<UserDto>>()
            .ProducesProblem(StatusCodes.Status401Unauthorized);

        return endpoints;
    }

    private static async Task<IResult> ChangePasswordAsync(
        ChangePasswordRequest request,
        ClaimsPrincipal principal,
        ISender sender,
        CancellationToken cancellationToken)
    {
        await sender.Send(new ChangePasswordCommand(GetUserId(principal), request), cancellationToken).ConfigureAwait(false);
        return Results.NoContent();
    }

    private static async Task<IResult> ForgotPasswordAsync(
        ForgotPasswordRequest request,
        ISender sender,
        CancellationToken cancellationToken)
    {
        await sender.Send(new ForgotPasswordCommand(request), cancellationToken).ConfigureAwait(false);
        return Results.Accepted();
    }

    private static async Task<IResult> ResetPasswordAsync(
        ResetPasswordRequest request,
        ISender sender,
        CancellationToken cancellationToken)
    {
        await sender.Send(new ResetPasswordCommand(request), cancellationToken).ConfigureAwait(false);
        return Results.NoContent();
    }

    private static async Task<IResult> RegisterAsync(
        RegisterRequest request,
        ISender sender,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        var session = await sender.Send(new RegisterCommand(request, GetIpAddress(httpContext), GetUserAgent(httpContext)), cancellationToken).ConfigureAwait(false);
        return Results.Created("/api/v1/auth/me", new DataEnvelope<AuthSessionDto>(session));
    }

    private static async Task<IResult> LoginAsync(
        LoginRequest request,
        ISender sender,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        var session = await sender.Send(new LoginCommand(request, GetIpAddress(httpContext), GetUserAgent(httpContext)), cancellationToken).ConfigureAwait(false);
        return Results.Ok(new DataEnvelope<AuthSessionDto>(session));
    }

    private static async Task<IResult> GoogleLoginAsync(
        GoogleLoginRequest request,
        ISender sender,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        var session = await sender.Send(new GoogleLoginCommand(request, GetIpAddress(httpContext), GetUserAgent(httpContext)), cancellationToken).ConfigureAwait(false);
        return Results.Ok(new DataEnvelope<AuthSessionDto>(session));
    }

    private static async Task<IResult> RefreshAsync(
        RefreshTokenRequest request,
        ISender sender,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        var session = await sender.Send(new RefreshSessionCommand(request, GetIpAddress(httpContext), GetUserAgent(httpContext)), cancellationToken).ConfigureAwait(false);
        return Results.Ok(new DataEnvelope<AuthSessionDto>(session));
    }

    private static async Task<IResult> LogoutAsync(
        RefreshTokenRequest request,
        ISender sender,
        HttpContext httpContext,
        IOptionsMonitor<JwtBearerOptions> bearerOptions,
        CancellationToken cancellationToken)
    {
        var authentication = await httpContext.AuthenticateAsync(JwtBearerDefaults.AuthenticationScheme).ConfigureAwait(false);
        string? owner = null;
        if (authentication.Succeeded)
        {
            owner = GetUserId(authentication.Principal);
        }
        else if (httpContext.Request.Headers.ContainsKey("Authorization"))
        {
            // Only a correctly signed, otherwise valid expired JWT may use the expiry exception.
            var header = httpContext.Request.Headers.Authorization.ToString();
            if (!header.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
            {
                throw new AuthProblemException("AUTH_TOKEN_INVALID", "The access token is invalid.");
            }

            var parameters = bearerOptions.Get(JwtBearerDefaults.AuthenticationScheme).TokenValidationParameters.Clone();
            parameters.LifetimeValidator = (notBefore, expires, _, _) =>
                expires.HasValue && expires.Value < DateTime.UtcNow &&
                (!notBefore.HasValue || notBefore.Value <= DateTime.UtcNow);
            try
            {
                new JwtSecurityTokenHandler().ValidateToken(header[7..].Trim(), parameters, out _);
            }
            catch (Exception exception) when (exception is SecurityTokenException or ArgumentException)
            {
                throw new AuthProblemException("AUTH_TOKEN_INVALID", "The access token is invalid.");
            }
        }

        await sender.Send(new LogoutCommand(request, GetIpAddress(httpContext), owner), cancellationToken)
            .ConfigureAwait(false);
        return Results.NoContent();
    }

    private static async Task<IResult> GetMeAsync(
        ClaimsPrincipal principal,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var user = await sender.Send(new GetCurrentUserQuery(GetUserId(principal)), cancellationToken).ConfigureAwait(false);
        return Results.Ok(new DataEnvelope<UserDto>(user));
    }

    private static async Task<IResult> UpdateMeAsync(
        JsonElement body,
        ClaimsPrincipal principal,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var hasDisplayName = body.TryGetProperty("displayName", out var displayNameElement);
        var hasAvatarUrl = body.TryGetProperty("avatarUrl", out var avatarUrlElement);
        var hasBio = body.TryGetProperty("bio", out var bioElement);
        var request = new UpdateProfileRequest(
            hasDisplayName ? ReadNullableString(displayNameElement, "displayName") : null,
            hasAvatarUrl ? ReadNullableString(avatarUrlElement, "avatarUrl") : null,
            hasBio ? ReadNullableString(bioElement, "bio") : null,
            hasDisplayName,
            hasAvatarUrl,
            hasBio);
        var user = await sender.Send(new UpdateProfileCommand(GetUserId(principal), request), cancellationToken)
            .ConfigureAwait(false);
        return Results.Ok(new DataEnvelope<UserDto>(user));
    }

    private static string GetUserId(ClaimsPrincipal principal)
    {
        return principal.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? principal.FindFirstValue("sub")
            ?? throw new AuthProblemException("AUTH_TOKEN_INVALID", "The access token is invalid.");
    }

    private static string? GetIpAddress(HttpContext context) => context.Connection.RemoteIpAddress?.ToString();

    private static string? GetUserAgent(HttpContext context) => context.Request.Headers.UserAgent.ToString();

    private static string? ReadNullableString(JsonElement element, string propertyName)
    {
        if (element.ValueKind == JsonValueKind.Null)
        {
            return null;
        }

        if (element.ValueKind == JsonValueKind.String)
        {
            return element.GetString();
        }

        throw new ValidationException([new ValidationFailure(propertyName, $"{propertyName} must be a string or null.")]);
    }

    private sealed record ProfileUpdateBody(string? DisplayName = null, string? AvatarUrl = null, string? Bio = null);
}
