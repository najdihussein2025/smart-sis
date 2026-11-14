using System.Net;
using SmartSis.Api.Features.Notifications.Enums;
using SmartSis.Api.Features.Notifications.Interfaces;
using SmartSis.Api.Features.Support.Enums;
using SmartSis.Api.Features.Support.Interfaces;
using SmartSis.Api.Features.Support.Models.Entities;

namespace SmartSis.Api.Features.Support.Services;

/// <summary>
/// Tells people about support activity: an in-app notification, plus a queued email to the student (rule C2: never
/// sent inline). Nothing is saved here; the caller's unit of work commits it with the change.
/// </summary>
public sealed class SupportNotifier(ISupportRepository repo, INotificationService notifications, IEmailQueue emails, IConfiguration config)
{
    public static string Label(SupportStatus status) => status switch
    {
        SupportStatus.InProgress => "In progress",
        _ => status.ToString(),
    };

    /// <summary>An admin replied: notification + email to the student.</summary>
    public async Task AdminRepliedAsync(SupportRequest request, string body, CancellationToken ct)
    {
        var title = "New reply to your support request";
        notifications.Notify(request.StudentId, NotificationType.Support, title, $"\"{request.Subject}\" has a new reply from support.", StudentLink(request.Id));
        await EmailStudentAsync(request, $"Support reply: {request.Subject}",
            $"<p>Support replied to your request <strong>{E(request.Subject)}</strong>:</p><blockquote>{E(body)}</blockquote>", ct);
    }

    /// <summary>The status changed: notification + email to the student.</summary>
    public async Task StatusChangedAsync(SupportRequest request, CancellationToken ct)
    {
        var label = Label(request.Status);
        notifications.Notify(request.StudentId, NotificationType.Support, $"Support request {label.ToLower()}",
            $"\"{request.Subject}\" is now {label}.", StudentLink(request.Id));
        await EmailStudentAsync(request, $"Support request {label.ToLower()}: {request.Subject}",
            $"<p>Your support request <strong>{E(request.Subject)}</strong> is now <strong>{E(label)}</strong>.</p>", ct);
    }

    /// <summary>The student replied: notification to the assigned admin (if any).</summary>
    public void StudentReplied(SupportRequest request)
    {
        if (request.AssignedAdminId is { } adminId)
            notifications.Notify(adminId, NotificationType.Support, "Student replied to a support request",
                $"\"{request.Subject}\" has a new reply.", AdminLink(request.Id));
    }

    /// <summary>A request was assigned to another admin: notification to them.</summary>
    public void Assigned(SupportRequest request, int adminId) =>
        notifications.Notify(adminId, NotificationType.Support, "Support request assigned to you", $"\"{request.Subject}\"", AdminLink(request.Id));

    private async Task EmailStudentAsync(SupportRequest request, string subject, string inner, CancellationToken ct)
    {
        var contact = await repo.GetContactAsync(request.StudentId, ct);
        if (contact is null) return;
        var baseUrl = (config.GetValue<string>("Frontend:BaseUrl") ?? "http://localhost:5173").TrimEnd('/');
        var html = $"""
            <div style="font-family:Segoe UI,Arial,sans-serif;font-size:14px;color:#1f2937">
            <p>Hello {E(contact.Value.FullName)},</p>
            {inner}
            <p><a href="{E(baseUrl + StudentLink(request.Id))}">Open the request</a></p>
            <p style="color:#6b7280">Smart SIS · Antonine University</p></div>
            """;
        emails.Enqueue(contact.Value.Email, subject, html);
    }

    private static string StudentLink(int id) => $"/student/support/{id}";

    private static string AdminLink(int id) => $"/admin/support/{id}";

    private static string E(string s) => WebUtility.HtmlEncode(s);
}
