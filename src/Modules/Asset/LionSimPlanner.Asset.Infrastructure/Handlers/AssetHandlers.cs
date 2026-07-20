using LionSimPlanner.Shared.Hubs;
using LionSimPlanner.Asset.Application.Commands;
using LionSimPlanner.Asset.Domain.Entities;
using LionSimPlanner.Asset.Domain.Enums;
using LionSimPlanner.Shared.Dtos;
using LionSimPlanner.Shared.Events;
using LionSimPlanner.Shared.Queries;
using MediatR;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace LionSimPlanner.Asset.Infrastructure.Handlers;

public sealed class SetSimulatorStatusHandler(
    AssetDbContext db,
    IPublisher publisher,
    IHubContext<SimPlannerHub> hubContext,
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

        if (req.NewStatus == SimulatorStatus.Ready)
        {
            var openLog = await db.MaintenanceLogs
                .Where(x => x.SimulatorId == req.SimulatorId && x.ResolvedAt == null)
                .OrderByDescending(x => x.CreatedAt)
                .FirstOrDefaultAsync(ct);

            if (openLog is not null)
            {
                openLog.ResolutionDescription = string.IsNullOrWhiteSpace(req.FaultDescription)
                    ? "Status set to Ready"
                    : req.FaultDescription;
                openLog.ResolvedAt = DateTime.UtcNow;
                openLog.UpdatedAt = DateTime.UtcNow;
            }
        }
        else
        {
            db.MaintenanceLogs.Add(new MaintenanceLog
            {
                MaintenanceLogId = Guid.NewGuid(),
                SimulatorId = req.SimulatorId,
                Severity = req.NewStatus.ToString(),
                FaultDescription = string.IsNullOrWhiteSpace(req.FaultDescription)
                    ? $"Simulator marked as {req.NewStatus}"
                    : req.FaultDescription,
                ResolutionDescription = null,
                ResolvedAt = null,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            });
        }

        await db.SaveChangesAsync(ct);

        logger.LogInformation("[Asset] Simulator {Id}: {Prev} → {New} by {Code}.",
            req.SimulatorId, prev.ToString(), req.NewStatus.ToString(), req.EngineerCode);

        if (req.NewStatus == SimulatorStatus.AOG)
        {
            logger.LogWarning("[Asset] Simulator {Id} DOWN — publishing AOG notification.", req.SimulatorId);
            await publisher.Publish(new SimulatorAOGNotification(
                req.SimulatorId,
                simulator.Name,
                req.EngineerCode,
                req.FaultDescription,
                DateTime.UtcNow), ct);

            await hubContext.Clients.All.SendAsync("AogReported", new
            {
                simulatorId = req.SimulatorId,
                simulatorName = simulator.Name,
                status = "AOG",
                reportedBy = req.EngineerCode,
                faultDescription = req.FaultDescription,
                occurredAt = DateTime.UtcNow
            }, ct);
        }

        return new SetSimulatorStatusResult(true, null);
    }
}

public sealed class ResolveDefectHandler(
    AssetDbContext db,
    ILogger<ResolveDefectHandler> logger)
    : IRequestHandler<ResolveDefectCommand, ResolveDefectResult>
{
    public async Task<ResolveDefectResult> Handle(ResolveDefectCommand req, CancellationToken ct)
    {
        var simulator = await db.Simulators.FirstOrDefaultAsync(s => s.SimulatorId == req.SimulatorId, ct);
        if (simulator is null)
            return new ResolveDefectResult(false, $"Simulator {req.SimulatorId} not found.", null);

        var activeLog = await db.MaintenanceLogs
            .Where(x => x.SimulatorId == req.SimulatorId && x.ResolvedAt == null)
            .OrderByDescending(x => x.CreatedAt)
            .FirstOrDefaultAsync(ct);

        var resolvedAt = DateTime.UtcNow;

        if (activeLog is null)
        {
            activeLog = new MaintenanceLog
            {
                MaintenanceLogId = Guid.NewGuid(),
                SimulatorId = req.SimulatorId,
                Severity = SimulatorStatus.Defect.ToString(),
                FaultDescription = "Auto-created from ResolveDefect action",
                ResolutionDescription = req.ResolutionDescription,
                ResolvedAt = resolvedAt,
                CreatedAt = resolvedAt,
                UpdatedAt = resolvedAt,
            };
            db.MaintenanceLogs.Add(activeLog);
        }
        else
        {
            activeLog.ResolutionDescription = req.ResolutionDescription;
            activeLog.ResolvedAt = resolvedAt;
            activeLog.UpdatedAt = resolvedAt;
        }

        simulator.Status = SimulatorStatus.Ready;
        simulator.LastStatusChangedByEngineerId = req.EngineerIdRef;
        simulator.LastStatusChangedByEngineerCode = req.EngineerCode;
        simulator.LastStatusChangedAt = resolvedAt;
        simulator.UpdatedAt = resolvedAt;

        await db.SaveChangesAsync(ct);

        logger.LogInformation("[Asset] ResolveDefect completed for simulator {Id} by {Code}.", req.SimulatorId, req.EngineerCode);
        return new ResolveDefectResult(true, null, resolvedAt);
    }
}

public sealed class CheckoutEngineerHandler(
    AssetDbContext db,
    ILogger<CheckoutEngineerHandler> logger)
    : IRequestHandler<CheckoutEngineerCommand, CheckoutEngineerResult>
{
    public async Task<CheckoutEngineerResult> Handle(CheckoutEngineerCommand req, CancellationToken ct)
    {
        var engineer = await db.Engineers.FirstOrDefaultAsync(e => e.EngineerID == req.EngineerId, ct);
        if (engineer is null)
            return new CheckoutEngineerResult(false, $"Engineer {req.EngineerId} not found.", null);

        var checkoutAt = DateTime.UtcNow;
        engineer.CheckoutTime = checkoutAt;
        engineer.UpdatedAt = checkoutAt;
        await db.SaveChangesAsync(ct);

        logger.LogInformation("[Asset] Engineer {Id} checked out at {CheckoutAt}.", req.EngineerId, checkoutAt);
        return new CheckoutEngineerResult(true, null, checkoutAt);
    }
}

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
