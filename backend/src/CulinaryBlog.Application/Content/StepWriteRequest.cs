// Culinary Blog. Authorship is recorded in the repository history.

using CulinaryBlog.Domain.Recipes;

namespace CulinaryBlog.Application.Content;

/// <summary>Carries step write request data.</summary>
/// <param name="Title">The title supplied to this operation.</param>
/// <param name="Description">The description supplied to this operation.</param>
/// <param name="TimerMinutes">The timer minutes supplied to this operation.</param>
/// <param name="ImageUrl">The image url supplied to this operation.</param>
/// <param name="StepNumber">The step number supplied to this operation.</param>
public sealed record StepWriteRequest(
    string Title,
    string Description,
    int? TimerMinutes = null,
    string? ImageUrl = null,
    int? StepNumber = null);
