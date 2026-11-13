// Culinary Blog. Authorship is recorded in the repository history.

using CulinaryBlog.Application.Behaviors;
using FluentValidation;

namespace CulinaryBlog.Application.Auth;

/// <summary>Validates the profile fields accompanying an avatar.</summary>
/// <param name="validator">Validates the accompanying profile fields.</param>
internal sealed class UpdateProfileAvatarCommandValidator(IValidator<UpdateProfileRequest> validator)
    : RequestBodyValidator<UpdateProfileAvatarCommand, UpdateProfileRequest>(validator);
