using FluentValidation;
using SmartSis.Api.Features.Finance.Models.Dtos;

namespace SmartSis.Api.Features.Finance.Validators;

/// <summary>Shared field rules. Existence of the major / term / payment type / student is checked in the service.</summary>
internal static class FinanceFieldRules
{
    /// <summary>Highest charge or payment accepted. Fits decimal(10,2) with room to spare.</summary>
    public const decimal MaxAmount = 1_000_000m;

    public static bool HasAtMostTwoDecimals(decimal d) => decimal.Round(d, 2) == d;

    public static IRuleBuilderOptions<T, decimal> ValidAmount<T>(this IRuleBuilder<T, decimal> rule) => rule
        .GreaterThan(0).WithMessage("Enter an amount greater than zero.")
        .LessThanOrEqualTo(MaxAmount).WithMessage("Enter an amount of at most 1,000,000.")
        .Must(HasAtMostTwoDecimals).WithMessage("Use at most 2 decimals.");

    public static IRuleBuilderOptions<T, string> ValidTitle<T>(this IRuleBuilder<T, string> rule) => rule
        .Must(t => !string.IsNullOrWhiteSpace(t)).WithMessage("Enter a title.")
        .Must(t => t is null || t.Trim().Length <= 128).WithMessage("Use at most 128 characters.");
}

internal sealed class CreateChargeBatchRequestValidator : AbstractValidator<CreateChargeBatchRequest>
{
    public CreateChargeBatchRequestValidator()
    {
        RuleFor(x => x.MajorId).GreaterThan(0).WithMessage("Choose a major.");
        RuleFor(x => x.TermId).GreaterThan(0).WithMessage("Choose a term.");
        RuleFor(x => x.PaymentTypeId).GreaterThan(0).WithMessage("Choose a payment type.");
        RuleFor(x => x.Title).ValidTitle();
        RuleFor(x => x.Amount).ValidAmount();
        RuleFor(x => x.DueDate).NotEqual(default(DateOnly)).WithMessage("Choose a due date.");
    }
}

internal sealed class CreateChargeRequestValidator : AbstractValidator<CreateChargeRequest>
{
    public CreateChargeRequestValidator()
    {
        RuleFor(x => x.UniversityId).GreaterThan(0).WithMessage("Enter the student's University ID.");
        RuleFor(x => x.TermId).GreaterThan(0).WithMessage("Choose a term.");
        RuleFor(x => x.PaymentTypeId).GreaterThan(0).WithMessage("Choose a payment type.");
        RuleFor(x => x.Title).ValidTitle();
        RuleFor(x => x.Amount).ValidAmount();
        RuleFor(x => x.DueDate).NotEqual(default(DateOnly)).WithMessage("Choose a due date.");
    }
}

internal sealed class RecordPaymentRequestValidator : AbstractValidator<RecordPaymentRequest>
{
    public RecordPaymentRequestValidator()
    {
        RuleFor(x => x.Amount).ValidAmount();
        RuleFor(x => x.Note).MaximumLength(512).WithMessage("Use at most 512 characters.");
    }
}
