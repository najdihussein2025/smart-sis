using SmartSis.Api.Features.Support.Enums;
using SmartSis.Api.Features.Users.Models.Entities;
using SmartSis.Api.Shared.Exceptions;
using SmartSis.Api.Shared.Models;

namespace SmartSis.Api.Features.Support.Models.Entities;

/// <summary>Submitted by a student, handled by admins (rule C3).</summary>
public class SupportRequest : BaseEntity
{
    public int StudentId { get; set; }
    public Student Student { get; set; } = null!;
    public int CategoryId { get; set; }
    public SupportCategory Category { get; set; } = null!;
    public string Subject { get; set; } = "";
    public string Description { get; set; } = "";
    public SupportStatus Status { get; set; } = SupportStatus.Open;

    /// <summary>Users.Id of the admin handling it.</summary>
    public int? AssignedAdminId { get; set; }
    public DateTime? ResolvedAt { get; set; }

    public ICollection<SupportMessage> Messages { get; set; } = [];

    /// <summary>
    /// Moves the request to <paramref name="status"/>. Resolved / Closed stamp <see cref="ResolvedAt"/> (kept if already set);
    /// Open / In progress clear it. Returns false when the status doesn't change.
    /// </summary>
    public bool ChangeStatus(SupportStatus status, DateTime utcNow)
    {
        if (status == Status) return false;
        Status = status;
        ResolvedAt = status is SupportStatus.Resolved or SupportStatus.Closed ? ResolvedAt ?? utcNow : null;
        return true;
    }

    /// <summary>Adds a reply to the thread. A closed request takes no more replies (409).</summary>
    public SupportMessage AddReply(int authorId, string body)
    {
        if (Status == SupportStatus.Closed)
            throw new BusinessRuleException("This request is closed, so it can't take more replies. Submit a new request if you still need help.", "request_closed");
        var message = new SupportMessage { RequestId = Id, Request = this, AuthorId = authorId, Body = body };
        Messages.Add(message);
        return message;
    }
}
