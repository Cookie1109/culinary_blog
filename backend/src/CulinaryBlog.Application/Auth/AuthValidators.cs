using FluentValidation;

namespace CulinaryBlog.Application.Auth;

public sealed class RegisterRequestValidator : AbstractValidator<RegisterRequest>
{
    public RegisterRequestValidator()
    {
        RuleFor(request => request.DisplayName).NotEmpty().Length(2, 100);
        RuleFor(request => request.Email).NotEmpty().EmailAddress().MaximumLength(256);
        RuleFor(request => request.Password)
            .NotEmpty()
            .MinimumLength(8)
            .MaximumLength(128)
            .Matches("[a-z]").WithMessage("Password must contain a lowercase letter.")
            .Matches("[A-Z]").WithMessage("Password must contain an uppercase letter.")
            .Matches("[0-9]").WithMessage("Password must contain a number.")
            .Matches("[^a-zA-Z0-9]").WithMessage("Password must contain a non-alphanumeric character.");
    }
}

public sealed class LoginRequestValidator : AbstractValidator<LoginRequest>
{
    public LoginRequestValidator()
    {
        RuleFor(request => request.Email).NotEmpty().EmailAddress().MaximumLength(256);
        RuleFor(request => request.Password).NotEmpty().MaximumLength(128);
    }
}

public sealed class ChangePasswordRequestValidator : AbstractValidator<ChangePasswordRequest>
{
    public ChangePasswordRequestValidator()
    {
        RuleFor(request => request.CurrentPassword).NotEmpty().MaximumLength(128);
        RuleFor(request => request.NewPassword).NotEmpty().SetValidator(new NewPasswordValidator());
    }
}

public sealed class ForgotPasswordRequestValidator : AbstractValidator<ForgotPasswordRequest>
{
    public ForgotPasswordRequestValidator()
    {
        RuleFor(request => request.Email).NotEmpty().EmailAddress().MaximumLength(256);
    }
}

public sealed class ResetPasswordRequestValidator : AbstractValidator<ResetPasswordRequest>
{
    public ResetPasswordRequestValidator()
    {
        RuleFor(request => request.Email).NotEmpty().EmailAddress().MaximumLength(256);
        RuleFor(request => request.Token).NotEmpty().MaximumLength(4096);
        RuleFor(request => request.NewPassword).NotEmpty().SetValidator(new NewPasswordValidator());
    }
}

internal sealed class NewPasswordValidator : AbstractValidator<string>
{
    public NewPasswordValidator()
    {
        RuleFor(password => password).NotEmpty().MinimumLength(8).MaximumLength(128)
            .Matches("[a-z]").Matches("[A-Z]").Matches("[0-9]").Matches("[^a-zA-Z0-9]");
    }
}

public sealed class RefreshTokenRequestValidator : AbstractValidator<RefreshTokenRequest>
{
    public RefreshTokenRequestValidator()
    {
        RuleFor(request => request.RefreshToken).NotEmpty().MaximumLength(256);
    }
}

public sealed class GoogleLoginRequestValidator : AbstractValidator<GoogleLoginRequest>
{
    public GoogleLoginRequestValidator()
    {
        RuleFor(request => request.IdToken).NotEmpty().MaximumLength(8192);
    }
}

public sealed class UpdateProfileRequestValidator : AbstractValidator<UpdateProfileRequest>
{
    public UpdateProfileRequestValidator()
    {
        RuleFor(request => request.DisplayName).NotEmpty().Length(2, 100).When(request => request.UpdateDisplayName);
        RuleFor(request => request.AvatarUrl)
            .MaximumLength(500)
            .Must(value => value is null ||
                (Uri.TryCreate(value, UriKind.Absolute, out var uri) &&
                 (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps)))
            .WithMessage("AvatarUrl must be an absolute HTTP or HTTPS URL.")
            .When(request => request.UpdateAvatarUrl);
        RuleFor(request => request.Bio).MaximumLength(2000).When(request => request.UpdateBio);
        RuleFor(request => request)
            .Must(request => request.UpdateDisplayName || request.UpdateAvatarUrl || request.UpdateBio)
            .WithMessage("At least one profile field is required.");
    }
}
