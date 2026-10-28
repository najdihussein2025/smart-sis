using FluentValidation;
using SmartSis.Api.Features.Auth.Models.Dtos;

namespace SmartSis.Api.Features.Auth.Validators;

public sealed class LoginRequestValidator : AbstractValidator<LoginRequest>
{
    public LoginRequestValidator()
    {
        RuleFor(x => x.UniversityId).GreaterThan(0).WithMessage("Enter your University ID.");
        RuleFor(x => x.Password).NotEmpty().WithMessage("Enter your password.");
    }
}

public sealed class ForgotPasswordRequestValidator : AbstractValidator<ForgotPasswordRequest>
{
    public ForgotPasswordRequestValidator()
    {
        RuleFor(x => x.UniversityId).GreaterThan(0).WithMessage("Enter your University ID.");
    }
}

public sealed class ResetPasswordRequestValidator : AbstractValidator<ResetPasswordRequest>
{
    public ResetPasswordRequestValidator()
    {
        RuleFor(x => x.Token).NotEmpty();
        RuleFor(x => x.NewPassword)
            .MinimumLength(8).WithMessage("Use at least 8 characters.")
            .Matches("[A-Za-z]").WithMessage("Include at least one letter.")
            .Matches("[0-9]").WithMessage("Include at least one number.")
            .MaximumLength(128);
    }
}
