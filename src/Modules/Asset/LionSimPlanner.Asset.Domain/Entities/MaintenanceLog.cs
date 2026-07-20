namespace LionSimPlanner.Asset.Domain.Entities;

public class MaintenanceLog
{
    public Guid MaintenanceLogId { get; set; }
    public Guid SimulatorId { get; set; }
    public string Severity { get; set; } = string.Empty;
    public string FaultDescription { get; set; } = string.Empty;
    public string? ResolutionDescription { get; set; }
    public DateTime? ResolvedAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public Simulator Simulator { get; set; } = null!;
}
