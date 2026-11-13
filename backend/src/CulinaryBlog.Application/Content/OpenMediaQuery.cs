// Culinary Blog. Authorship is recorded in the repository history.

using CulinaryBlog.Application.Abstractions.Caching;
using CulinaryBlog.Application.Auth;
using CulinaryBlog.Application.Behaviors;
using CulinaryBlog.Application.Content;
using CulinaryBlog.Domain.Recipes;
using FluentValidation;
using MediatR;

namespace CulinaryBlog.Application.Content;

/// <summary>Carries open media query data.</summary>
/// <param name="ImageId">The image id supplied to this operation.</param>
/// <param name="Variant">The variant supplied to this operation.</param>
/// <param name="UserId">The identifier of the user performing or owning the operation.</param>
/// <param name="IsAdmin">The is admin supplied to this operation.</param>
public sealed record OpenMediaQuery(Guid ImageId, string Variant, string? UserId, bool IsAdmin) : IRequest<MediaFileDto>;
