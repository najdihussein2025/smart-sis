using FluentValidation;
using SmartSis.Api.Features.Users.Models.Dtos;

namespace SmartSis.Api.Features.Users.Validators;

/// <summary>Field rules. Whether the profile block matches the user's role is checked in the service (it needs the stored role).</summary>
public sealed class UpdateUserRequestValidator : AbstractValidator<UpdateUserRequest>
{
    public UpdateUserRequestValidator(TimeProvider clock)
    {
        RuleFor(x => x.Email).UserEmail();
        RuleFor(x => x.FullName).UserFullName();
        RuleFor(x => x.Phone).UserPhone();
        RuleFor(x => x.DateOfBirth).InThePast(clock);

        RuleFor(x => x)
            .Must(x => x.Student is null || x.Instructor is null)
            .WithName("instructor").OverridePropertyName("instructor")
            .WithMessage("Send either student or instructor details, not both.");

        RuleFor(x => x.Student!).SetValidator(new StudentInfoRequestValidator()).When(x => x.Student is not null);
        RuleFor(x => x.Instructor!).SetValidator(new InstructorInfoRequestValidator()).When(x => x.Instructor is not null);
    }
}
