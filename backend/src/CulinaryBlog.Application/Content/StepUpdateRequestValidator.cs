// Culinary Blog. Authorship is recorded in the repository history.

using FluentValidation;

namespace CulinaryBlog.Application.Content;

/// <summary>Represents the step update request validator.</summary>
internal sealed class StepUpdateRequestValidator : AbstractValidator<StepUpdateRequest>
{
    /// <summary>Initializes a new instance of the <see cref="StepUpdateRequestValidator"/> class.</summary>
    public StepUpdateRequestValidator()
    {
        this.RuleFor(request => request.Title.Value).NotEmpty().MaximumLength(200)
            .When(request => request.Title.IsSpecified).OverridePropertyName("Title");
        this.RuleFor(request => request.Description.Value).NotEmpty().MaximumLength(2000)
            .When(request => request.Description.IsSpecified).OverridePropertyName("Description");
        this.RuleFor(request => request.TimerMinutes.Value).InclusiveBetween(0, double.MaxValue)
            .When(request => request.TimerMinutes.IsSpecified).OverridePropertyName("TimerMinutes");
        this.RuleFor(request => request.ImageUrl.Value).MaximumLength(500).Must(value => string.IsNullOrWhiteSpace(value) || (Uri.TryCreate(value, UriKind.Absolute, out var uri) && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps)))
            .When(request => request.ImageUrl.IsSpecified).OverridePropertyName("ImageUrl");
        this.RuleFor(request => request.StepNumber.Value).NotNull().GreaterThan(0)
            .When(request => request.StepNumber.IsSpecified).OverridePropertyName("StepNumber");
    }
}
