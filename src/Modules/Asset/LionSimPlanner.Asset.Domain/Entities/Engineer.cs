namespace LionSimPlanner.Asset.Domain.Entities;

/// <summary>
/// Represents a Simulator Engineer responsible for hardware maintenance and
/// the daily readiness sign-off ("Maintenance Shield").
/// Maps to maint.engineers.
/// </summary>
public class Engineer
{
    public Guid EngineerID { get; set; }

    public string EmployeeCode { get; set; } = string.Empty;

    public string FullName { get; set; } = string.Empty;

    /// <summary>Regulatory clearance level. Determines which maintenance activities the engineer can approve.</summary>
    public string ClearanceLevel { get; set; } = string.Empty;

    /// <summary>Simulator types/hardware families the engineer is rated for. JSONB array.</summary>
    public List<string> HardwareRatings { get; set; } = [];

    public DateTime ShiftStartTime { get; set; }
    public DateTime ShiftEndTime   { get; set; }

    public bool IsOnCall { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
