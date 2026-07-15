namespace LionSimPlanner.Asset.Domain.Entities;

/// <summary>
/// Represents a physical Level D Full Flight Simulator bay in the maint schema.
/// This is the asset that the Simulator Engineer manages.
/// SimulatorId is the shared key used by sched.simulator_sessions (as a plain Guid, no FK).
/// </summary>
public class Simulator
{
    public Guid SimulatorId { get; set; }

    /// <summary>Bay designation, e.g. "SIM-01", "SIM-02". Appears in email notifications.</summary>
    public string Name { get; set; } = string.Empty;

    public string BayNumber { get; set; } = string.Empty;

    /// <summary>Aircraft type this simulator replicates, e.g. "B737-MAX".</summary>
    public string AircraftType { get; set; } = string.Empty;

    public string Status { get; set; } = "Ready";   // Stored as string for clarity in DB

    /// <summary>EngineerId of the last Engineer to change status. Denormalized plain Guid — no FK to hr.</summary>
    public Guid? LastStatusChangedByEngineerId { get; set; }

    public string? LastStatusChangedByEngineerCode { get; set; }

    public DateTime LastStatusChangedAt { get; set; } = DateTime.UtcNow;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
