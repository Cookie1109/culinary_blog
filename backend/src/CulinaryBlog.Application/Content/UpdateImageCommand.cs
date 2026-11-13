// Culinary Blog. Authorship is recorded in the repository history.

using CulinaryBlog.Application.Behaviors;
using CulinaryBlog.Application.Content;
using CulinaryBlog.Domain.Recipes;
using FluentValidation;
using MediatR;

namespace CulinaryBlog.Application.Content;

/// <summary>Carries update image command data.</summary>
/// <param name="RecipeId">The identifier of the recipe being accessed.</param>
/// <param name="ImageId">The image id supplied to this operation.</param>
/// <param name="UserId">The identifier of the user performing or owning the operation.</param>
/// <param name="IsAdmin">The is admin supplied to this operation.</param>
/// <param name="ExpectedVersion">The recipe version used to reject concurrent changes.</param>
/// <param name="Body">The body supplied to this operation.</param>
public sealed record UpdateImageCommand(Guid RecipeId, Guid ImageId, string UserId, bool IsAdmin, long ExpectedVersion, ImageMetadataRequest Body) : IRequest<ChildMutationDto<RecipeImageDto>>, IRequestWithBody<ImageMetadataRequest>, IContentMutation;
