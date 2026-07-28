namespace LionSimPlanner.Asset.Domain.Entities;

public class SimulatorDefect
{
    public Guid DefectId { get; set; }
    public Guid SimulatorId { get; set; }
    public Guid? SessionId { get; set; }
    public string ReportedBy { get; set; } = string.Empty;
    public string SystemAffected { get; set; } = string.Empty;
    public string Severity { get; set; } = string.Empty;
    public string InstructorNotes { get; set; } = string.Empty;
    public string Status { get; set; } = "Open";
    public string? ResolutionNotes { get; set; }
    public Guid? ResolvedByEngineerId { get; set; }
    public string? ResolvedByEngineerCode { get; set; }
    public DateTime? ResolvedAt { get; set; }
    public DateTime ReportedAt { get; set; } = DateTime.UtcNow;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public Simulator Simulator { get; set; } = null!;
}
