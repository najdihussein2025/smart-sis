using FluentValidation;
using SmartSis.Api.Features.Attendance.Models.Dtos;

namespace SmartSis.Api.Features.Attendance.Validators;

internal sealed class SubmitAttendanceRequestValidator : AbstractValidator<SubmitAttendanceRequest>
{
    public SubmitAttendanceRequestValidator()
    {
        RuleFor(x => x.MeetingId).GreaterThan(0).WithMessage("Choose the session.");
        RuleFor(x => x.SessionDate).NotEqual(default(DateOnly)).WithMessage("Choose the date.");
        RuleFor(x => x.Records)
            .NotEmpty().WithMessage("There are no students to mark.")
            .Must(r => r.Select(x => x.EnrollmentId).Distinct().Count() == r.Count).WithMessage("A student is marked twice.");
        RuleForEach(x => x.Records).ChildRules(r =>
        {
            r.RuleFor(x => x.EnrollmentId).GreaterThan(0).WithMessage("Choose a student.");
            r.RuleFor(x => x.Status).IsInEnum().WithMessage("Mark the student Present or Absent.");
        });
    }
}
