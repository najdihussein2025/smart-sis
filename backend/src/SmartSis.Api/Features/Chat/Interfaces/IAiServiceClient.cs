using SmartSis.Api.Features.Chat.Enums;

namespace SmartSis.Api.Features.Chat.Interfaces;

/// <summary>One earlier turn sent to the AI service as history.</summary>
public sealed record AiTurn(string Role, string Content);

/// <summary>What the .NET API sends to the Python <c>/chat</c> endpoint (besides the student's own JWT).</summary>
public sealed record AiChatRequest(string Message, IReadOnlyList<AiTurn> History, ChatMode Mode, TutorCourse? Course);

public sealed record AiChatResult(string Answer, IReadOnlyList<string> ToolsUsed);

/// <summary>
/// The Python AI service (PROJECT.md §6.2). Calls carry the shared <c>X-Service-Key</c>; chat calls also carry the
/// <b>calling student's own JWT</b>, so every tool call the model makes is limited to that student's data.
/// Failures throw <c>BusinessRuleException</c> with code <c>assistant_unavailable</c> and a message for people.
/// </summary>
public interface IAiServiceClient
{
    Task<AiChatResult> ChatAsync(AiChatRequest request, string studentToken, CancellationToken ct);

    /// <summary>Chunks, embeds and stores the document's text in KnowledgeChunks (replacing old chunks). Returns the chunk count.</summary>
    Task<int> IngestAsync(int documentId, string title, string content, CancellationToken ct);
}

/// <summary>Where uploaded knowledge-base files live (their text), so a document can be re-ingested.</summary>
public interface IKnowledgeFileStore
{
    /// <summary>Saves the text and returns the key stored in <c>KnowledgeDocuments.FileUrl</c>.</summary>
    Task<string> SaveAsync(string content, string extension, CancellationToken ct);

    /// <summary>The text, or null when the file is gone.</summary>
    Task<string?> ReadAsync(string key, CancellationToken ct);

    void Delete(string key);
}

/// <summary>The caller's own bearer token, forwarded to the AI service. Never logged.</summary>
public interface IBearerTokenAccessor
{
    string GetToken();
}
