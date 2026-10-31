// Culinary Blog. Authorship is recorded in the repository history.

using CulinaryBlog.Domain.Recipes;

namespace CulinaryBlog.Application.Content;

/// <summary>Carries step dto data.</summary>
/// <param name="Id">The identifier of the requested resource.</param>
/// <param name="StepNumber">The step number supplied to this operation.</param>
/// <param name="Title">The title supplied to this operation.</param>
/// <param name="Description">The description supplied to this operation.</param>
/// <param name="TimerMinutes">The timer minutes supplied to this operation.</param>
/// <param name="ImageUrl">The image url supplied to this operation.</param>
public sealed record StepDto(
    Guid Id,
    int StepNumber,
    string Title,
    string Description,
    int? TimerMinutes,
    string? ImageUrl);
