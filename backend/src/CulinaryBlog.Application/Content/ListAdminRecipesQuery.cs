// Culinary Blog. Authorship is recorded in the repository history.

using CulinaryBlog.Application.Abstractions.Caching;
using CulinaryBlog.Application.Auth;
using CulinaryBlog.Application.Behaviors;
using CulinaryBlog.Application.Content;
using CulinaryBlog.Domain.Recipes;
using FluentValidation;
using MediatR;

namespace CulinaryBlog.Application.Content;

/// <summary>Carries list admin recipes query data.</summary>
/// <param name="Status">The status supplied to this operation.</param>
/// <param name="AuthorId">The identifier of the user performing or owning the operation.</param>
/// <param name="Search">The search supplied to this operation.</param>
/// <param name="Page">The page supplied to this operation.</param>
/// <param name="PageSize">The page size supplied to this operation.</param>
public sealed record ListAdminRecipesQuery(RecipeStatus? Status, string? AuthorId, string? Search, int Page, int PageSize) : IRequest<PageEnvelope<RecipeDto>>;
