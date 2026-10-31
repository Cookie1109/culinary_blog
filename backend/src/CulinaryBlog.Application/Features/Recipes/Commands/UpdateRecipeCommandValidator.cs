// Culinary Blog. Authorship is recorded in the repository history.

using CulinaryBlog.Application.Behaviors;
using CulinaryBlog.Application.Content;
using CulinaryBlog.Domain.Recipes;
using FluentValidation;
using MediatR;

namespace CulinaryBlog.Application.Features.Recipes.Commands;

/// <summary>Represents the update recipe command validator.</summary>
/// <param name="validator">The validator supplied to this operation.</param>
internal sealed class UpdateRecipeCommandValidator(IValidator<RecipeUpdateRequest> validator)
    : RequestBodyValidator<UpdateRecipeCommand, RecipeUpdateRequest>(validator);
