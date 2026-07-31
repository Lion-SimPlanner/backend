namespace LionSimPlanner.Notifications;

public interface IEmailNotificationService
{
    Task SendSessionScheduledAsync(
        Guid sessionId,
        DateTime startTime,
        DateTime endTime,
        string simulatorName,
        string syllabusId,
        IReadOnlyList<string> recipientEmails,
        CancellationToken ct = default);

    Task SendSessionCancelledAsync(
        Guid sessionId,
        DateTime originalStartTime,
        DateTime originalEndTime,
        string cancellationReason,
        IReadOnlyList<string> recipientEmails,
        CancellationToken ct = default);

    Task SendSessionRescheduledAsync(
        Guid sessionId,
        DateTime originalStartTime,
        DateTime originalEndTime,
        DateTime newStartTime,
        DateTime newEndTime,
        string simulatorName,
        string syllabusId,
        IReadOnlyList<string> recipientEmails,
        CancellationToken ct = default);
}
