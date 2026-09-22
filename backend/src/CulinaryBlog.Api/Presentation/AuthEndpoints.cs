using System.Security.Claims;
using System.Text.Json;
using CulinaryBlog.Application.Auth;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.RateLimiting;

namespace CulinaryBlog.Api.Presentation;

internal static class AuthEndpoints
{
    public static IEndpointRouteBuilder MapAuthEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var auth = endpoints.MapGroup("/api/v1/auth").RequireRateLimiting("auth");

        auth.MapPost("/register", RegisterAsync).AllowAnonymous();
        auth.MapPost("/login", LoginAsync).AllowAnonymous();
        auth.MapPost("/google", GoogleLoginAsync).AllowAnonymous();
        auth.MapPost("/refresh", RefreshAsync).AllowAnonymous();
        auth.MapPost("/logout", LogoutAsync).AllowAnonymous();
        auth.MapGet("/me", GetMeAsync).RequireAuthorization("AuthorPolicy");
        auth.MapPatch("/me", UpdateMeAsync).RequireAuthorization("AuthorPolicy");

        return endpoints;
    }

    private static async Task<IResult> RegisterAsync(
        RegisterRequest request,
        IValidator<RegisterRequest> validator,
        IAuthService authService,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        await validator.ValidateAndThrowAsync(request, cancellationToken).ConfigureAwait(false);
        var session = await authService.RegisterAsync(
            request,
            GetIpAddress(httpContext),
            GetUserAgent(httpContext),
            cancellationToken).ConfigureAwait(false);
        return Results.Created("/api/v1/auth/me", new DataEnvelope<AuthSessionDto>(session));
    }

    private static async Task<IResult> LoginAsync(
        LoginRequest request,
        IValidator<LoginRequest> validator,
        IAuthService authService,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        await validator.ValidateAndThrowAsync(request, cancellationToken).ConfigureAwait(false);
        var session = await authService.LoginAsync(
            request,
            GetIpAddress(httpContext),
            GetUserAgent(httpContext),
            cancellationToken).ConfigureAwait(false);
        return Results.Ok(new DataEnvelope<AuthSessionDto>(session));
    }

    private static async Task<IResult> GoogleLoginAsync(
        GoogleLoginRequest request,
        IValidator<GoogleLoginRequest> validator,
        IAuthService authService,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        await validator.ValidateAndThrowAsync(request, cancellationToken).ConfigureAwait(false);
        var session = await authService.LoginWithGoogleAsync(
            request.IdToken,
            GetIpAddress(httpContext),
            GetUserAgent(httpContext),
            cancellationToken).ConfigureAwait(false);
        return Results.Ok(new DataEnvelope<AuthSessionDto>(session));
    }

    private static async Task<IResult> RefreshAsync(
        RefreshTokenRequest request,
        IValidator<RefreshTokenRequest> validator,
        IAuthService authService,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        await validator.ValidateAndThrowAsync(request, cancellationToken).ConfigureAwait(false);
        var session = await authService.RefreshAsync(
            request.RefreshToken,
            GetIpAddress(httpContext),
            GetUserAgent(httpContext),
            cancellationToken).ConfigureAwait(false);
        return Results.Ok(new DataEnvelope<AuthSessionDto>(session));
    }

    private static async Task<IResult> LogoutAsync(
        RefreshTokenRequest request,
        IValidator<RefreshTokenRequest> validator,
        IAuthService authService,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        await validator.ValidateAndThrowAsync(request, cancellationToken).ConfigureAwait(false);
        await authService.LogoutAsync(request.RefreshToken, GetIpAddress(httpContext), cancellationToken)
            .ConfigureAwait(false);
        return Results.NoContent();
    }

    private static async Task<IResult> GetMeAsync(
        ClaimsPrincipal principal,
        IAuthService authService,
        CancellationToken cancellationToken)
    {
        var user = await authService.GetCurrentUserAsync(GetUserId(principal), cancellationToken).ConfigureAwait(false);
        return Results.Ok(new DataEnvelope<UserDto>(user));
    }

    private static async Task<IResult> UpdateMeAsync(
        JsonElement body,
        ClaimsPrincipal principal,
        IValidator<UpdateProfileRequest> validator,
        IAuthService authService,
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
        await validator.ValidateAndThrowAsync(request, cancellationToken).ConfigureAwait(false);
        var user = await authService.UpdateCurrentUserAsync(GetUserId(principal), request, cancellationToken)
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
}
