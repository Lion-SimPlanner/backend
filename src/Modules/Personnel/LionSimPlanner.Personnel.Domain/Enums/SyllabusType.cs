namespace LionSimPlanner.Personnel.Domain.Enums;

/// <summary>
/// Training syllabus types. Determines required instructor authorization level
/// and the training record format posted to the external CMS.
/// </summary>
public enum SyllabusType
{
    InitialTypeRating,
    RecurrentTraining,
    LineCheck,
    ProficiencyCheck,
    RouteCheck,
    EmergencyAndAbnormal,
    LowVisibilityOperations
}
