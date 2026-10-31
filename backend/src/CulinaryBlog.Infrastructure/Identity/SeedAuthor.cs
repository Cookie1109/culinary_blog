// Culinary Blog. Authorship is recorded in the repository history.

using CulinaryBlog.Domain.Recipes;

namespace CulinaryBlog.Infrastructure.Identity;

/// <summary>Carries seed author data.</summary>
/// <param name="Email">The email supplied to this operation.</param>
/// <param name="DisplayName">The display name supplied to this operation.</param>
/// <param name="Bio">The bio supplied to this operation.</param>
internal sealed record SeedAuthor(string Email, string DisplayName, string Bio);
