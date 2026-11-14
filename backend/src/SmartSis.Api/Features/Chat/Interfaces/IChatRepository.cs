using SmartSis.Api.Features.Chat.Models.Dtos;
using SmartSis.Api.Features.Chat.Models.Entities;

namespace SmartSis.Api.Features.Chat.Interfaces;

/// <summary>Course facts handed to the tutor (T69).</summary>
public sealed record TutorCourse(int Id, string Code, string Name, string? Description);

/// <summary>A conversation row plus the content of its context (Tool) message.</summary>
public sealed record ConversationRow(int Id, string Title, string? ContextJson, DateTime CreatedAt, DateTime UpdatedAt);

public interface IChatRepository
{
    Task<List<ConversationRow>> ListAsync(int studentId, CancellationToken ct);

    /// <summary>The student's own conversation (tracked), or null, also when it belongs to someone else.</summary>
    Task<ChatConversation?> FindOwnAsync(int id, int studentId, CancellationToken ct);

    /// <summary>All messages of a conversation, oldest first (Tool messages included).</summary>
    Task<List<ChatMessage>> MessagesAsync(int conversationId, CancellationToken ct);

    /// <summary>The course, only if the student has an <c>Enrolled</c> enrollment in one of its sections.</summary>
    Task<TutorCourse?> GetEnrolledCourseAsync(int studentId, int courseId, CancellationToken ct);

    Task<TutorCourse?> GetCourseAsync(int courseId, CancellationToken ct);

    void Add(ChatConversation conversation);
    void Add(ChatMessage message);
    void Remove(ChatConversation conversation);
}

public interface IKnowledgeRepository
{
    Task<List<KnowledgeDocumentDto>> ListAsync(CancellationToken ct);
    Task<KnowledgeDocumentDto?> GetDtoAsync(int id, CancellationToken ct);
    Task<KnowledgeDocument?> FindAsync(int id, CancellationToken ct);
    Task<List<KnowledgeChunk>> ChunksAsync(int documentId, CancellationToken ct);
    void Add(KnowledgeDocument document);
    void Remove(KnowledgeDocument document);
    void RemoveChunks(IEnumerable<KnowledgeChunk> chunks);
}
