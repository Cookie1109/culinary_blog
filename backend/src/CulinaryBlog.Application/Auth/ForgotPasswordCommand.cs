// Culinary Blog. Authorship is recorded in the repository history.

using CulinaryBlog.Application.Behaviors;
using CulinaryBlog.Application.Content;
using CulinaryBlog.Domain.Recipes;
using FluentValidation;
using MediatR;

namespace CulinaryBlog.Application.Auth;

/// <summary>Carries forgot password command data.</summary>
/// <param name="Body">The body supplied to this operation.</param>
public sealed record ForgotPasswordCommand(ForgotPasswordRequest Body) : IRequest, IRequestWithBody<ForgotPasswordRequest>;
