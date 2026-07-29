using LionSimPlanner.Scheduling.Domain.Enums;

namespace LionSimPlanner.Scheduling.Domain.Entities;

/// <summary>
/// Core scheduling entity mapping to sched.simulator_sessions.
///
/// CaptainId, FirstOfficerId, InstructorId, and EngineerId are deliberately nullable
/// Guid references — NOT EF Core navigation properties to hr or maint entities.
/// Cross-schema FK constraints are prohibited by the module isolation rules.
///
/// Status transitions are enforced by application-layer handlers, not DB constraints.
/// </summary>
public class SimulatorSession
{
    public Guid SessionId { get; set; }

    /// <summary>
    /// References the physical simulator bay. Matches Simulator.SimulatorId in the Asset module
    /// but stored as a plain Guid — no FK constraint to maint schema.
    /// </summary>
    public Guid SimulatorId { get; set; }

    public SessionType SessionType { get; set; }

    public SessionStatus Status { get; set; } = SessionStatus.Draft;

    public DateTime StartTime { get; set; }
    public DateTime EndTime   { get; set; }

    /// <summary>Duration in whole hours — used by FTL monthly hours cap check.</summary>
    public double DurationHours => (EndTime - StartTime).TotalHours;

    public void TransitionTo(SessionStatus target)
    {
        var allowed = (Status, target) switch
        {
            (SessionStatus.Draft, SessionStatus.Scheduled) => true,
            (SessionStatus.Draft, SessionStatus.Cancelled) => true,
            (SessionStatus.Scheduled, SessionStatus.InProgress) => true,
            (SessionStatus.Scheduled, SessionStatus.Cancelled) => true,
            (SessionStatus.InProgress, SessionStatus.Completed) => true,
            (SessionStatus.InProgress, SessionStatus.Cancelled) => true,
            (SessionStatus.InProgress, SessionStatus.TerminatedEarly) => true,
            _ => false,
        };
        if (!allowed)
            throw new InvalidOperationException(
                $"Cannot transition session {SessionId} from {Status} to {target}.");
        Status = target;
    }

    /// <summary>
    /// Pilot in command (Captain seat).
    /// Null in DRAFT state; must be set before Validation Gate allows SCHEDULED transition.
    /// Plain Guid — NOT an EF FK to hr.pilots.
    /// </summary>
    public Guid? CaptainId { get; set; }

    /// <summary>Right-seat crew. Same isolation rules as CaptainId.</summary>
    public Guid? FirstOfficerId { get; set; }

    /// <summary>Assigned instructor. Validation Gate confirms type cert and FTL before publish.</summary>
    public Guid? InstructorId { get; set; }

    /// <summary>Responsible engineer for the maintenance shield sign-off verification.</summary>
    public Guid? EngineerId { get; set; }

    /// <summary>Syllabus being trained/examined. Matched against instructor's AuthorizedSyllabi.</summary>
    public string SyllabusId { get; set; } = string.Empty;

    /// <summary>Set to true by CompleteGradingHandler when the Instructor submits a grading form.</summary>
    public bool IsGraded { get; set; }

    /// <summary>
    /// Free-text notes entered by the Instructor on the digital grading form.
    /// Included verbatim in the CMS training record POST payload.
    /// </summary>
    public string InstructorNotes { get; set; } = string.Empty;

    /// <summary>Grading outcome — stored as string to exactly match CMS payload ("PASSED" / "FAILED").</summary>
    public string? GradeStatus { get; set; }

    /// <summary>
    /// Employee code of the pilot whose training record is being completed.
    /// Required for the CMS POST payload; set during grading form submission.
    /// </summary>
    public string TraineeEmployeeCode { get; set; } = string.Empty;

    /// <summary>Reason for cancellation — populated on AOG cascade or Admin manual cancel.</summary>
    public string? CancellationReason { get; set; }
    public DateTime? OriginalEndTime { get; set; }
    public string? TerminationReason { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
