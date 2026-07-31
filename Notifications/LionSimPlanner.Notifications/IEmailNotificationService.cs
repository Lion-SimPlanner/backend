namespace LionSimPlanner.Notifications;

/// <summary>
/// Email notification service interface.
/// Implemented by EmailNotificationService using MailKit + Gmail SMTP.
/// This interface is referenced by Scheduling.Application handlers without
/// any coupling to the MailKit implementation details.
/// </summary>
public interface IEmailNotificationService
{
    /// <summary>
    /// Dispatches a structured HTML itinerary email to the assigned pilots' corporate email addresses
    /// when a session transitions from DRAFT to SCHEDULED.
    /// Supports the read-only Pilot role's awareness without giving them any edit access.
    /// </summary>
    Task SendSessionScheduledAsync(
        Guid sessionId,
        DateTime startTime,
        DateTime endTime,
        string simulatorName,
        string syllabusId,
        IReadOnlyList<string> recipientEmails,
        CancellationToken ct = default);

    /// <summary>
    /// Dispatches a cancellation alert email when an AOG event or Admin cancels a session.
    /// Includes the specific cancellation reason so pilots know exactly why.
    /// Emails go to the configured CancellationAlertList, plus any pilot addresses provided.
    /// </summary>
    Task SendSessionCancelledAsync(
        Guid sessionId,
        DateTime originalStartTime,
        DateTime originalEndTime,
        string cancellationReason,
        IReadOnlyList<string>? recipientEmails = null,
        CancellationToken ct = default);
}
