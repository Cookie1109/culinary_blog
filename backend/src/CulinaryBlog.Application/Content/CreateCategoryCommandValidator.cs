// Culinary Blog. Authorship is recorded in the repository history.

using CulinaryBlog.Application.Abstractions.Caching;
using CulinaryBlog.Application.Behaviors;
using CulinaryBlog.Application.Content;
using CulinaryBlog.Domain.Recipes;
using FluentValidation;
using MediatR;

namespace CulinaryBlog.Application.Content;

/// <summary>Represents the create category command validator.</summary>
/// <param name="validator">The validator supplied to this operation.</param>
internal sealed class CreateCategoryCommandValidator(IValidator<CategoryWriteRequest> validator)
    : RequestBodyValidator<CreateCategoryCommand, CategoryWriteRequest>(validator);
