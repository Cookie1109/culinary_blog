// Culinary Blog. Authorship is recorded in the repository history.

using CulinaryBlog.Application.Behaviors;
using CulinaryBlog.Application.Content;
using CulinaryBlog.Domain.Recipes;
using FluentValidation;
using MediatR;

namespace CulinaryBlog.Application.Auth;

/// <summary>Carries get current user query data.</summary>
/// <param name="UserId">The identifier of the user performing or owning the operation.</param>
public sealed record GetCurrentUserQuery(string UserId) : IRequest<UserDto>;
