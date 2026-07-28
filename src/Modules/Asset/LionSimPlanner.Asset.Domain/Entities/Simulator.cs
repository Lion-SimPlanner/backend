using LionSimPlanner.Asset.Domain.Enums;

namespace LionSimPlanner.Asset.Domain.Entities;
public class Simulator
{
    public Guid SimulatorId { get; set; }
    public string Name { get; set; } = string.Empty;

    public string BayNumber { get; set; } = string.Empty;
    public string AircraftType { get; set; } = string.Empty;

    public SimulatorStatus Status { get; set; } = SimulatorStatus.Ready;

    public Guid? LastStatusChangedByEngineerId { get; set; }

    public string? LastStatusChangedByEngineerCode { get; set; }

    public DateTime LastStatusChangedAt { get; set; } = DateTime.UtcNow;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<MaintenanceLog> MaintenanceLogs { get; set; } = new List<MaintenanceLog>();
    public ICollection<SimulatorDefect> Defects { get; set; } = new List<SimulatorDefect>();
}
