using LionSimPlanner.Shared.Dtos;

namespace LionSimPlanner.Shared.Queries;

/// <summary>
/// Issued by the Scheduling module's Validation Gate before publishing a session.
/// The Asset module resolves this by checking if the Maintenance Shield has been signed off
/// for the given SimulatorId on the session date. Blocks publish if not cleared.
/// </summary>
public record GetMaintenanceClearanceQuery(
    Guid SimulatorId,
    DateOnly SessionDate)
    : MediatR.IRequest<MaintenanceClearanceDto>;
