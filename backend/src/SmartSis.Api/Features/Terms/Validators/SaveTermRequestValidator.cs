using FluentValidation;
using SmartSis.Api.Features.Terms.Models.Dtos;

namespace SmartSis.Api.Features.Terms.Validators;

/// <summary>Mirrors the CK_Terms_* check constraints (so the user gets a 400, not a 500) plus window ordering.</summary>
public sealed class SaveTermRequestValidator : AbstractValidator<SaveTermRequest>
{
    public const string UtcMessage = "Use UTC times (ending in Z).";

    public SaveTermRequestValidator()
    {
        RuleFor(x => x.Name)
            .Must(n => !string.IsNullOrWhiteSpace(n)).WithMessage("Enter a term name.")
            .Must(n => n is null || n.Trim().Length <= 64).WithMessage("Use at most 64 characters.");

        RuleFor(x => x.EndDate)
            .GreaterThan(x => x.StartDate).WithMessage("The term must end after it starts.");

        RuleFor(x => x.EnrollStart).Must(IsUtc).WithMessage(UtcMessage);
        RuleFor(x => x.EnrollEnd).Must(IsUtc).WithMessage(UtcMessage);
        RuleFor(x => x.AddDropStart).Must(IsUtc).WithMessage(UtcMessage);
        RuleFor(x => x.AddDropEnd).Must(IsUtc).WithMessage(UtcMessage);

        RuleFor(x => x.EnrollEnd)
            .GreaterThan(x => x.EnrollStart).WithMessage("The enrollment window must end after it starts.");
        RuleFor(x => x.AddDropEnd)
            .GreaterThan(x => x.AddDropStart).WithMessage("The add/drop window must end after it starts.");

        RuleFor(x => x.AddDropStart)
            .GreaterThanOrEqualTo(x => x.EnrollEnd).WithMessage("Add/drop can't open before the enrollment window closes.");
        RuleFor(x => x.AddDropEnd)
            .Must((r, end) => DateOnly.FromDateTime(end) <= r.EndDate).WithMessage("Add/drop must close by the end of the term.");
    }

    private static bool IsUtc(DateTime d) => d.Kind == DateTimeKind.Utc;
}
