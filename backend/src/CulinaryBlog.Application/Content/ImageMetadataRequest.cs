// Culinary Blog. Authorship is recorded in the repository history.

using CulinaryBlog.Domain.Recipes;

namespace CulinaryBlog.Application.Content;

/// <summary>Carries image metadata request data.</summary>
/// <param name="AltText">The alt text supplied to this operation.</param>
/// <param name="IsPrimary">The is primary supplied to this operation.</param>
/// <param name="OrderIndex">The order index supplied to this operation.</param>
public sealed record ImageMetadataRequest(string? AltText, bool? IsPrimary, int? OrderIndex);
