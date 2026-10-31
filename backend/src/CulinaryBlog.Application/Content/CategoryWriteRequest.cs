// Culinary Blog. Authorship is recorded in the repository history.

using CulinaryBlog.Domain.Recipes;

namespace CulinaryBlog.Application.Content;

/// <summary>Carries category write request data.</summary>
/// <param name="Name">The name supplied to this operation.</param>
/// <param name="Description">The description supplied to this operation.</param>
/// <param name="ImageUrl">The image url supplied to this operation.</param>
/// <param name="OrderIndex">The order index supplied to this operation.</param>
public sealed record CategoryWriteRequest(
    [property: System.ComponentModel.DataAnnotations.StringLength(50, MinimumLength = 2)] string Name,
    string? Description = null,
    string? ImageUrl = null,
    int OrderIndex = 0);
