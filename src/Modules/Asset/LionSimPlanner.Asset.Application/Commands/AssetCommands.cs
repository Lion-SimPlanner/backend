using MediatR;
using LionSimPlanner.Asset.Domain.Enums;

namespace LionSimPlanner.Asset.Application.Commands;

public record SetSimulatorStatusCommand(
    Guid SimulatorId,
    SimulatorStatus NewStatus,
    Guid EngineerIdRef,
    string EngineerCode,
    string FaultDescription)
    : IRequest<SetSimulatorStatusResult>;

public record SetSimulatorStatusResult(bool Success, string? ErrorMessage);

public record ResolveDefectCommand(
    Guid SimulatorId,
    string ResolutionDescription,
    Guid EngineerIdRef,
    string EngineerCode)
    : IRequest<ResolveDefectResult>;

public record ResolveDefectResult(bool Success, string? ErrorMessage, DateTime? ResolvedAt);

public record CheckoutEngineerCommand(
    Guid EngineerId,
    Guid RequestedByEngineerId,
    string RequestedByEngineerCode)
    : IRequest<CheckoutEngineerResult>;

public record CheckoutEngineerResult(bool Success, string? ErrorMessage, DateTime? CheckoutTime);

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

public record SubmitDefectReportCommand(
    Guid SimulatorId,
    Guid? SessionId,
    string ReportedBy,
    string SystemAffected,
    string Severity,
    string InstructorNotes)
    : IRequest<SubmitDefectReportResult>;

public record SubmitDefectReportResult(bool Success, Guid? DefectId, string? ErrorMessage);

public record ResolveDefectReportCommand(
    Guid DefectId,
    string ResolutionNotes,
    Guid EngineerIdRef,
    string EngineerCode)
    : IRequest<ResolveDefectReportResult>;

public record ResolveDefectReportResult(bool Success, string? ErrorMessage, DateTime? ResolvedAt);
