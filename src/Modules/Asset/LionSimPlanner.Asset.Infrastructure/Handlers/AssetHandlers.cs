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

public sealed class GetSimulatorOperationalStateHandler(AssetDbContext db)
    : IRequestHandler<GetSimulatorOperationalStateQuery, SimulatorOperationalStateDto>
{
    public async Task<SimulatorOperationalStateDto> Handle(GetSimulatorOperationalStateQuery req, CancellationToken ct)
    {
        var simulator = await db.Simulators.AsNoTracking()
            .FirstOrDefaultAsync(s => s.SimulatorId == req.SimulatorId, ct);

        if (simulator is null)
            return new SimulatorOperationalStateDto(req.SimulatorId, false, null, false);

        var status = simulator.Status.ToString();
        var isOperationalUp = simulator.Status == SimulatorStatus.Ready;

        return new SimulatorOperationalStateDto(
            req.SimulatorId,
            true,
            status,
            isOperationalUp);
    }
}

public sealed class SubmitDefectReportHandler(
    AssetDbContext db,
    ISender mediator,
    IHubContext<SimPlannerHub> hubContext,
    ILogger<SubmitDefectReportHandler> logger)
    : IRequestHandler<SubmitDefectReportCommand, SubmitDefectReportResult>
{
    public async Task<SubmitDefectReportResult> Handle(SubmitDefectReportCommand req, CancellationToken ct)
    {
        var simulator = await db.Simulators.FirstOrDefaultAsync(s => s.SimulatorId == req.SimulatorId, ct);
        if (simulator is null)
            return new SubmitDefectReportResult(false, null, $"Simulator {req.SimulatorId} not found.");

        var defect = new SimulatorDefect
        {
            DefectId        = Guid.NewGuid(),
            SimulatorId     = req.SimulatorId,
            SessionId       = req.SessionId,
            ReportedBy      = req.ReportedBy,
            SystemAffected  = req.SystemAffected,
            Severity        = req.Severity,
            InstructorNotes = req.InstructorNotes,
            Status          = "Open",
            ReportedAt      = DateTime.UtcNow,
            CreatedAt       = DateTime.UtcNow,
            UpdatedAt       = DateTime.UtcNow,
        };

        db.Defects.Add(defect);
        await db.SaveChangesAsync(ct);

        logger.LogInformation("[Asset] DefectReport {Id} ({Severity}) filed for Simulator {SimId} by {Reporter}.",
            defect.DefectId, req.Severity, req.SimulatorId, req.ReportedBy);

        if (string.Equals(req.Severity, "AOG", StringComparison.OrdinalIgnoreCase))
        {
            await mediator.Send(new SetSimulatorStatusCommand(
                req.SimulatorId,
                SimulatorStatus.AOG,
                Guid.Empty,
                req.ReportedBy,
                $"[AOG] [{req.SystemAffected}] {req.InstructorNotes}"), ct);

            logger.LogWarning("[Asset] AOG defect — Simulator {SimId} locked.", req.SimulatorId);
        }

        await hubContext.Clients.All.SendAsync("DefectReported", new
        {
            defectId        = defect.DefectId,
            simulatorId     = defect.SimulatorId,
            sessionId       = defect.SessionId,
            reportedBy      = defect.ReportedBy,
            systemAffected  = defect.SystemAffected,
            severity        = defect.Severity,
            instructorNotes = defect.InstructorNotes,
            status          = defect.Status,
            reportedAt      = defect.ReportedAt,
        }, ct);

        return new SubmitDefectReportResult(true, defect.DefectId, null);
    }
}

public sealed class ResolveDefectReportHandler(
    AssetDbContext db,
    ISender mediator,
    IHubContext<SimPlannerHub> hubContext,
    ILogger<ResolveDefectReportHandler> logger)
    : IRequestHandler<ResolveDefectReportCommand, ResolveDefectReportResult>
{
    public async Task<ResolveDefectReportResult> Handle(ResolveDefectReportCommand req, CancellationToken ct)
    {
        var defect = await db.Defects.FirstOrDefaultAsync(d => d.DefectId == req.DefectId, ct);
        if (defect is null)
            return new ResolveDefectReportResult(false, $"Defect {req.DefectId} not found.", null);

        defect.Status                 = "Resolved";
        defect.ResolutionNotes        = req.ResolutionNotes;
        defect.ResolvedByEngineerId   = req.EngineerIdRef;
        defect.ResolvedByEngineerCode = req.EngineerCode;
        defect.ResolvedAt             = DateTime.UtcNow;
        defect.UpdatedAt              = DateTime.UtcNow;

        await db.SaveChangesAsync(ct);

        logger.LogInformation("[Asset] DefectReport {Id} resolved by {Code}.", req.DefectId, req.EngineerCode);

        await mediator.Send(new ResolveDefectCommand(
            defect.SimulatorId,
            req.ResolutionNotes,
            req.EngineerIdRef,
            req.EngineerCode), ct);

        logger.LogInformation("[Asset] Simulator {SimId} returned to Ready after defect resolution.",
            defect.SimulatorId);

        await hubContext.Clients.All.SendAsync("DefectResolved", new
        {
            defectId    = defect.DefectId,
            simulatorId = defect.SimulatorId,
            resolvedAt  = defect.ResolvedAt,
        }, ct);

        return new ResolveDefectReportResult(true, null, defect.ResolvedAt);
    }
}
