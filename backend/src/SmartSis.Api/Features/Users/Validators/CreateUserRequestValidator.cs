using FluentValidation;
using SmartSis.Api.Features.Users.Enums;
using SmartSis.Api.Features.Users.Models.Dtos;

namespace SmartSis.Api.Features.Users.Validators;

public sealed class CreateUserRequestValidator : AbstractValidator<CreateUserRequest>
{
    public const string AdminNotAllowed = "Admin accounts can't be created here.";

    public CreateUserRequestValidator(TimeProvider clock)
    {
        RuleFor(x => x.UniversityId)
            .InclusiveBetween(UserValidationRules.MinUniversityId, UserValidationRules.MaxUniversityId)
            .WithMessage("University ID must have 4 to 9 digits.");

        RuleFor(x => x.Email).UserEmail();
        RuleFor(x => x.FullName).UserFullName();
        RuleFor(x => x.Phone).UserPhone();
        RuleFor(x => x.DateOfBirth).InThePast(clock);

        RuleFor(x => x.Role)
            .IsInEnum().WithMessage("Role must be Student or Instructor.")
            .NotEqual(UserRole.Admin).WithMessage(AdminNotAllowed);

        RuleFor(x => x.InitialPassword).Password();

        When(x => x.Role == UserRole.Student, () =>
        {
            RuleFor(x => x.Student).NotNull().WithMessage("Student details are required for a student.");
            RuleFor(x => x.Instructor).Null().WithMessage("Leave instructor details empty for a student.");
        });
        When(x => x.Role == UserRole.Instructor, () =>
        {
            RuleFor(x => x.Instructor).NotNull().WithMessage("Instructor details are required for an instructor.");
            RuleFor(x => x.Student).Null().WithMessage("Leave student details empty for an instructor.");
        });

        RuleFor(x => x.Student!).SetValidator(new StudentInfoRequestValidator()).When(x => x.Student is not null);
        RuleFor(x => x.Instructor!).SetValidator(new InstructorInfoRequestValidator()).When(x => x.Instructor is not null);
    }
}
