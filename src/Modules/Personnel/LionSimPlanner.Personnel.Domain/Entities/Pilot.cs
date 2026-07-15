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

    /// <summary>Airline employee identifier — matches the external CMS primary key for sync.</summary>
    public string EmployeeCode { get; set; } = string.Empty;

    public string FullName { get; set; } = string.Empty;

    /// <summary>Corporate email used to send schedule notifications (read-only Pilot role).</summary>
    public string CorporateEmail { get; set; } = string.Empty;

    public PilotRank Rank { get; set; }

    /// <summary>Aircraft type ratings held. Stored as PostgreSQL JSONB array.</summary>
    public List<string> TypeRatings { get; set; } = [];

    /// <summary>Medical certificate expiry. Violations here block scheduling via the Validation Gate.</summary>
    public DateTime MedicalExpiry { get; set; }

    public DateTime LastTrainingDate { get; set; }

    /// <summary>
    /// Calculated by Lion SimPlanner (LastTrainingDate + TrainingSync:NextTrainingDueDays config).
    /// Drives priority queue ranking — pilots closest to expiry appear first.
    /// </summary>
    public DateTime NextTrainingDue { get; set; }

    /// <summary>Syllabus the pilot is due to complete next. Used to filter eligible instructors.</summary>
    public string RequiredSyllabus { get; set; } = string.Empty;

    /// <summary>
    /// Timestamp of last duty end. FTL rest validation compares this against session StartTime.
    /// A minimum 10-hour gap is required before the next duty period.
    /// </summary>
    public DateTime LastDutyEndTime { get; set; }

    public DateTime NextDutyStartTime { get; set; }

    /// <summary>Populated by CMS sync; tracks which records have been updated.</summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
