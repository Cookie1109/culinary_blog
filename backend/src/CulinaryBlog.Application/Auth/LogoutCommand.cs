// Culinary Blog. Authorship is recorded in the repository history.

using CulinaryBlog.Application.Behaviors;
using CulinaryBlog.Application.Content;
using CulinaryBlog.Domain.Recipes;
using FluentValidation;
using MediatR;

namespace CulinaryBlog.Application.Auth;

/// <summary>Carries logout command data.</summary>
/// <param name="Body">The body supplied to this operation.</param>
/// <param name="IpAddress">The ip address supplied to this operation.</param>
/// <param name="AuthenticatedUserId">The authenticated user id supplied to this operation.</param>
public sealed record LogoutCommand(RefreshTokenRequest Body, string? IpAddress, string? AuthenticatedUserId = null) : IRequest, IRequestWithBody<RefreshTokenRequest>;
