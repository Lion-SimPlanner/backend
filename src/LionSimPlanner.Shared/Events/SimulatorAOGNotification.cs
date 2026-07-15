namespace LionSimPlanner.Shared.Events;

/// <summary>
/// Published by the Asset module when an Engineer marks a simulator as AOG (Aircraft On Ground / Down).
/// The Scheduling module handles this to cascade cancellations across all affected sessions.
/// No Asset module internals are exposed — only the SimulatorId crosses the module boundary.
/// </summary>
public record SimulatorAOGNotification(
    Guid SimulatorId,
    string SimulatorName,
    string ReportedByEngineerCode,
    string FaultDescription,
    DateTime OccurredAt) : MediatR.INotification;
