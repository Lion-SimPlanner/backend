using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MimeKit;

namespace LionSimPlanner.Notifications;

/// <summary>
/// Gmail SMTP email notification service using MailKit + MimeKit.
/// Configured via GmailOptions bound from appsettings.json "Notifications:Gmail" section.
///
/// Authentication: Google App Password (not OAuth — simpler for server-to-server scenarios).
/// Transport: STARTTLS on port 587.
///
/// Email failures are logged but do not propagate exceptions to callers,
/// following the principle that a failed notification must never roll back a
/// successful schedule/grading operation.
/// </summary>
public sealed class EmailNotificationService(
    IOptions<GmailOptions> options,
    ILogger<EmailNotificationService> logger)
    : IEmailNotificationService
{
    private readonly GmailOptions _opts = options.Value;

    // ─────────────────────────────────────────────────────────────────────────
    public async Task SendSessionScheduledAsync(
        Guid sessionId,
        DateTime startTime,
        DateTime endTime,
        string simulatorName,
        string syllabusId,
        IReadOnlyList<string> recipientEmails,
        CancellationToken ct = default)
    {
        if (recipientEmails.Count == 0)
        {
            logger.LogWarning("[Email] SendSessionScheduled: no recipient emails for session {SessionId}.", sessionId);
            return;
        }

        var subject = $"[Lion SimPlanner] Session Scheduled — {startTime:ddd dd MMM yyyy HH:mm} UTC";
        var body    = SessionScheduledTemplate.Build(sessionId, startTime, endTime, simulatorName, syllabusId);

        await SendAsync(subject, body, recipientEmails, ct);
    }

    // ─────────────────────────────────────────────────────────────────────────
    public async Task SendSessionCancelledAsync(
        Guid sessionId,
        DateTime originalStartTime,
        DateTime originalEndTime,
        string cancellationReason,
        IReadOnlyList<string> recipientEmails,
        CancellationToken ct = default)
    {
        if (recipientEmails.Count == 0)
        {
            logger.LogWarning("[Email] SendSessionCancelled: no recipients for session {SessionId}. Skipping.", sessionId);
            return;
        }

        var subject = $"[Lion SimPlanner] ⚠ SESSION CANCELLED — {originalStartTime:ddd dd MMM yyyy HH:mm} UTC";
        var body    = SessionCancelledTemplate.Build(sessionId, originalStartTime, originalEndTime, cancellationReason);

        await SendAsync(subject, body, recipientEmails, ct);
    }

    // ─────────────────────────────────────────────────────────────────────────
    public async Task SendSessionRescheduledAsync(
        Guid sessionId,
        DateTime originalStartTime,
        DateTime originalEndTime,
        DateTime newStartTime,
        DateTime newEndTime,
        string simulatorName,
        string syllabusId,
        IReadOnlyList<string> recipientEmails,
        CancellationToken ct = default)
    {
        if (recipientEmails.Count == 0)
        {
            logger.LogWarning("[Email] SendSessionRescheduled: no recipients for session {SessionId}. Skipping.", sessionId);
            return;
        }

        var subject = $"[Lion SimPlanner] SESSION RESCHEDULED — {newStartTime:ddd dd MMM yyyy HH:mm} UTC";
        var body    = SessionRescheduledTemplate.Build(
            sessionId, originalStartTime, originalEndTime, newStartTime, newEndTime, simulatorName, syllabusId);

        await SendAsync(subject, body, recipientEmails, ct);
    }

    // ─────────────────────────────────────────────────────────────────────────
    private static async Task TryConnectWithFallbackAsync(SmtpClient client, CancellationToken ct)
    {
        var timeout = TimeSpan.FromSeconds(30);
        var attempts = new[]
        {
            ("smtp.gmail.com", 587, SecureSocketOptions.StartTls),
            ("smtp.gmail.com", 465, SecureSocketOptions.SslOnConnect),
        };

        Exception? lastError = null;
        foreach (var (host, port, options) in attempts)
        {
            try
            {
                using var linked = CancellationTokenSource.CreateLinkedTokenSource(ct);
                linked.CancelAfter(timeout);
                await client.ConnectAsync(host, port, options, linked.Token);
                return;
            }
            catch (Exception ex)
            {
                lastError = ex;
            }
        }

        throw lastError ?? new InvalidOperationException("No SMTP connection attempts available.");
    }

    // ─────────────────────────────────────────────────────────────────────────
    private async Task SendAsync(
        string subject,
        string htmlBody,
        IReadOnlyList<string> recipientEmails,
        CancellationToken ct)
    {
        try
        {
            var message = new MimeMessage();
            message.From.Add(new MailboxAddress(_opts.SenderName, _opts.SenderEmail));
            message.Subject = subject;

            foreach (var email in recipientEmails)
                message.To.Add(MailboxAddress.Parse(email));

            message.Body = new BodyBuilder { HtmlBody = htmlBody }.ToMessageBody();

            using var client = new SmtpClient();
            await TryConnectWithFallbackAsync(client, ct);
            await client.AuthenticateAsync(_opts.SenderEmail, _opts.AppPassword, ct);
            await client.SendAsync(message, ct);
            await client.DisconnectAsync(quit: true, ct);

            logger.LogInformation(
                "[Email] Sent '{Subject}' to {Count} recipient(s).",
                subject, recipientEmails.Count);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "[Email] Failed to send '{Subject}' to {Recipients}.",
                subject, string.Join(", ", recipientEmails));
            // Intentionally not re-thrown: email failure must not affect data operations
        }
    }
}

/// <summary>Gmail SMTP configuration bound from appsettings.json "Notifications:Gmail".</summary>
public sealed class GmailOptions
{
    public string SenderEmail  { get; init; } = string.Empty;
    public string SenderName   { get; init; } = "Lion SimPlanner";
    public string AppPassword  { get; init; } = string.Empty;

    /// <summary>Distribution list for AOG cancellation alerts when individual pilot emails are unavailable.</summary>
    public List<string> CancellationAlertList { get; init; } = [];
}
