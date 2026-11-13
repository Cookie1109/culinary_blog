// Culinary Blog. Authorship is recorded in the repository history.

using CulinaryBlog.Application.Behaviors;
using CulinaryBlog.Application.Content;
using CulinaryBlog.Domain.Recipes;
using FluentValidation;
using MediatR;

namespace CulinaryBlog.Application.Auth;

/// <summary>Represents the forgot password command validator.</summary>
/// <param name="validator">The validator supplied to this operation.</param>
internal sealed class ForgotPasswordCommandValidator(IValidator<ForgotPasswordRequest> validator)
    : RequestBodyValidator<ForgotPasswordCommand, ForgotPasswordRequest>(validator);
