namespace LionSimPlanner.Shared.Dtos;

/// <summary>
/// Maintenance clearance result DTO crossing the Asset→Scheduling boundary.
/// The Scheduling Validation Gate uses IsCleared to block or allow session publish.
/// </summary>
public record MaintenanceClearanceDto(
    Guid SimulatorId,
    DateOnly SessionDate,
    bool IsCleared,
    string? SignedOffByEngineerCode,
    DateTime? SignedOffAt,
    /// <summary>Human-readable reason when IsCleared is false, for the Validation Gate error message.</summary>
    string? BlockingReason);
