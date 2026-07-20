using LionSimPlanner.Shared.Dtos;

namespace LionSimPlanner.Shared.Queries;

/// <summary>
/// Issued by Scheduling at session start to verify simulator execution readiness.
/// </summary>
public record GetSimulatorOperationalStateQuery(Guid SimulatorId)
    : MediatR.IRequest<SimulatorOperationalStateDto>;
