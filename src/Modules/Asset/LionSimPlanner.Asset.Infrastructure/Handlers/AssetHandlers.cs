using LionSimPlanner.Asset.Application.Commands;
using LionSimPlanner.Asset.Domain.Entities;
using LionSimPlanner.Shared.Dtos;
using LionSimPlanner.Shared.Events;
using LionSimPlanner.Shared.Queries;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace LionSimPlanner.Asset.Infrastructure.Handlers;

// ─────────────────────────────────────────────────────────────────────────────
// SetSimulatorStatusHandler
// ─────────────────────────────────────────────────────────────────────────────
public sealed class SetSimulatorStatusHandler(
    AssetDbContext db,
    IPublisher publisher,
    ILogger<SetSimulatorStatusHandler> logger)
    : IRequestHandler<SetSimulatorStatusCommand, SetSimulatorStatusResult>
{
    public async Task<SetSimulatorStatusResult> Handle(SetSimulatorStatusCommand req, CancellationToken ct)
    {
        var simulator = await db.Simulators.FirstOrDefaultAsync(s => s.SimulatorId == req.SimulatorId, ct);
        if (simulator is null)
            return new SetSimulatorStatusResult(false, $"Simulator {req.SimulatorId} not found.");

        var prev = simulator.Status;
        simulator.Status                          = req.NewStatus;
        simulator.LastStatusChangedByEngineerId   = req.EngineerIdRef;
        simulator.LastStatusChangedByEngineerCode = req.EngineerCode;
        simulator.LastStatusChangedAt             = DateTime.UtcNow;
        simulator.UpdatedAt                       = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);

        logger.LogInformation("[Asset] Simulator {Id}: {Prev} → {New} by {Code}.",
            req.SimulatorId, prev, req.NewStatus, req.EngineerCode);

        if (string.Equals(req.NewStatus, "Down", StringComparison.OrdinalIgnoreCase))
        {
            logger.LogWarning("[Asset] Simulator {Id} DOWN — publishing AOG notification.", req.SimulatorId);
            await publisher.Publish(new SimulatorAOGNotification(
                req.SimulatorId,
                simulator.Name,
                req.EngineerCode,
                req.FaultDescription,
                DateTime.UtcNow), ct);
        }

        return new SetSimulatorStatusResult(true, null);
    }
}

// ─────────────────────────────────────────────────────────────────────────────
// SubmitMaintenanceChecklistHandler
// ─────────────────────────────────────────────────────────────────────────────
public sealed class SubmitMaintenanceChecklistHandler(
    AssetDbContext db,
    ILogger<SubmitMaintenanceChecklistHandler> logger)
    : IRequestHandler<SubmitMaintenanceChecklistCommand, SubmitChecklistResult>
{
    public async Task<SubmitChecklistResult> Handle(SubmitMaintenanceChecklistCommand req, CancellationToken ct)
    {
        var existing = await db.Checklists.FirstOrDefaultAsync(
            c => c.SimulatorId == req.SimulatorId && c.ChecklistDate == req.ChecklistDate, ct);

        if (existing is not null)
        {
            existing.IsCleared      = req.IsCleared;
            existing.Notes          = req.Notes;
            existing.BlockingReason = req.BlockingReason;
            existing.SignedOffAt    = req.IsCleared ? DateTime.UtcNow : null;
            existing.EngineerIdRef  = req.EngineerIdRef;
            existing.EngineerCode   = req.EngineerCode;
            existing.UpdatedAt      = DateTime.UtcNow;
            await db.SaveChangesAsync(ct);
            logger.LogInformation("[Asset] Checklist updated for Simulator {Id} on {Date}. Cleared={C}",
                req.SimulatorId, req.ChecklistDate, req.IsCleared);
            return new SubmitChecklistResult(true, existing.ChecklistId, null);
        }

        var checklist = new MaintenanceChecklist
        {
            ChecklistId    = Guid.NewGuid(),
            SimulatorId    = req.SimulatorId,
            EngineerIdRef  = req.EngineerIdRef,
            EngineerCode   = req.EngineerCode,
            ChecklistDate  = req.ChecklistDate,
            IsCleared      = req.IsCleared,
            Notes          = req.Notes,
            BlockingReason = req.BlockingReason,
            SignedOffAt    = req.IsCleared ? DateTime.UtcNow : null,
            CreatedAt      = DateTime.UtcNow,
            UpdatedAt      = DateTime.UtcNow
        };
        db.Checklists.Add(checklist);
        await db.SaveChangesAsync(ct);
        logger.LogInformation("[Asset] Checklist submitted for Simulator {Id} on {Date}. Cleared={C}",
            req.SimulatorId, req.ChecklistDate, req.IsCleared);
        return new SubmitChecklistResult(true, checklist.ChecklistId, null);
    }
}

// ─────────────────────────────────────────────────────────────────────────────
// GetMaintenanceClearanceHandler — resolves Scheduling module's query
// ─────────────────────────────────────────────────────────────────────────────
public sealed class GetMaintenanceClearanceHandler(AssetDbContext db)
    : IRequestHandler<GetMaintenanceClearanceQuery, MaintenanceClearanceDto>
{
    public async Task<MaintenanceClearanceDto> Handle(GetMaintenanceClearanceQuery req, CancellationToken ct)
    {
        var checklist = await db.Checklists.AsNoTracking()
            .FirstOrDefaultAsync(c => c.SimulatorId == req.SimulatorId && c.ChecklistDate == req.SessionDate, ct);

        if (checklist is null)
            return new MaintenanceClearanceDto(req.SimulatorId, req.SessionDate, false, null, null,
                "No maintenance checklist submitted for this simulator on this date.");

        return new MaintenanceClearanceDto(
            req.SimulatorId, req.SessionDate,
            checklist.IsCleared,
            checklist.EngineerCode,
            checklist.SignedOffAt,
            checklist.IsCleared ? null : checklist.BlockingReason);
    }
}
