// Culinary Blog. Authorship is recorded in the repository history.

using CulinaryBlog.Application.Behaviors;
using CulinaryBlog.Application.Content;
using CulinaryBlog.Domain.Recipes;
using FluentValidation;
using MediatR;

namespace CulinaryBlog.Application.Auth;

/// <summary>Carries login command data.</summary>
/// <param name="Body">The body supplied to this operation.</param>
/// <param name="IpAddress">The ip address supplied to this operation.</param>
/// <param name="UserAgent">The user agent supplied to this operation.</param>
public sealed record LoginCommand(LoginRequest Body, string? IpAddress, string? UserAgent) : IRequest<AuthSessionDto>, IRequestWithBody<LoginRequest>;
