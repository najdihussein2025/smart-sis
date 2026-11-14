using SmartSis.Api.Features.Chat.Enums;
using SmartSis.Api.Features.Chat.Interfaces;
using SmartSis.Api.Features.Chat.Models;
using SmartSis.Api.Features.Chat.Models.Dtos;
using SmartSis.Api.Features.Chat.Models.Entities;
using SmartSis.Api.Shared.Auth;
using SmartSis.Api.Shared.Data;
using SmartSis.Api.Shared.Exceptions;

namespace SmartSis.Api.Features.Chat.Services;

/// <summary>
/// PROJECT.md §6.2 chat flow: the message, recent history and the caller's own JWT go to the Python service, which answers
/// using tools that call this API with that JWT. Both messages are saved only once an answer came back, so a failed call
/// leaves nothing half-saved. Conversations of other students behave as if they didn't exist (404).
/// </summary>
public sealed class ChatService(
    IChatRepository chats, IAiServiceClient ai, IBearerTokenAccessor bearer, ICurrentUser me, IUnitOfWork uow, TimeProvider clock) : IChatService
{
    /// <summary>How many earlier user/assistant messages are sent as history.</summary>
    public const int HistoryLimit = 20;

    private const int TitleLength = 60;

    public async Task<ChatReplyDto> SendAsync(SendChatMessageRequest request, CancellationToken ct)
    {
        var studentId = me.UserId;
        var text = request.Message.Trim();

        ChatConversation? conversation = null;
        ConversationContext context;
        TutorCourse? course = null;
        List<AiTurn> history = [];

        if (request.ConversationId is { } id)
        {
            conversation = await chats.FindOwnAsync(id, studentId, ct) ?? throw NotFound();
            var messages = await chats.MessagesAsync(conversation.Id, ct);
            context = ContextOf(messages);
            history = messages.Where(m => m.Role != ChatRole.Tool).TakeLast(HistoryLimit)
                .Select(m => new AiTurn(m.Role == ChatRole.User ? "user" : "assistant", m.Content)).ToList();
            if (context is { Mode: ChatMode.Tutor, CourseId: { } courseId })
                course = await chats.GetCourseAsync(courseId, ct);
        }
        else if (request.Mode == ChatMode.Tutor)
        {
            course = await chats.GetEnrolledCourseAsync(studentId, request.CourseId ?? 0, ct)
                     ?? throw new BusinessRuleException("Tutoring is available for the courses you're enrolled in. Choose one of them.", "not_enrolled");
            context = new ConversationContext(ChatMode.Tutor, course.Id, course.Code, course.Name);
        }
        else
        {
            context = ConversationContext.Assistant;
        }

        // The caller's own token: the AI service can only see what this student can see.
        var reply = await ai.ChatAsync(new AiChatRequest(text, history, context.Mode, course), bearer.GetToken(), ct);

        if (conversation is null)
        {
            conversation = new ChatConversation { StudentId = studentId, Title = MakeTitle(text, context) };
            chats.Add(conversation);
            if (context.Mode != ChatMode.Assistant)
                chats.Add(new ChatMessage { Conversation = conversation, Role = ChatRole.Tool, Content = context.Serialize() });
        }
        else
        {
            conversation.UpdatedAt = clock.GetUtcNow().UtcDateTime; // moves it to the top of the sidebar
        }

        var userMessage = new ChatMessage { Conversation = conversation, Role = ChatRole.User, Content = text };
        var assistantMessage = new ChatMessage { Conversation = conversation, Role = ChatRole.Assistant, Content = reply.Answer };
        chats.Add(userMessage);
        chats.Add(assistantMessage);
        await uow.SaveChangesAsync(ct);

        return new ChatReplyDto(Summary(conversation, context), userMessage.ToDto(), assistantMessage.ToDto());
    }

    public async Task<IReadOnlyList<ConversationSummaryDto>> ListAsync(CancellationToken ct) =>
        (await chats.ListAsync(me.UserId, ct))
        .Select(r =>
        {
            var c = ConversationContext.Parse(r.ContextJson);
            return new ConversationSummaryDto(r.Id, r.Title, c.Mode, c.CourseId, c.CourseCode, r.CreatedAt, r.UpdatedAt);
        })
        .ToList();

    public async Task<ConversationDto> GetAsync(int id, CancellationToken ct)
    {
        var conversation = await chats.FindOwnAsync(id, me.UserId, ct) ?? throw NotFound();
        var messages = await chats.MessagesAsync(conversation.Id, ct);
        return new ConversationDto(
            Summary(conversation, ContextOf(messages)),
            messages.Where(m => m.Role != ChatRole.Tool).Select(m => m.ToDto()).ToList());
    }

    public async Task<ConversationSummaryDto> RenameAsync(int id, RenameConversationRequest request, CancellationToken ct)
    {
        var conversation = await chats.FindOwnAsync(id, me.UserId, ct) ?? throw NotFound();
        conversation.Title = request.Title.Trim();
        await uow.SaveChangesAsync(ct);
        return Summary(conversation, ContextOf(await chats.MessagesAsync(conversation.Id, ct)));
    }

    public async Task DeleteAsync(int id, CancellationToken ct)
    {
        var conversation = await chats.FindOwnAsync(id, me.UserId, ct) ?? throw NotFound();
        chats.Remove(conversation); // soft delete (DeletedAt)
        await uow.SaveChangesAsync(ct);
    }

    private static NotFoundException NotFound() => new("This conversation was not found.");

    private static ConversationContext ContextOf(IEnumerable<ChatMessage> messages) =>
        ConversationContext.Parse(messages.FirstOrDefault(m => m.Role == ChatRole.Tool)?.Content);

    private static ConversationSummaryDto Summary(ChatConversation c, ConversationContext context) =>
        new(c.Id, c.Title, context.Mode, context.CourseId, context.CourseCode, c.CreatedAt, c.UpdatedAt);

    /// <summary>First line of the message, shortened; tutor conversations start with the course code.</summary>
    internal static string MakeTitle(string message, ConversationContext context)
    {
        var oneLine = string.Join(' ', message.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));
        var title = oneLine.Length <= TitleLength ? oneLine : oneLine[..(TitleLength - 1)].TrimEnd() + "…";
        return context is { Mode: ChatMode.Tutor, CourseCode: { } code } ? $"{code}: {title}" : title;
    }
}

internal static class ChatMessageMapping
{
    public static ChatMessageDto ToDto(this ChatMessage m) => new(m.Id, m.Role, m.Content, m.CreatedAt);
}
