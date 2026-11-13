// Culinary Blog. Authorship is recorded in the repository history.

using CulinaryBlog.Application.Abstractions.Caching;
using CulinaryBlog.Application.Auth;
using CulinaryBlog.Application.Behaviors;
using CulinaryBlog.Application.Content;
using CulinaryBlog.Domain.Recipes;
using FluentValidation;
using MediatR;

namespace CulinaryBlog.Application.Content;

/// <summary>Carries get admin recipe query data.</summary>
/// <param name="Id">The identifier of the requested resource.</param>
public sealed record GetAdminRecipeQuery(Guid Id) : IRequest<RecipeDto>;
