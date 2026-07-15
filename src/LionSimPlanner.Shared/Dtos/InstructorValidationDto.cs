namespace LionSimPlanner.Shared.Dtos;

/// <summary>
/// Instructor data shape required by the FTL Validation Service.
/// Lives in Shared so it can cross the Scheduling ↔ Personnel boundary
/// without either module directly referencing the other's project.
/// </summary>
public sealed record InstructorValidationData(
    Guid InstructorId,
    string EmployeeCode,
    string FullName,
    IReadOnlyList<string> CertifiedTypes,
    IReadOnlyList<string> AuthorizedSyllabi,
    DateTime LicenseExpiry,
    DateTime LastDutyEndTime,
    int CurrentMonthlyHours,
    int MaxMonthlyHours);
