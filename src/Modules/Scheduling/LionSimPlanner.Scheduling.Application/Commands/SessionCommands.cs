using LionSimPlanner.Scheduling.Domain.Entities;
using LionSimPlanner.Scheduling.Domain.Enums;
using MediatR;

namespace LionSimPlanner.Scheduling.Application.Commands;

// ─────────────────────────────────────────────────────────────────────────────
// All command/query records for the Scheduling module.
// Handlers live in Scheduling.Infrastructure (avoids circular dependency).
// ─────────────────────────────────────────────────────────────────────────────

/// <summary>Creates a new session in DRAFT state. Validation Gate not run yet.</summary>
public record CreateSessionCommand(
    Guid SimulatorId,
    string SessionType,
    DateTime StartTime,
    DateTime EndTime,
    Guid? CaptainId,
    Guid? FirstOfficerId,
    Guid? InstructorId,
    Guid? EngineerId,
    string SyllabusId,
    string TraineeEmployeeCode)
    : IRequest<CreateSessionResult>;

public record CreateSessionResult(bool Success, Guid? SessionId, IReadOnlyList<string> Violations);

/// <summary>
/// Triggers the Validation Gate — runs all FTL and qualification checks then
/// transitions DRAFT → SCHEDULED only if all checks pass.
/// </summary>
public record PublishSessionCommand(Guid SessionId) : IRequest<PublishSessionResult>;

public record PublishSessionResult(bool Success, IReadOnlyList<string> Violations);

public record RescheduleSessionCommand(Guid SessionId, DateTime StartTime, DateTime EndTime)
    : IRequest<RescheduleSessionResult>;

public record RescheduleSessionResult(bool Success, IReadOnlyList<string> Violations);

/// <summary>Admin manually cancels a session.</summary>
public record CancelSessionCommand(Guid SessionId, string Reason) : IRequest;

/// <summary>Marks session as IN_PROGRESS.</summary>
public record StartSessionCommand(Guid SessionId) : IRequest;

/// <summary>
/// Submitted by an Instructor when finalising the digital grading form.
/// Lifecycle step 6: Digital Grading + CMS Sync.
/// </summary>
public record CompleteGradingCommand(
    Guid SessionId,
    string GradeStatus,
    string InstructorNotes,
    string TraineeEmployeeCode)
    : IRequest<CompleteGradingResult>;

public record CompleteGradingResult(bool Success, string? ErrorMessage);

public record TerminateSessionEarlyCommand(
    Guid SessionId,
    DateTime ActualEndTime,
    string Reason)
    : IRequest<TerminateSessionEarlyResult>;

public record TerminateSessionEarlyResult(bool Success, string? ErrorMessage);
