using FluentValidation;
using SmartSis.Api.Features.Sections.Models.Dtos;

namespace SmartSis.Api.Features.Sections.Validators;

/// <summary>Field rules (mirrors CK_Sections_* / CK_SectionMeetings_*). Existence and clashes are checked in the service.</summary>
public sealed class SaveSectionRequestValidator : AbstractValidator<SaveSectionRequest>
{
    public const int MaxMeetings = 10;

    public SaveSectionRequestValidator()
    {
        RuleFor(x => x.CourseId).GreaterThan(0).WithMessage("Choose a course.");
        RuleFor(x => x.TermId).GreaterThan(0).WithMessage("Choose a term.");

        RuleFor(x => x.SectionCode)
            .Must(c => !string.IsNullOrWhiteSpace(c)).WithMessage("Enter a section code.")
            .Must(c => c is null || c.Trim().Length <= 8).WithMessage("Use at most 8 characters.")
            .Must(c => c is null || c.Trim().All(char.IsAsciiLetterOrDigit)).WithMessage("Use letters and digits only, e.g. \"A\" or \"B2\".");

        RuleFor(x => x.InstructorId)
            .Must(id => id is null || id > 0).WithMessage("Choose an instructor.");

        RuleFor(x => x.Capacity).InclusiveBetween(1, 500).WithMessage("Capacity must be between 1 and 500.");

        RuleFor(x => x.Meetings)
            .NotNull().WithMessage("Add at least one meeting.")
            .Must(ms => ms is null || ms.Length >= 1).WithMessage("Add at least one meeting.")
            .Must(ms => ms is null || ms.Length <= MaxMeetings).WithMessage($"Use at most {MaxMeetings} meetings.")
            .Must(ms => ms is null || ms.Where(m => m?.Id is not null).Select(m => m!.Id).Distinct().Count()
                                       == ms.Count(m => m?.Id is not null))
            .WithMessage("A meeting is listed twice.");

        RuleForEach(x => x.Meetings).NotNull().ChildRules(m =>
        {
            m.RuleFor(x => x.DayOfWeek).InclusiveBetween(1, 6).WithMessage("Choose a day from Monday to Saturday.");
            m.RuleFor(x => x.EndTime).GreaterThan(x => x.StartTime).WithMessage("The meeting must end after it starts.");
            m.RuleFor(x => x.RoomId).GreaterThan(0).WithMessage("Choose a room.");
            m.RuleFor(x => x.Type).IsInEnum().WithMessage("Type must be Lecture or Lab.");
        });
    }
}
