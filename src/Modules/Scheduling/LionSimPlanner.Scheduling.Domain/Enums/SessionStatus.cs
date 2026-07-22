namespace LionSimPlanner.Scheduling.Domain.Enums;

/// <summary>
/// Lifecycle states of a SimulatorSession.
/// Transitions are strictly controlled by the Validation Gate and lifecycle handlers.
///
/// Allowed transitions:
///   DRAFT → SCHEDULED  (only after FTL validation passes)
///   SCHEDULED → IN_PROGRESS  (triggered by session start)
///   IN_PROGRESS → COMPLETED  (triggered by grading form submission)
///   SCHEDULED | IN_PROGRESS → CANCELLED  (triggered by AOG event or Admin manual cancel)
/// </summary>
public enum SessionStatus
{
    Draft,
    Scheduled,
    InProgress,
    Completed,
    Cancelled,
    TerminatedEarly
}
