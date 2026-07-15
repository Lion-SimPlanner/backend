namespace LionSimPlanner.Scheduling.Domain.Enums;

/// <summary>
/// Classifies what kind of activity is being scheduled in the simulator bay.
/// Determines which validation rules apply: only TRAINING sessions require
/// crew FTL rest checks; MAINTENANCE sessions require only Engineer clearance.
/// </summary>
public enum SessionType
{
    Training,
    Maintenance,
    BriefingOnly,
    CheckRide
}
