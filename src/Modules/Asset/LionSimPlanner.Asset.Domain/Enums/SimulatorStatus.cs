namespace LionSimPlanner.Asset.Domain.Enums;

/// <summary>
/// Operational status of a physical Level D Full Flight Simulator bay.
/// Only Ready simulators may have sessions published against them.
/// DOWN triggers SimulatorAOGNotification propagation to the Scheduling module.
/// </summary>
public enum SimulatorStatus
{
    Ready,
    Down,          // AOG — triggers cascade cancellation
    Maintenance,   // Planned maintenance (scheduled, not emergency)
    Standby        // Available but not active
}
