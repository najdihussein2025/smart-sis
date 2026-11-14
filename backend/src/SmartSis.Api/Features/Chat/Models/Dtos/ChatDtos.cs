using SmartSis.Api.Features.Chat.Enums;

namespace SmartSis.Api.Features.Chat.Models.Dtos;

/// <summary>Send a message to the assistant. Leave <c>conversationId</c> empty to start a new conversation.</summary>
/// <param name="Message">The student's message (1–4000 characters).</param>
/// <param name="ConversationId">An existing conversation of the student, or null for a new one.</param>
/// <param name="Mode">Only for a new conversation: "Assistant" (default) or "Tutor".</param>
/// <param name="CourseId">Only for a new Tutor conversation: a course the student is enrolled in.</param>
public sealed record SendChatMessageRequest(string Message, int? ConversationId = null, ChatMode? Mode = null, int? CourseId = null);

/// <summary>Rename a conversation.</summary>
/// <param name="Title">New title (1–200 characters).</param>
public sealed record RenameConversationRequest(string Title);

/// <summary>A conversation in the sidebar.</summary>
/// <param name="Id">Conversation id.</param>
/// <param name="Title">Title (from the first message, or renamed).</param>
/// <param name="Mode">"Assistant" or "Tutor".</param>
/// <param name="CourseId">Tutor conversations: the course being explained.</param>
/// <param name="CourseCode">Tutor conversations: the course code, e.g. "CSC 210".</param>
/// <param name="CreatedAt">Started (UTC).</param>
/// <param name="UpdatedAt">Last message or rename (UTC).</param>
public sealed record ConversationSummaryDto(
    int Id, string Title, ChatMode Mode, int? CourseId, string? CourseCode, DateTime CreatedAt, DateTime UpdatedAt);

/// <summary>A conversation with its messages, oldest first.</summary>
public sealed record ConversationDto(ConversationSummaryDto Conversation, IReadOnlyList<ChatMessageDto> Messages);

/// <summary>One message.</summary>
/// <param name="Id">Message id.</param>
/// <param name="Role">"User" or "Assistant".</param>
/// <param name="Content">Text (the assistant may use simple Markdown).</param>
/// <param name="CreatedAt">Sent (UTC).</param>
public sealed record ChatMessageDto(int Id, ChatRole Role, string Content, DateTime CreatedAt);

/// <summary>The saved question and answer, plus the (possibly new) conversation.</summary>
public sealed record ChatReplyDto(ConversationSummaryDto Conversation, ChatMessageDto UserMessage, ChatMessageDto AssistantMessage);
