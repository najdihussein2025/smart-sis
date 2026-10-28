using System.Net;

namespace SmartSis.Api.Shared.Email;

/// <summary>Plain HTML templates. Keep them short; every value is HTML-encoded.</summary>
public static class EmailTemplates
{
    public static (string Subject, string Body) PasswordReset(string fullName, string resetUrl) => (
        "Reset your Smart SIS password",
        Wrap($"""
            <p>Hello {E(fullName)},</p>
            <p>We received a request to reset your password. The link below works for 30 minutes.</p>
            <p><a href="{E(resetUrl)}">Reset my password</a></p>
            <p>If you didn't ask for this, you can ignore this email.</p>
            """));

    public static (string Subject, string Body) AbsenceWarning(string fullName, string courseCode, int absences, int limit) => (
        $"Attendance warning: {courseCode}",
        Wrap($"""
            <p>Hello {E(fullName)},</p>
            <p>You have <strong>{absences}</strong> absences in <strong>{E(courseCode)}</strong>. The limit is {limit}.
            One more absence beyond the limit will drop you from the course with an F.</p>
            """));

    private static string E(string s) => WebUtility.HtmlEncode(s);

    private static string Wrap(string inner) =>
        $"""<div style="font-family:Segoe UI,Arial,sans-serif;font-size:14px;color:#1f2937">{inner}<p style="color:#6b7280">Smart SIS · Antonine University</p></div>""";
}
