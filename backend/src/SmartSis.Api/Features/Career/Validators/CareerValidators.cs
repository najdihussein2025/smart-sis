using FluentValidation;
using SmartSis.Api.Features.Career.Models.Dtos;
using SmartSis.Api.Features.Career.Rules;

namespace SmartSis.Api.Features.Career.Validators;

/// <summary>Field rules only. Existence and history checks are in the services.</summary>
public sealed class SaveTraitRequestValidator : AbstractValidator<SaveTraitRequest>
{
    public SaveTraitRequestValidator()
    {
        RuleFor(x => x.Name)
            .Must(n => !string.IsNullOrWhiteSpace(n) && n.Trim().Length >= 2).WithMessage("Enter a name of at least 2 characters.")
            .Must(n => n is null || n.Trim().Length <= 60).WithMessage("Use at most 60 characters.");
        RuleFor(x => x.Description).MaximumLength(300).WithMessage("Use at most 300 characters.");
    }
}

public sealed class SaveQuestionRequestValidator : AbstractValidator<SaveQuestionRequest>
{
    public const int MinOptions = 2;
    public const int MaxOptions = 6;

    public SaveQuestionRequestValidator()
    {
        RuleFor(x => x.Text)
            .Must(t => !string.IsNullOrWhiteSpace(t) && t.Trim().Length >= 5).WithMessage("Enter a question of at least 5 characters.")
            .Must(t => t is null || t.Trim().Length <= 500).WithMessage("Use at most 500 characters.");

        RuleFor(x => x.Options)
            .NotNull().WithMessage($"Add {MinOptions} to {MaxOptions} options.")
            .Must(o => o is null || o.Length is >= MinOptions and <= MaxOptions).WithMessage($"Add {MinOptions} to {MaxOptions} options.")
            .Must(o => o is null || o.Where(x => x?.Id is not null).Select(x => x!.Id).Distinct().Count() == o.Count(x => x?.Id is not null))
            .WithMessage("An option is listed twice.")
            .Must(o => o is null || o.Any(x => x?.Points is { Length: > 0 })).WithMessage("At least one option must add points to a trait.");

        RuleForEach(x => x.Options).NotNull().ChildRules(o =>
        {
            o.RuleFor(x => x.Text)
                .Must(t => !string.IsNullOrWhiteSpace(t)).WithMessage("Enter the option text.")
                .Must(t => t is null || t.Trim().Length <= 300).WithMessage("Use at most 300 characters.");
            o.RuleFor(x => x.Points)
                .NotNull().WithMessage("Send the trait points (an empty list is fine).")
                .Must(p => p is null || p.Select(x => x?.TraitId).Distinct().Count() == p.Length).WithMessage("Each trait can be listed once per option.");
            o.RuleForEach(x => x.Points).NotNull().ChildRules(p =>
            {
                p.RuleFor(x => x.TraitId).GreaterThan(0).WithMessage("Choose a trait.");
                p.RuleFor(x => x.Points).InclusiveBetween(1, 5).WithMessage("Points must be between 1 and 5.");
            });
        });
    }
}

public sealed class SaveCareerPathRequestValidator : AbstractValidator<SaveCareerPathRequest>
{
    public const int MaxSteps = 20;

    public SaveCareerPathRequestValidator()
    {
        RuleFor(x => x.Name)
            .Must(n => !string.IsNullOrWhiteSpace(n) && n.Trim().Length >= 2).WithMessage("Enter a name of at least 2 characters.")
            .Must(n => n is null || n.Trim().Length <= 100).WithMessage("Use at most 100 characters.");
        RuleFor(x => x.Description).MaximumLength(1000).WithMessage("Use at most 1000 characters.");

        RuleFor(x => x.Traits)
            .NotNull().WithMessage("Weigh at least one trait.")
            .Must(t => t is null || t.Length >= 1).WithMessage("Weigh at least one trait.")
            .Must(t => t is null || t.Select(x => x?.TraitId).Distinct().Count() == t.Length).WithMessage("Each trait can be weighed once.");
        RuleForEach(x => x.Traits).NotNull().ChildRules(t =>
        {
            t.RuleFor(x => x.TraitId).GreaterThan(0).WithMessage("Choose a trait.");
            t.RuleFor(x => x.Weight).InclusiveBetween(1, 10).WithMessage("Weight must be between 1 and 10.");
        });

        RuleFor(x => x.Steps)
            .NotNull().WithMessage("Send the roadmap steps (an empty list is fine).")
            .Must(s => s is null || s.Length <= MaxSteps).WithMessage($"Use at most {MaxSteps} steps.");
        RuleForEach(x => x.Steps).NotNull().ChildRules(s =>
        {
            s.RuleFor(x => x.Title)
                .Must(t => !string.IsNullOrWhiteSpace(t) && t.Trim().Length >= 2).WithMessage("Enter a title of at least 2 characters.")
                .Must(t => t is null || t.Trim().Length <= 150).WithMessage("Use at most 150 characters.");
            s.RuleFor(x => x.Description).MaximumLength(1000).WithMessage("Use at most 1000 characters.");
            s.RuleFor(x => x.CourseId).Must(id => id is null || id > 0).WithMessage("Choose a course.");
        });
    }
}

public sealed class SaveCourseTagsRequestValidator : AbstractValidator<SaveCourseTagsRequest>
{
    public SaveCourseTagsRequestValidator()
    {
        RuleFor(x => x.TraitIds)
            .NotNull().WithMessage("Send the trait ids (an empty list removes every tag).")
            .Must(t => t is null || t.Length <= 10).WithMessage("Use at most 10 traits.")
            .Must(t => t is null || t.Distinct().Count() == t.Length).WithMessage("A trait is listed twice.")
            .Must(t => t is null || t.All(id => id > 0)).WithMessage("Choose a trait.");
    }
}

public sealed class SubmitAssessmentRequestValidator : AbstractValidator<SubmitAssessmentRequest>
{
    public SubmitAssessmentRequestValidator()
    {
        RuleFor(x => x.Answers)
            .NotNull().WithMessage("Answer the questions.")
            .Must(a => a is null || a.Length >= 1).WithMessage("Answer the questions.")
            .Must(a => a is null || a.Length <= AssessmentScoring.MaxQuestions).WithMessage($"A quiz has at most {AssessmentScoring.MaxQuestions} questions.");
        RuleForEach(x => x.Answers).NotNull().ChildRules(a =>
        {
            a.RuleFor(x => x.QuestionId).GreaterThan(0).WithMessage("Choose a question.");
            a.RuleFor(x => x.OptionId).GreaterThan(0).WithMessage("Choose an option.");
        });
    }
}
