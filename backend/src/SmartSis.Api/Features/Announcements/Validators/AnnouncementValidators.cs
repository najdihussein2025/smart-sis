using FluentValidation;
using SmartSis.Api.Features.Announcements.Enums;
using SmartSis.Api.Features.Announcements.Models.Dtos;

namespace SmartSis.Api.Features.Announcements.Validators;

/// <summary>Field rules shared by the admin and instructor forms. Time-dependent checks (expiry in the future) are in the service.</summary>
internal static class AnnouncementFieldRules
{
    public const int TitleMax = 200;
    public const int BodyMax = 4000;
    public const string UtcMessage = "Use UTC times (ending in Z).";

    public static void Title<T>(this IRuleBuilder<T, string> rule) => rule
        .Must(t => !string.IsNullOrWhiteSpace(t)).WithMessage("Enter a title.")
        .Must(t => t is null || t.Trim().Length <= TitleMax).WithMessage($"Use at most {TitleMax} characters.");

    public static void Body<T>(this IRuleBuilder<T, string> rule) => rule
        .Must(b => !string.IsNullOrWhiteSpace(b)).WithMessage("Write the announcement.")
        .Must(b => b is null || b.Trim().Length <= BodyMax).WithMessage($"Use at most {BodyMax} characters.");

    public static bool IsUtc(DateTime? d) => d is null || d.Value.Kind == DateTimeKind.Utc;
}

public sealed class SaveAnnouncementRequestValidator : AbstractValidator<SaveAnnouncementRequest>
{
    public SaveAnnouncementRequestValidator()
    {
        RuleFor(x => x.Title).Title();
        RuleFor(x => x.Body).Body();
        RuleFor(x => x.Scope).IsInEnum().WithMessage("Choose who should see it.");
        // Mirrors CK_Announcements_MajorScope / CK_Announcements_SectionScope.
        RuleFor(x => x.MajorId).NotNull().WithMessage("Choose a major.").When(x => x.Scope == AnnouncementScope.Major);
        RuleFor(x => x.MajorId).Null().WithMessage("A major is only used with the Major audience.").When(x => x.Scope != AnnouncementScope.Major);
        RuleFor(x => x.SectionId).NotNull().WithMessage("Choose a section.").When(x => x.Scope == AnnouncementScope.Section);
        RuleFor(x => x.SectionId)
            .Null().When(x => x.Scope != AnnouncementScope.Section).WithMessage("A section is only used with the Section audience.");
        RuleFor(x => x.PublishAt).Must(AnnouncementFieldRules.IsUtc).WithMessage(AnnouncementFieldRules.UtcMessage);
        RuleFor(x => x.ExpiresAt).Must(AnnouncementFieldRules.IsUtc).WithMessage(AnnouncementFieldRules.UtcMessage);
        RuleFor(x => x.ExpiresAt)
            .Must((r, e) => e is null || r.PublishAt is null || e > r.PublishAt).WithMessage("The expiry must be after the publish time.");
    }
}

public sealed class SaveSectionAnnouncementRequestValidator : AbstractValidator<SaveSectionAnnouncementRequest>
{
    public SaveSectionAnnouncementRequestValidator()
    {
        RuleFor(x => x.Title).Title();
        RuleFor(x => x.Body).Body();
        RuleFor(x => x.SectionId).GreaterThan(0).WithMessage("Choose a section.");
        RuleFor(x => x.PublishAt).Must(AnnouncementFieldRules.IsUtc).WithMessage(AnnouncementFieldRules.UtcMessage);
        RuleFor(x => x.ExpiresAt).Must(AnnouncementFieldRules.IsUtc).WithMessage(AnnouncementFieldRules.UtcMessage);
        RuleFor(x => x.ExpiresAt)
            .Must((r, e) => e is null || r.PublishAt is null || e > r.PublishAt).WithMessage("The expiry must be after the publish time.");
    }
}
