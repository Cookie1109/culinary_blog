// Culinary Blog. Authorship is recorded in the repository history.

using CulinaryBlog.Application.Behaviors;
using CulinaryBlog.Application.Content;
using CulinaryBlog.Domain.Recipes;
using FluentValidation;
using MediatR;

namespace CulinaryBlog.Application.Auth;

/// <summary>Carries change password command data.</summary>
/// <param name="UserId">The identifier of the user performing or owning the operation.</param>
/// <param name="Body">The body supplied to this operation.</param>
public sealed record ChangePasswordCommand(string UserId, ChangePasswordRequest Body) : IRequest, IRequestWithBody<ChangePasswordRequest>;
