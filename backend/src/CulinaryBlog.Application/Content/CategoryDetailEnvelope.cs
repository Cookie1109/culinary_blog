// Culinary Blog. Authorship is recorded in the repository history.

using CulinaryBlog.Domain.Recipes;

namespace CulinaryBlog.Application.Content;

/// <summary>Carries category detail envelope data.</summary>
/// <param name="Data">The data supplied to this operation.</param>
/// <param name="Meta">The meta supplied to this operation.</param>
public sealed record CategoryDetailEnvelope(CategoryDetailData Data, PageMeta Meta);
