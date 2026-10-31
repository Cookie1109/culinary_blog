// Culinary Blog. Authorship is recorded in the repository history.

using CulinaryBlog.Application.Abstractions.Caching;
using CulinaryBlog.Application.Behaviors;
using CulinaryBlog.Application.Content;
using CulinaryBlog.Domain.Recipes;
using FluentValidation;
using MediatR;

namespace CulinaryBlog.Application.Content;

/// <summary>Carries delete category command data.</summary>
/// <param name="Id">The identifier of the requested resource.</param>
/// <param name="IsAdmin">The is admin supplied to this operation.</param>
public sealed record DeleteCategoryCommand(Guid Id, bool IsAdmin = false) : IRequest, IContentMutation;
