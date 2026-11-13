// Culinary Blog. Authorship is recorded in the repository history.

using CulinaryBlog.Application.Abstractions.Caching;
using CulinaryBlog.Application.Auth;
using CulinaryBlog.Application.Behaviors;
using CulinaryBlog.Application.Content;
using CulinaryBlog.Domain.Recipes;
using FluentValidation;
using MediatR;

namespace CulinaryBlog.Application.Content;

/// <summary>Carries list my recipes query data.</summary>
/// <param name="UserId">The identifier of the user performing or owning the operation.</param>
/// <param name="Status">The status supplied to this operation.</param>
/// <param name="Search">The search supplied to this operation.</param>
/// <param name="Page">The page supplied to this operation.</param>
/// <param name="PageSize">The page size supplied to this operation.</param>
public sealed record ListMyRecipesQuery(string UserId, RecipeStatus? Status, string? Search, int Page, int PageSize) : IRequest<PageEnvelope<RecipeDto>>;
