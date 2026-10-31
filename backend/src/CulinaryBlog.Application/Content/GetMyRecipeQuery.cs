// Culinary Blog. Authorship is recorded in the repository history.

using CulinaryBlog.Application.Abstractions.Caching;
using CulinaryBlog.Application.Auth;
using CulinaryBlog.Application.Behaviors;
using CulinaryBlog.Application.Content;
using CulinaryBlog.Domain.Recipes;
using FluentValidation;
using MediatR;

namespace CulinaryBlog.Application.Content;

/// <summary>Carries get my recipe query data.</summary>
/// <param name="Id">The identifier of the requested resource.</param>
/// <param name="UserId">The identifier of the user performing or owning the operation.</param>
public sealed record GetMyRecipeQuery(Guid Id, string UserId) : IRequest<RecipeDto>;
