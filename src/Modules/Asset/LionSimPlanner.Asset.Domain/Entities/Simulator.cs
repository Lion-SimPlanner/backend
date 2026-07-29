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

    public void ApplyDefect(SimulatorDefect defect)
    {
        if (defect.Severity.Equals("AOG", StringComparison.OrdinalIgnoreCase))
            Status = SimulatorStatus.AOG;
        else if (defect.Severity.Equals("MEL", StringComparison.OrdinalIgnoreCase))
            Status = SimulatorStatus.MEL;
        else if (defect.Severity.Equals("Defect", StringComparison.OrdinalIgnoreCase))
            Status = SimulatorStatus.Defect;
    }
}
