// Culinary Blog. Authorship is recorded in the repository history.

using CulinaryBlog.Domain.Recipes;

namespace CulinaryBlog.Infrastructure.Identity;

/// <summary>Carries seed category data.</summary>
/// <param name="Slug">The slug supplied to this operation.</param>
/// <param name="Name">The name supplied to this operation.</param>
/// <param name="Description">The description supplied to this operation.</param>
internal sealed record SeedCategory(string Slug, string Name, string Description);
