using SmartSis.Api.Features.Chat.Models.Dtos;

namespace SmartSis.Api.Features.Chat.Interfaces;

/// <summary>The student's assistant conversations (PB-15, PB-19). The student always comes from the token.</summary>
public interface IChatService
{
    Task<ChatReplyDto> SendAsync(SendChatMessageRequest request, CancellationToken ct);
    Task<IReadOnlyList<ConversationSummaryDto>> ListAsync(CancellationToken ct);
    Task<ConversationDto> GetAsync(int id, CancellationToken ct);
    Task<ConversationSummaryDto> RenameAsync(int id, RenameConversationRequest request, CancellationToken ct);
    Task DeleteAsync(int id, CancellationToken ct);
}

/// <summary>Admin knowledge base (policies / FAQs the assistant searches).</summary>
public interface IKnowledgeService
{
    Task<IReadOnlyList<KnowledgeDocumentDto>> ListAsync(CancellationToken ct);
    Task<KnowledgeDocumentDto> UploadAsync(UploadKnowledgeRequest request, CancellationToken ct);
    Task<KnowledgeDocumentDto> ReingestAsync(int id, CancellationToken ct);
    Task DeleteAsync(int id, CancellationToken ct);
}
