using FluentValidation;
using SmartSis.Api.Features.Users.Models.Dtos;

namespace SmartSis.Api.Features.Users.Validators;

/// <summary>Field rules shared by the create and update validators.</summary>
internal static class UserValidationRules
{
    public const long MinUniversityId = 1_000;        // 4 digits, like the seeded 1001 / 2001 / 3001
    public const long MaxUniversityId = 999_999_999;  // 9 digits

    public static void UserEmail<T>(this IRuleBuilder<T, string> rule) => rule
        .Must(e => !string.IsNullOrWhiteSpace(e)).WithMessage("Enter an email address.")
        .Must(e => e is null || e.Trim().Length <= 256).WithMessage("Use at most 256 characters.")
        .Must(e => string.IsNullOrWhiteSpace(e) || IsEmail(e.Trim())).WithMessage("Enter a valid email address.");

    public static void UserFullName<T>(this IRuleBuilder<T, string> rule) => rule
        .Must(n => !string.IsNullOrWhiteSpace(n)).WithMessage("Enter the full name.")
        .Must(n => n is null || n.Trim().Length <= 150).WithMessage("Use at most 150 characters.");

    public static void UserPhone<T>(this IRuleBuilder<T, string?> rule) => rule
        .Must(p => p is null || p.Trim().Length <= 32).WithMessage("Use at most 32 characters.");

    public static void InThePast<T>(this IRuleBuilder<T, DateOnly?> rule, TimeProvider clock) => rule
        .Must(d => d is null || d < DateOnly.FromDateTime(clock.GetUtcNow().UtcDateTime))
        .WithMessage("Date of birth must be in the past.");

    /// <summary>Same rule as ResetPasswordRequestValidator.NewPassword in Auth (copied; a test keeps them in sync).</summary>
    public static IRuleBuilderOptions<T, string> Password<T>(this IRuleBuilder<T, string> rule) => rule
        .NotEmpty().WithMessage("Enter an initial password.")
        .MinimumLength(8).WithMessage("Use at least 8 characters.")
        .Matches("[A-Za-z]").WithMessage("Include at least one letter.")
        .Matches("[0-9]").WithMessage("Include at least one number.")
        .MaximumLength(128);

    // Simple shape check: something@something.tld, no spaces.
    private static bool IsEmail(string e) =>
        System.Text.RegularExpressions.Regex.IsMatch(e, @"^[^@\s]+@[^@\s]+\.[^@\s]+$");
}

public sealed class StudentInfoRequestValidator : AbstractValidator<StudentInfoRequest>
{
    public StudentInfoRequestValidator()
    {
        RuleFor(x => x.MajorId).GreaterThan(0).WithMessage("Choose a major.");
        RuleFor(x => x.CampusId).GreaterThan(0).WithMessage("Choose a campus.");
        RuleFor(x => x.EnrollmentDate).NotEqual(default(DateOnly)).WithMessage("Enter the enrollment date.");
        // DB check: MaxCreditsOverride IS NULL OR > 0.
        RuleFor(x => x.MaxCreditsOverride).InclusiveBetween(1, 30).When(x => x.MaxCreditsOverride is not null)
            .WithMessage("Credit cap override must be between 1 and 30.");
    }
}

public sealed class InstructorInfoRequestValidator : AbstractValidator<InstructorInfoRequest>
{
    public InstructorInfoRequestValidator()
    {
        RuleFor(x => x.DepartmentId).GreaterThan(0).WithMessage("Choose a department.");
    }
}
