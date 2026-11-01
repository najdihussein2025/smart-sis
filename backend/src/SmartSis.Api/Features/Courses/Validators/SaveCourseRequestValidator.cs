using FluentValidation;
using SmartSis.Api.Features.Courses.Models.Dtos;
using SmartSis.Api.Features.Courses.Rules;

namespace SmartSis.Api.Features.Courses.Validators;

/// <summary>Field rules. Existence of the department, majors and prerequisites is checked in the service.</summary>
public sealed class SaveCourseRequestValidator : AbstractValidator<SaveCourseRequest>
{
    public SaveCourseRequestValidator()
    {
        RuleFor(x => x.Code)
            .Must(CourseCode.IsValid).WithMessage("Use a code like \"CSC 101\": 2–5 letters, 3 digits, optional letter.");

        RuleFor(x => x.Name)
            .Must(n => !string.IsNullOrWhiteSpace(n)).WithMessage("Enter a course name.")
            .Must(n => n is null || n.Trim().Length <= 150).WithMessage("Use at most 150 characters.");

        RuleFor(x => x.Description).MaximumLength(4000).WithMessage("Use at most 4000 characters.");

        // Matches CK_Courses_Credits.
        RuleFor(x => x.Credits).InclusiveBetween(0, 12).WithMessage("Credits must be between 0 and 12.");

        RuleFor(x => x.DepartmentId).GreaterThan(0).WithMessage("Choose a department.");

        RuleFor(x => x.PrerequisiteIds)
            .NotNull().WithMessage("Send the prerequisite list ([] for none).")
            .Must(ids => ids is null || ids.Distinct().Count() == ids.Length).WithMessage("A prerequisite is listed twice.");
        RuleForEach(x => x.PrerequisiteIds).GreaterThan(0).WithMessage("Choose a course.");

        RuleFor(x => x.Majors)
            .NotNull().WithMessage("Send the major list ([] for none).")
            .Must(ms => ms is null || ms.Select(m => m?.MajorId).Distinct().Count() == ms.Length).WithMessage("A major is listed twice.");
        RuleForEach(x => x.Majors).NotNull().ChildRules(m =>
        {
            m.RuleFor(x => x.MajorId).GreaterThan(0).WithMessage("Choose a major.");
            m.RuleFor(x => x.Type).IsInEnum().WithMessage("Type must be Major or Elective.");
        });
    }
}
