// Culinary Blog. Authorship is recorded in the repository history.

using CulinaryBlog.Application.Abstractions.Caching;
using CulinaryBlog.Application.Behaviors;
using CulinaryBlog.Application.Content;
using CulinaryBlog.Domain.Recipes;
using FluentValidation;
using MediatR;

namespace CulinaryBlog.Application.Content;

/// <summary>Carries create category command data.</summary>
/// <param name="Body">The body supplied to this operation.</param>
/// <param name="IsAdmin">The is admin supplied to this operation.</param>
public sealed record CreateCategoryCommand(CategoryWriteRequest Body, bool IsAdmin = false) : IRequest<CategoryDto>, IRequestWithBody<CategoryWriteRequest>, IContentMutation;
