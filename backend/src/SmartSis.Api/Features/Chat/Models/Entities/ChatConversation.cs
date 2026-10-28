using SmartSis.Api.Features.Users.Models.Entities;
using SmartSis.Api.Shared.Models;

namespace SmartSis.Api.Features.Chat.Models.Entities;

public class ChatConversation : BaseEntity, ISoftDeletable
{
    public int StudentId { get; set; }
    public Student Student { get; set; } = null!;
    public string Title { get; set; } = "";
    public DateTime? DeletedAt { get; set; }

    public ICollection<ChatMessage> Messages { get; set; } = [];

    public void MarkDeleted(DateTime utcNow) => DeletedAt = utcNow;
}
