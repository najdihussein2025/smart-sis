using SmartSis.Api.Features.Chat.Enums;
using SmartSis.Api.Shared.Models;

namespace SmartSis.Api.Features.Chat.Models.Entities;

public class ChatMessage : BaseEntity
{
    public int ConversationId { get; set; }
    public ChatConversation Conversation { get; set; } = null!;
    public ChatRole Role { get; set; }
    public string Content { get; set; } = "";
}
