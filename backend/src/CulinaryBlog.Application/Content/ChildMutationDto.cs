// Culinary Blog. Authorship is recorded in the repository history.

using CulinaryBlog.Domain.Recipes;

namespace CulinaryBlog.Application.Content;

/// <summary>Carries child mutation dto data.</summary>
/// <param name="Resource">The resource supplied to this operation.</param>
/// <param name="RecipeVersion">The recipe version supplied to this operation.</param>
/// <typeparam name="T">The  type.</typeparam>
public sealed record ChildMutationDto<T>(T Resource, long RecipeVersion);
