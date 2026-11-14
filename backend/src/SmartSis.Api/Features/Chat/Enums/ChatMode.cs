namespace SmartSis.Api.Features.Chat.Enums;

/// <summary>
/// Assistant = personal questions (grades, schedule, deadlines) and policies (PB-15).
/// Tutor = step-by-step concept explanations tied to one enrolled course (PB-19).
/// </summary>
public enum ChatMode
{
    Assistant,
    Tutor,
}
