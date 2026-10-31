// Culinary Blog. Authorship is recorded in the repository history.

using FluentValidation;

namespace CulinaryBlog.Application.Content;

/// <summary>Represents the step write request validator.</summary>
internal sealed class StepWriteRequestValidator : AbstractValidator<StepWriteRequest>
{
    /// <summary>Initializes a new instance of the <see cref="StepWriteRequestValidator"/> class.</summary>
    public StepWriteRequestValidator()
    {
        this.RuleFor(request => request.Title).NotEmpty().MaximumLength(200);
        this.RuleFor(request => request.Description).NotEmpty().MaximumLength(2000);
        this.RuleFor(request => request.TimerMinutes).GreaterThanOrEqualTo(0).When(request => request.TimerMinutes.HasValue);
        this.RuleFor(request => request.ImageUrl).MaximumLength(500).Must(BeSafeUrl)
            .When(request => !string.IsNullOrWhiteSpace(request.ImageUrl));
        this.RuleFor(request => request.StepNumber).GreaterThan(0).When(request => request.StepNumber.HasValue);
    }

    private static bool BeSafeUrl(string? value) =>
        Uri.TryCreate(value, UriKind.Absolute, out var uri) &&
        (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);
}
