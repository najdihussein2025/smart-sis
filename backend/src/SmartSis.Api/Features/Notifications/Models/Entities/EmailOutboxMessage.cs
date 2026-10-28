using SmartSis.Api.Features.Notifications.Enums;
using SmartSis.Api.Shared.Models;

namespace SmartSis.Api.Features.Notifications.Models.Entities;

/// <summary>Row in EmailOutbox. Emails are queued here and sent by EmailOutboxJob (rule C2).</summary>
public class EmailOutboxMessage : BaseEntity
{
    public const int MaxAttempts = 5;

    public string ToEmail { get; set; } = "";
    public string Subject { get; set; } = "";
    public string Body { get; set; } = "";
    public EmailStatus Status { get; set; } = EmailStatus.Queued;
    public int Attempts { get; set; }
    public DateTime? SentAt { get; set; }
    public string? LastError { get; set; }

    public void MarkSent(DateTime utcNow)
    {
        Attempts++;
        Status = EmailStatus.Sent;
        SentAt = utcNow;
        LastError = null;
    }

    public void MarkFailedAttempt(string error)
    {
        Attempts++;
        LastError = error.Length > 1000 ? error[..1000] : error;
        Status = Attempts >= MaxAttempts ? EmailStatus.Failed : EmailStatus.Queued;
    }
}
