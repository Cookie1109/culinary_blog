// Culinary Blog. Authorship is recorded in the repository history.

using CulinaryBlog.Application.Behaviors;
using CulinaryBlog.Application.Content;
using CulinaryBlog.Domain.Recipes;
using FluentValidation;
using MediatR;

namespace CulinaryBlog.Application.Content;

/// <summary>Represents the update step command validator.</summary>
/// <param name="validator">The validator supplied to this operation.</param>
internal sealed class UpdateStepCommandValidator(IValidator<StepUpdateRequest> validator)
    : RequestBodyValidator<UpdateStepCommand, StepUpdateRequest>(validator);
