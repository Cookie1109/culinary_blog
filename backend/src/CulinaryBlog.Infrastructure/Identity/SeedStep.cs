// Culinary Blog. Authorship is recorded in the repository history.

using CulinaryBlog.Domain.Recipes;

namespace CulinaryBlog.Infrastructure.Identity;

/// <summary>Carries seed step data.</summary>
/// <param name="Title">The title supplied to this operation.</param>
/// <param name="Description">The description supplied to this operation.</param>
/// <param name="TimerMinutes">The timer minutes supplied to this operation.</param>
internal sealed record SeedStep(string Title, string Description, int? TimerMinutes);
