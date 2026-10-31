// Culinary Blog. Authorship is recorded in the repository history.

using CulinaryBlog.Domain.Recipes;

namespace CulinaryBlog.Application.Content;

/// <summary>Carries category detail data data.</summary>
/// <param name="Category">The category supplied to this operation.</param>
/// <param name="Recipes">The recipes supplied to this operation.</param>
public sealed record CategoryDetailData(CategoryDto Category, IReadOnlyCollection<RecipeDto> Recipes);
