using FluentValidation;
using SmartSis.Api.Features.Grading.Models.Dtos;

namespace SmartSis.Api.Features.Grading.Validators;

/// <summary>Field rules shared by create and update (mirror CK_GradeComponents_*).</summary>
internal static class ComponentFieldRules
{
    public const decimal MaxGradeLimit = 1000m;

    public static void Name<T>(this AbstractValidator<T> v, System.Linq.Expressions.Expression<Func<T, string>> name) =>
        v.RuleFor(name)
            .Must(n => !string.IsNullOrWhiteSpace(n)).WithMessage("Enter a name, e.g. \"Midterm\".")
            .Must(n => n is null || n.Trim().Length <= 64).WithMessage("Use at most 64 characters.");

    public static void MaxGrade<T>(this AbstractValidator<T> v, System.Linq.Expressions.Expression<Func<T, decimal>> max) =>
        v.RuleFor(max)
            .GreaterThan(0).WithMessage("Max grade must be more than 0.")
            .LessThanOrEqualTo(MaxGradeLimit).WithMessage($"Max grade can be at most {MaxGradeLimit:0}.")
            .Must(HasTwoDecimals).WithMessage("Use at most 2 decimals.");

    public static void Weight<T>(this AbstractValidator<T> v, System.Linq.Expressions.Expression<Func<T, decimal>> weight) =>
        v.RuleFor(weight)
            .GreaterThan(0).WithMessage("Weight must be more than 0%.")
            .LessThanOrEqualTo(100).WithMessage("Weight can be at most 100%.")
            .Must(HasTwoDecimals).WithMessage("Use at most 2 decimals.");

    public static bool HasTwoDecimals(decimal d) => decimal.Round(d, 2) == d;
}

internal sealed class GradeComponentQueryValidator : AbstractValidator<GradeComponentQuery>
{
    public GradeComponentQueryValidator()
    {
        RuleFor(x => x.CourseId).GreaterThan(0).WithMessage("Choose a course.");
        RuleFor(x => x.TermId).GreaterThan(0).WithMessage("Choose a term.");
    }
}

internal sealed class CreateGradeComponentRequestValidator : AbstractValidator<CreateGradeComponentRequest>
{
    public CreateGradeComponentRequestValidator()
    {
        RuleFor(x => x.CourseId).GreaterThan(0).WithMessage("Choose a course.");
        RuleFor(x => x.TermId).GreaterThan(0).WithMessage("Choose a term.");
        this.Name(x => x.Name);
        this.MaxGrade(x => x.MaxGrade);
        this.Weight(x => x.Weight);
    }
}

internal sealed class UpdateGradeComponentRequestValidator : AbstractValidator<UpdateGradeComponentRequest>
{
    public UpdateGradeComponentRequestValidator()
    {
        this.Name(x => x.Name);
        this.MaxGrade(x => x.MaxGrade);
        this.Weight(x => x.Weight);
    }
}
