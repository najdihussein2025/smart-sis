using FluentValidation;
using SmartSis.Api.Features.Enrollments.Models.Dtos;

namespace SmartSis.Api.Features.Enrollments.Validators;

internal sealed class EnrollRequestValidator : AbstractValidator<EnrollRequest>
{
    public EnrollRequestValidator()
    {
        RuleFor(x => x.SectionIds)
            .NotEmpty().WithMessage("Choose at least one section.")
            .Must(ids => ids.Length <= 10).WithMessage("Enroll in at most 10 sections at a time.")
            .Must(ids => ids.Distinct().Count() == ids.Length).WithMessage("A section is listed twice.");
        RuleForEach(x => x.SectionIds).GreaterThan(0).WithMessage("Choose a section.");
    }
}

internal sealed class SwapRequestValidator : AbstractValidator<SwapRequest>
{
    public SwapRequestValidator()
    {
        RuleFor(x => x.FromSectionId).GreaterThan(0).WithMessage("Choose the section to swap out.");
        RuleFor(x => x.ToSectionId).GreaterThan(0).WithMessage("Choose the section to swap to.");
    }
}

internal sealed class DropRequestValidator : AbstractValidator<DropRequest>
{
    public DropRequestValidator()
    {
        RuleFor(x => x.SectionId).GreaterThan(0).WithMessage("Choose a section.");
    }
}
