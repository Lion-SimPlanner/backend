using MediatR;

namespace LionSimPlanner.Asset.Application.Commands;

// ─────────────────────────────────────────────────────────────────────────────
// All command/query records for the Asset module.
// Handlers live in Asset.Infrastructure (avoids circular dependency).
// ─────────────────────────────────────────────────────────────────────────────

/// <summary>
/// Changes a simulator's operational status. Triggers SimulatorAOGNotification
/// via MediatR if new status is "Down".
/// </summary>
public record SetSimulatorStatusCommand(
    Guid SimulatorId,
    string NewStatus,
    Guid EngineerIdRef,
    string EngineerCode,
    string FaultDescription)
    : IRequest<SetSimulatorStatusResult>;

public record SetSimulatorStatusResult(bool Success, string? ErrorMessage);

/// <summary>
/// Submits the Engineer's daily readiness checklist (Maintenance Shield sign-off).
/// IsCleared = true raises the shield, allowing sessions to be published.
/// </summary>
public record SubmitMaintenanceChecklistCommand(
    Guid SimulatorId,
    Guid EngineerIdRef,
    string EngineerCode,
    DateOnly ChecklistDate,
    bool IsCleared,
    string Notes,
    string? BlockingReason)
    : IRequest<SubmitChecklistResult>;

public record SubmitChecklistResult(bool Success, Guid ChecklistId, string? ErrorMessage);
