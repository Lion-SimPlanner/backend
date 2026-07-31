namespace LionSimPlanner.Shared.Dtos;

/// <summary>
/// The only Personnel data the Scheduling module ever sees.
/// Projected from hr.Pilot by the Personnel module's GetPriorityQueueHandler.
/// </summary>
public record PilotPriorityDto(
    Guid PilotId,
    string EmployeeCode,
    string FullName,
    string Rank,
    bool IsExternalUser,
    DateTime NextTrainingDue,
    string? RequiredSyllabus,
    IReadOnlyList<string> TypeRatings,
    DateTime MedicalExpiry,
    DateTime LastDutyEndTime,
    DateTime NextDutyStartTime,
    string? CorporateEmail = null);
