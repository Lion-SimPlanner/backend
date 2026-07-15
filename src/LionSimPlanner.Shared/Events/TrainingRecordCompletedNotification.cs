namespace LionSimPlanner.Shared.Events;

/// <summary>
/// Published by the Scheduling module when an Instructor completes and submits a grading form.
/// The Personnel module handles this to POST the training record to the external CMS.
/// Carries only the data needed for the CMS POST payload — no Scheduling internals leak out.
/// </summary>
public record TrainingRecordCompletedNotification(
    Guid SessionId,
    string EmployeeCode,
    string SyllabusId,
    bool IsGraded,
    string GradeStatus,      // "PASSED" | "FAILED"
    DateTime CompletionDate,
    string InstructorNotes) : MediatR.INotification;
