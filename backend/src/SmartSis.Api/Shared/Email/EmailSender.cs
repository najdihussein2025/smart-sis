using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Options;

namespace SmartSis.Api.Shared.Email;

public sealed class EmailSettings
{
    public const string Section = "Email";

    public string Provider { get; set; } = "Log";
    public string From { get; set; } = "no-reply@smartsis.local";
    public SmtpSettings Smtp { get; set; } = new();

    public sealed class SmtpSettings
    {
        public string Host { get; set; } = "";
        public int Port { get; set; } = 587;
        public string User { get; set; } = "";
        public string Password { get; set; } = "";
        public bool EnableSsl { get; set; } = true;
    }
}

/// <summary>
/// Sends one email. Only the EmailOutbox job calls this. Features queue emails with
/// IEmailQueue (Features/Notifications) instead of sending inline (rule C2).
/// </summary>
public interface IEmailSender
{
    Task SendAsync(string to, string subject, string htmlBody, CancellationToken ct = default);
}

/// <summary>Development sender: writes the email to the log so reset links are visible in the console.</summary>
public sealed class LogEmailSender(ILogger<LogEmailSender> logger) : IEmailSender
{
    public Task SendAsync(string to, string subject, string htmlBody, CancellationToken ct = default)
    {
        logger.LogInformation("[EMAIL] To: {To} | Subject: {Subject}\n{Body}", to, subject, htmlBody);
        return Task.CompletedTask;
    }
}

public sealed class SmtpEmailSender(IOptions<EmailSettings> options) : IEmailSender
{
    private readonly EmailSettings _s = options.Value;

    public async Task SendAsync(string to, string subject, string htmlBody, CancellationToken ct = default)
    {
        using var client = new SmtpClient(_s.Smtp.Host, _s.Smtp.Port)
        {
            EnableSsl = _s.Smtp.EnableSsl,
            Credentials = string.IsNullOrEmpty(_s.Smtp.User) ? null : new NetworkCredential(_s.Smtp.User, _s.Smtp.Password),
        };
        using var message = new MailMessage(_s.From, to, subject, htmlBody) { IsBodyHtml = true };
        await client.SendMailAsync(message, ct);
    }
}
