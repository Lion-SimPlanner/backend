using System.Text.Json.Serialization;

namespace LionSimPlanner.Personnel.Infrastructure.CmsSync;

// ─────────────────────────────────────────────────────────────────────────────
// These models represent the exact JSON payload shape of the external CMS REST
// API as specified. They are private to the Personnel module's Infrastructure
// layer — nothing else in the system ever sees these shapes.
// ─────────────────────────────────────────────────────────────────────────────

/// <summary>CMS GET /api/v1/cms/roster/pilots array element.</summary>
public sealed class CmsPilotRecord
{
    [JsonPropertyName("employee_code")]
    public string EmployeeCode { get; init; } = string.Empty;

    [JsonPropertyName("full_name")]
    public string FullName { get; init; } = string.Empty;

    [JsonPropertyName("rank")]
    public string Rank { get; init; } = string.Empty;   // "Captain" | "FirstOfficer"

    [JsonPropertyName("type_ratings")]
    public List<string> TypeRatings { get; init; } = [];

    [JsonPropertyName("medical_expiry")]
    public DateTime MedicalExpiry { get; init; }

    [JsonPropertyName("last_duty_end_time")]
    public DateTime LastDutyEndTime { get; init; }

    [JsonPropertyName("next_duty_start_time")]
    public DateTime NextDutyStartTime { get; init; }

    /// <summary>CMS may optionally provide last training date. If absent we retain local value.</summary>
    [JsonPropertyName("last_training_date")]
    public DateTime? LastTrainingDate { get; init; }

    [JsonPropertyName("required_syllabus")]
    public string? RequiredSyllabus { get; init; }

    [JsonPropertyName("corporate_email")]
    public string? CorporateEmail { get; init; }
}

/// <summary>CMS GET /api/v1/cms/roster/instructors array element.</summary>
public sealed class CmsInstructorRecord
{
    [JsonPropertyName("employee_code")]
    public string EmployeeCode { get; init; } = string.Empty;

    [JsonPropertyName("full_name")]
    public string FullName { get; init; } = string.Empty;

    [JsonPropertyName("role_level")]
    public string RoleLevel { get; init; } = string.Empty;   // "SFI" | "TRI" | "TRE"

    [JsonPropertyName("certified_types")]
    public List<string> CertifiedTypes { get; init; } = [];

    [JsonPropertyName("authorized_syllabi")]
    public List<string> AuthorizedSyllabi { get; init; } = [];

    [JsonPropertyName("license_expiry")]
    public DateTime LicenseExpiry { get; init; }

    [JsonPropertyName("last_duty_end_time")]
    public DateTime LastDutyEndTime { get; init; }

    [JsonPropertyName("next_duty_start_time")]
    public DateTime NextDutyStartTime { get; init; }

    [JsonPropertyName("current_monthly_hours")]
    public int CurrentMonthlyHours { get; init; }

    [JsonPropertyName("max_monthly_hours")]
    public int MaxMonthlyHours { get; init; }

    [JsonPropertyName("corporate_email")]
    public string? CorporateEmail { get; init; }
}

/// <summary>
/// Payload sent by Lion SimPlanner to CMS POST /api/v1/cms/training/records.
/// Shape is exactly as specified in the integration contract.
/// </summary>
public sealed class CmsTrainingRecordPayload
{
    [JsonPropertyName("session_id")]
    public string SessionId { get; init; } = string.Empty;

    [JsonPropertyName("employee_code")]
    public string EmployeeCode { get; init; } = string.Empty;

    [JsonPropertyName("syllabus_id")]
    public string SyllabusId { get; init; } = string.Empty;

    [JsonPropertyName("is_graded")]
    public bool IsGraded { get; init; }

    [JsonPropertyName("grade_status")]
    public string GradeStatus { get; init; } = string.Empty;   // "PASSED" | "FAILED"

    [JsonPropertyName("completion_date")]
    public DateTime CompletionDate { get; init; }

    [JsonPropertyName("instructor_notes")]
    public string InstructorNotes { get; init; } = string.Empty;
}
