using LionSimPlanner.Personnel.Domain.Enums;

namespace LionSimPlanner.Personnel.Domain.Entities;

/// <summary>
/// Represents a flight crew member (Captain or First Officer) in the hr schema.
/// Data originates from the external CMS and is refreshed daily by the CmsSyncJob.
/// Lion SimPlanner is NOT the system of record — never mutate fields synced from CMS directly.
/// </summary>
public class Pilot
{
    public Guid PilotId { get; set; }

    public string EmployeeCode { get; set; } = string.Empty;

    public string FullName { get; set; } = string.Empty;

    public string? CorporateEmail { get; set; }

    public string? CompanyName { get; set; }

    public string? ContactNumber { get; set; }

    public bool IsExternalUser { get; set; } = false;

    public string? FtlStatus { get; set; }

    public PilotRank Rank { get; set; }

    public List<string>? TypeRatings { get; set; }

    public DateTime MedicalExpiry { get; set; }

    public DateTime LastTrainingDate { get; set; }

    public DateTime NextTrainingDue { get; set; }

    public string? RequiredSyllabus { get; set; }

    public DateTime LastDutyEndTime { get; set; }

    public DateTime NextDutyStartTime { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
