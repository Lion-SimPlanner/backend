using LionSimPlanner.Personnel.Domain.Enums;

namespace LionSimPlanner.Personnel.Domain.Entities;

/// <summary>
/// Represents a qualified Flight Simulator Instructor. Maps to hr.instructors.
/// Monthly hours tracking enforces FTL duty limits specific to instructors.
/// </summary>
public class Instructor
{
    public Guid InstructorId { get; set; }

    /// <summary>Airline employee identifier — primary key for CMS sync.</summary>
    public string EmployeeCode { get; set; } = string.Empty;

    public string FullName { get; set; } = string.Empty;

    /// <summary>Corporate email for schedule notifications.</summary>
    public string CorporateEmail { get; set; } = string.Empty;

    public InstructorRoleLevel RoleLevel { get; set; }

    /// <summary>Aircraft types the instructor is certified to instruct on. JSONB array.</summary>
    public List<string> CertifiedTypes { get; set; } = [];

    /// <summary>
    /// Syllabus types the instructor is authorized to deliver.
    /// Stored as JSONB array of SyllabusType string values.
    /// Validation Gate checks this before allowing session publish.
    /// </summary>
    public List<string> AuthorizedSyllabi { get; set; } = [];

    /// <summary>Instructor license expiry. Expired license blocks assignment.</summary>
    public DateTime LicenseExpiry { get; set; }

    /// <summary>End of last duty period. FTL rest calculation anchor point.</summary>
    public DateTime LastDutyEndTime { get; set; }

    public DateTime NextDutyStartTime { get; set; }

    /// <summary>
    /// Accumulated instructing hours this calendar month.
    /// FTL cap check: CurrentMonthlyHours + session duration must not exceed MaxMonthlyHours.
    /// </summary>
    public int CurrentMonthlyHours { get; set; }

    /// <summary>Maximum instructing hours permitted per calendar month per FTL regulations.</summary>
    public int MaxMonthlyHours { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
