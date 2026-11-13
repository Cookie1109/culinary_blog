// Culinary Blog. Authorship is recorded in the repository history.

using CulinaryBlog.Domain.Recipes;

namespace CulinaryBlog.Application.Content;

/// <summary>Carries step update request data.</summary>
public sealed record StepUpdateRequest
{
    /// <summary>Gets or initializes the title.</summary>
    public InputField<string?> Title { get; init; }

    /// <summary>Gets or initializes the description.</summary>
    public InputField<string?> Description { get; init; }

    /// <summary>Gets or initializes the timer minutes.</summary>
    public InputField<double?> TimerMinutes { get; init; }

    /// <summary>Gets or initializes the image url.</summary>
    public InputField<string?> ImageUrl { get; init; }

    /// <summary>Gets or initializes the step number.</summary>
    public InputField<int?> StepNumber { get; init; }
}
