using FluentValidation;
using SmartSis.Api.Features.Support.Models.Dtos;

namespace SmartSis.Api.Features.Support.Validators;

internal static class SupportLimits
{
    public const int Subject = 200;
    public const int Text = 4000;
    public const int CategoryName = 100;
}

internal sealed class CreateSupportRequestRequestValidator : AbstractValidator<CreateSupportRequestRequest>
{
    public CreateSupportRequestRequestValidator()
    {
        RuleFor(x => x.CategoryId).GreaterThan(0).WithMessage("Choose a category.");
        RuleFor(x => x.Subject).Must(s => !string.IsNullOrWhiteSpace(s)).WithMessage("Enter a subject.")
            .MaximumLength(SupportLimits.Subject).WithMessage($"Keep the subject under {SupportLimits.Subject} characters.");
        RuleFor(x => x.Description).Must(s => !string.IsNullOrWhiteSpace(s)).WithMessage("Describe the problem.")
            .MaximumLength(SupportLimits.Text).WithMessage($"Keep the description under {SupportLimits.Text} characters.");
    }
}

internal sealed class SupportReplyRequestValidator : AbstractValidator<SupportReplyRequest>
{
    public SupportReplyRequestValidator()
    {
        RuleFor(x => x.Body).Must(s => !string.IsNullOrWhiteSpace(s)).WithMessage("Write a reply.")
            .MaximumLength(SupportLimits.Text).WithMessage($"Keep the reply under {SupportLimits.Text} characters.");
    }
}

internal sealed class ChangeSupportStatusRequestValidator : AbstractValidator<ChangeSupportStatusRequest>
{
    public ChangeSupportStatusRequestValidator()
    {
        RuleFor(x => x.Status).IsInEnum().WithMessage("Choose a status.");
    }
}

internal sealed class AssignSupportRequestRequestValidator : AbstractValidator<AssignSupportRequestRequest>
{
    public AssignSupportRequestRequestValidator()
    {
        RuleFor(x => x.AdminId).GreaterThan(0).When(x => x.AdminId is not null).WithMessage("Choose an admin.");
    }
}

internal sealed class SaveSupportCategoryRequestValidator : AbstractValidator<SaveSupportCategoryRequest>
{
    public SaveSupportCategoryRequestValidator()
    {
        RuleFor(x => x.Name).Must(s => !string.IsNullOrWhiteSpace(s)).WithMessage("Enter a name.")
            .Must(s => s is null || s.Trim().Length <= SupportLimits.CategoryName)
            .WithMessage($"Keep the name under {SupportLimits.CategoryName} characters.");
    }
}
