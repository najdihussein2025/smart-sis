namespace SmartSis.Api.Features.Notifications.Interfaces;

/// <summary>
/// Queue an email in EmailOutbox (rule C2). Does not save: the caller's unit of work commits it
/// together with the change that caused it. EmailOutboxJob sends it.
/// </summary>
public interface IEmailQueue
{
    void Enqueue(string toEmail, string subject, string htmlBody);
}
