// Culinary Blog. Authorship is recorded in the repository history.

using CulinaryBlog.Application.Behaviors;
using FluentValidation;

namespace CulinaryBlog.Application.Auth;

/// <summary>Validates the profile fields accompanying an avatar.</summary>
internal sealed class UpdateProfileAvatarCommandValidator(IValidator<UpdateProfileRequest> validator)
    : RequestBodyValidator<UpdateProfileAvatarCommand, UpdateProfileRequest>(validator);
