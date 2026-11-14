using System.Text.Json;
using System.Text.Json.Serialization;
using SmartSis.Api.Features.Chat.Enums;

namespace SmartSis.Api.Features.Chat.Models;

/// <summary>
/// Mode and course of a conversation (T70). Stored as the conversation's first message with <c>Role = Tool</c>, so no new
/// column (and no migration) is needed. Tool messages are never shown to the student nor sent to the model as history.
/// </summary>
public sealed record ConversationContext(ChatMode Mode, int? CourseId, string? CourseCode, string? CourseName)
{
    public const string Kind = "conversation-context";

    public static readonly ConversationContext Assistant = new(ChatMode.Assistant, null, null, null);

    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web) { Converters = { new JsonStringEnumConverter() } };

    private sealed record Stored(string Kind, ChatMode Mode, int? CourseId, string? CourseCode, string? CourseName);

    public string Serialize() => JsonSerializer.Serialize(new Stored(Kind, Mode, CourseId, CourseCode, CourseName), Json);

    /// <summary>The context in a Tool message, or <see cref="Assistant"/> when there is none or it can't be read.</summary>
    public static ConversationContext Parse(string? content)
    {
        if (string.IsNullOrWhiteSpace(content)) return Assistant;
        try
        {
            var s = JsonSerializer.Deserialize<Stored>(content, Json);
            return s is { Kind: Kind } ? new ConversationContext(s.Mode, s.CourseId, s.CourseCode, s.CourseName) : Assistant;
        }
        catch (JsonException)
        {
            return Assistant;
        }
    }
}
