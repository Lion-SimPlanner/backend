namespace LionSimPlanner.Asset.Domain.Entities;

/// <summary>
/// The "Maintenance Shield" — a daily sign-off record from the Simulator Engineer
/// certifying the simulator hardware is ready for flight training.
///
/// The Scheduling module's Validation Gate queries this (via MediatR) to confirm
/// clearance before publishing a session. No clearance = publish blocked.
/// </summary>
public class MaintenanceChecklist
{
    public Guid ChecklistId { get; set; }

    /// <summary>References maint.simulators.simulator_id — same schema, can use EF navigation.</summary>
    public Guid SimulatorId { get; set; }

    /// <summary>Plain Guid reference to the Engineer. No FK to hr schema.</summary>
    public Guid EngineerIdRef { get; set; }

    /// <summary>Employee code of the signing engineer — duplicated here for the Validation Gate DTO.</summary>
    public string EngineerCode { get; set; } = string.Empty;

    /// <summary>Calendar date this checklist applies to. One per simulator per day.</summary>
    public DateOnly ChecklistDate { get; set; }

    /// <summary>True once the Engineer submits the sign-off. This is the Maintenance Shield flag.</summary>
    public bool IsCleared { get; set; }

    /// <summary>Specific notes from the Engineer, e.g. "Hydraulic system A: checked OK".</summary>
    public string Notes { get; set; } = string.Empty;

    public DateTime? SignedOffAt { get; set; }

    /// <summary>Non-null when the checklist is rejected or the simulator is found unserviceable.</summary>
    public string? BlockingReason { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
