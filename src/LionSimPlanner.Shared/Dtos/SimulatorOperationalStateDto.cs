namespace LionSimPlanner.Shared.Dtos;

/// <summary>
/// Asset-side simulator execution readiness state exposed to Scheduling.
/// </summary>
public record SimulatorOperationalStateDto(
    Guid SimulatorId,
    bool Exists,
    string? Status,
    bool IsOperationalUp);
