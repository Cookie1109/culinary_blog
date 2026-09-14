using FluentValidation;

namespace CulinaryBlog.Application.Content;

internal sealed class CategoryWriteRequestValidator : AbstractValidator<CategoryWriteRequest>
{
    public CategoryWriteRequestValidator()
    {
        RuleFor(request => request.Name).NotEmpty().MinimumLength(2).MaximumLength(100);
        RuleFor(request => request.Description).MaximumLength(2000);
        RuleFor(request => request.ImageUrl)
            .MaximumLength(500)
            .Must(BeSafeUrl)
            .When(request => !string.IsNullOrWhiteSpace(request.ImageUrl))
            .WithMessage("imageUrl must be an absolute HTTP or HTTPS URL.");
        RuleFor(request => request.OrderIndex).GreaterThanOrEqualTo(0);
    }

    private static bool BeSafeUrl(string? value) =>
        Uri.TryCreate(value, UriKind.Absolute, out var uri) &&
        (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);
}

internal sealed class RecipeWriteRequestValidator : AbstractValidator<RecipeWriteRequest>
{
    public RecipeWriteRequestValidator()
    {
        RuleFor(request => request.Title).NotEmpty().MinimumLength(5).MaximumLength(200);
        RuleFor(request => request.Description).NotEmpty().MaximumLength(2000);
        RuleFor(request => request.CategoryId).NotEmpty();
        RuleFor(request => request.PrepTime).GreaterThan(0);
        RuleFor(request => request.CookTime).GreaterThanOrEqualTo(0);
        RuleFor(request => request.Servings).GreaterThan(0);
        RuleFor(request => request.Difficulty).IsInEnum();
        RuleFor(request => request.Instructions).MaximumLength(10000);
        When(request => request.Nutrition is not null, () =>
        {
            RuleFor(request => request.Nutrition!.Calories).GreaterThanOrEqualTo(0).When(request => request.Nutrition!.Calories.HasValue);
            RuleFor(request => request.Nutrition!.Protein).GreaterThanOrEqualTo(0).When(request => request.Nutrition!.Protein.HasValue);
            RuleFor(request => request.Nutrition!.Carbohydrates).GreaterThanOrEqualTo(0).When(request => request.Nutrition!.Carbohydrates.HasValue);
            RuleFor(request => request.Nutrition!.Fat).GreaterThanOrEqualTo(0).When(request => request.Nutrition!.Fat.HasValue);
            RuleFor(request => request.Nutrition!.Fiber).GreaterThanOrEqualTo(0).When(request => request.Nutrition!.Fiber.HasValue);
            RuleFor(request => request.Nutrition!.Sodium).GreaterThanOrEqualTo(0).When(request => request.Nutrition!.Sodium.HasValue);
        });
    }
}

internal sealed class IngredientWriteRequestValidator : AbstractValidator<IngredientWriteRequest>
{
    public IngredientWriteRequestValidator()
    {
        RuleFor(request => request.Name).NotEmpty().MaximumLength(200);
        RuleFor(request => request.Quantity).GreaterThan(0).When(request => request.Quantity.HasValue);
        RuleFor(request => request.Unit).MaximumLength(50);
        RuleFor(request => request.Notes).MaximumLength(500);
        RuleFor(request => request.OrderIndex).GreaterThanOrEqualTo(0);
    }
}

internal sealed class StepWriteRequestValidator : AbstractValidator<StepWriteRequest>
{
    public StepWriteRequestValidator()
    {
        RuleFor(request => request.Title).NotEmpty().MaximumLength(200);
        RuleFor(request => request.Description).NotEmpty().MaximumLength(2000);
        RuleFor(request => request.TimerMinutes).GreaterThanOrEqualTo(0).When(request => request.TimerMinutes.HasValue);
        RuleFor(request => request.ImageUrl).MaximumLength(500).Must(BeSafeUrl)
            .When(request => !string.IsNullOrWhiteSpace(request.ImageUrl));
        RuleFor(request => request.StepNumber).GreaterThan(0).When(request => request.StepNumber.HasValue);
    }

    private static bool BeSafeUrl(string? value) =>
        Uri.TryCreate(value, UriKind.Absolute, out var uri) &&
        (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);
}

internal sealed class ImageMetadataRequestValidator : AbstractValidator<ImageMetadataRequest>
{
    public ImageMetadataRequestValidator()
    {
        RuleFor(request => request.AltText).MaximumLength(200);
        RuleFor(request => request.OrderIndex).GreaterThanOrEqualTo(0).When(request => request.OrderIndex.HasValue);
    }
}
