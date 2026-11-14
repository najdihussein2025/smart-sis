using FluentValidation;
using SmartSis.Api.Features.Grading.Models.Dtos;

namespace SmartSis.Api.Features.Grading.Validators;

/// <summary>Shape of a score batch. The max grade and the roster are checked in the service.</summary>
internal sealed class SaveScoresRequestValidator : AbstractValidator<SaveScoresRequest>
{
    public const int MaxEntries = 500;

    public SaveScoresRequestValidator()
    {
        RuleFor(x => x.Scores)
            .NotEmpty().WithMessage("Enter at least one score.")
            .Must(s => s is null || s.Length <= MaxEntries).WithMessage($"Send at most {MaxEntries} scores at a time.")
            .Must(s => s is null || s.Where(e => e is not null).Select(e => e.EnrollmentId).Distinct().Count() == s.Count(e => e is not null))
            .WithMessage("A student is listed twice.");

        RuleForEach(x => x.Scores).NotNull().ChildRules(e =>
        {
            e.RuleFor(x => x.EnrollmentId).GreaterThan(0).WithMessage("Choose a student.");
            e.RuleFor(x => x.Score)
                .GreaterThanOrEqualTo(0).WithMessage("A score can't be negative.")
                .Must(s => s is null || ComponentFieldRules.HasTwoDecimals(s.Value)).WithMessage("Use at most 2 decimals.");
        });
    }
}
