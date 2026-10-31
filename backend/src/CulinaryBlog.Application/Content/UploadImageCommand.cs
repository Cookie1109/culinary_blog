// Culinary Blog. Authorship is recorded in the repository history.

using CulinaryBlog.Application.Behaviors;
using CulinaryBlog.Application.Content;
using CulinaryBlog.Domain.Recipes;
using FluentValidation;
using MediatR;

namespace CulinaryBlog.Application.Content;

/// <summary>Carries upload image command data.</summary>
/// <param name="RecipeId">The identifier of the recipe being accessed.</param>
/// <param name="UserId">The identifier of the user performing or owning the operation.</param>
/// <param name="IsAdmin">The is admin supplied to this operation.</param>
/// <param name="ExpectedVersion">The recipe version used to reject concurrent changes.</param>
/// <param name="Content">The content supplied to this operation.</param>
/// <param name="Length">The length supplied to this operation.</param>
/// <param name="ContentType">The content type supplied to this operation.</param>
/// <param name="AltText">The alt text supplied to this operation.</param>
/// <param name="IsPrimary">The is primary supplied to this operation.</param>
public sealed record UploadImageCommand(Guid RecipeId, string UserId, bool IsAdmin, long ExpectedVersion, Stream Content, long Length, string ContentType, string? AltText, bool IsPrimary) : IRequest<ChildMutationDto<RecipeImageDto>>, IContentMutation;
