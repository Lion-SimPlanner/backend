namespace LionSimPlanner.Personnel.Domain.Enums;

/// <summary>
/// Instructor authorization level, determining which session types they may conduct.
/// SFI = Synthetic Flight Instructor (sim only)
/// TRI = Type Rating Instructor (initial + recurrent)
/// TRE = Type Rating Examiner (proficiency checks and line checks)
/// </summary>
public enum InstructorRoleLevel
{
    SFI,
    TRI,
    TRE
}
