// Culinary Blog. Authorship is recorded in the repository history.

using CulinaryBlog.Application.Abstractions.Persistence;
using CulinaryBlog.Application.Content;
using MediatR;

namespace CulinaryBlog.Application.Features.Recipes.Commands;

/// <summary>Carries delete recipe command data.</summary>
/// <param name="Id">The identifier of the requested resource.</param>
/// <param name="UserId">The identifier of the user performing or owning the operation.</param>
/// <param name="IsAdmin">The is admin supplied to this operation.</param>
/// <param name="ExpectedVersion">The recipe version used to reject concurrent changes.</param>
public sealed record DeleteRecipeCommand(Guid Id, string UserId, bool IsAdmin, long ExpectedVersion) : IRequest, IContentMutation;
