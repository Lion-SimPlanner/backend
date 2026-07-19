using LionSimPlanner.Notifications;
using LionSimPlanner.Scheduling.Application.Commands;
using LionSimPlanner.Scheduling.Domain.Entities;
using LionSimPlanner.Scheduling.Domain.Enums;
using LionSimPlanner.Scheduling.Domain.Validation;
using LionSimPlanner.Shared.Events;
using LionSimPlanner.Shared.Hubs;
using LionSimPlanner.Shared.Queries;
using MediatR;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace LionSimPlanner.Scheduling.Infrastructure.Handlers;

// ─────────────────────────────────────────────────────────────────────────────
// CreateSessionHandler
// ─────────────────────────────────────────────────────────────────────────────
public sealed class CreateSessionHandler(
    SchedulingDbContext db,
    ISender mediator,
    IConfiguration config,
    ILogger<CreateSessionHandler> logger)
    : IRequestHandler<CreateSessionCommand, CreateSessionResult>
{
    public async Task<CreateSessionResult> Handle(CreateSessionCommand req, CancellationToken ct)
    {
        if (!Enum.TryParse<SessionType>(req.SessionType, true, out var parsedType))
            return new CreateSessionResult(false, null, ["Validation Gate Blocked: Invalid session type."]);

        var session = new SimulatorSession
        {
            SessionId           = Guid.NewGuid(),
            SimulatorId         = req.SimulatorId,
            SessionType         = parsedType,
            Status              = SessionStatus.Draft,
            StartTime           = req.StartTime,
            EndTime             = req.EndTime,
            CaptainId           = req.CaptainId,
            FirstOfficerId      = req.FirstOfficerId,
            InstructorId        = req.InstructorId,
            EngineerId          = req.EngineerId,
            SyllabusId          = req.SyllabusId,
            TraineeEmployeeCode = req.TraineeEmployeeCode,
            IsGraded            = false,
            CreatedAt           = DateTime.UtcNow,
            UpdatedAt           = DateTime.UtcNow
        };

        var queue = await mediator.Send(new GetPriorityQueueQuery(), ct);
        var captain = queue.FirstOrDefault(p => p.PilotId == session.CaptainId);
        if (captain is null)
            return new CreateSessionResult(false, null,
                ["Validation Gate Blocked: No Captain assigned. Assign a Captain before publishing."]);

        var fo = session.FirstOfficerId.HasValue
            ? queue.FirstOrDefault(p => p.PilotId == session.FirstOfficerId.Value)
            : null;

        var instructor = await mediator.Send(new GetInstructorByIdQuery(session.InstructorId ?? Guid.Empty), ct);
        if (instructor is null)
            return new CreateSessionResult(false, null,
                ["Validation Gate Blocked: No Instructor assigned. Assign a qualified Instructor before publishing."]);

        var clearance = await mediator.Send(
            new GetMaintenanceClearanceQuery(session.SimulatorId, DateOnly.FromDateTime(session.StartTime)), ct);

        var minRest = double.TryParse(config["TrainingSync:MinRestHours"], out var h) ? h : 10.0;
        var validator = new FtlValidationService(minRest);
        var ftlResult = validator.Validate(session, captain, fo, instructor, clearance);

        if (!ftlResult.IsValid)
        {
            logger.LogWarning("[ValidationGate] Session {Id} CREATE BLOCKED. {Count} violation(s).",
                session.SessionId, ftlResult.Violations.Count);
            return new CreateSessionResult(false, null, ftlResult.Violations.AsReadOnly());
        }

        db.Sessions.Add(session);
        await db.SaveChangesAsync(ct);
        logger.LogInformation("[Scheduling] Session {Id} created as DRAFT.", session.SessionId);
        return new CreateSessionResult(true, session.SessionId, []);
    }
}

// ─────────────────────────────────────────────────────────────────────────────
// PublishSessionHandler — THE VALIDATION GATE (Lifecycle Step 4)
// ─────────────────────────────────────────────────────────────────────────────
public sealed class PublishSessionHandler(
    SchedulingDbContext db,
    ISender mediator,
    IConfiguration config,
    ILogger<PublishSessionHandler> logger)
    : IRequestHandler<PublishSessionCommand, PublishSessionResult>
{
    public async Task<PublishSessionResult> Handle(PublishSessionCommand req, CancellationToken ct)
    {
        var session = await db.Sessions.FirstOrDefaultAsync(s => s.SessionId == req.SessionId, ct)
            ?? throw new InvalidOperationException($"Session {req.SessionId} not found.");

        if (session.Status != SessionStatus.Draft)
            return new PublishSessionResult(false,
                [$"Session {session.SessionId} is already '{session.Status}' and cannot be re-published."]);

        logger.LogInformation("[ValidationGate] Starting for session {Id}.", session.SessionId);

        // ── Fetch crew via MediatR (no direct Personnel project references) ───
        var queue   = await mediator.Send(new GetPriorityQueueQuery(), ct);
        var captain = queue.FirstOrDefault(p => p.PilotId == session.CaptainId);
        if (captain is null)
            return new PublishSessionResult(false,
                ["Validation Gate Blocked: No Captain assigned. Assign a Captain before publishing."]);

        var fo = session.FirstOfficerId.HasValue
            ? queue.FirstOrDefault(p => p.PilotId == session.FirstOfficerId.Value)
            : null;

        var instructor = await mediator.Send(new GetInstructorByIdQuery(session.InstructorId ?? Guid.Empty), ct);
        if (instructor is null)
            return new PublishSessionResult(false,
                ["Validation Gate Blocked: No Instructor assigned. Assign a qualified Instructor before publishing."]);

        // ── Fetch maintenance clearance via MediatR (no Asset project reference) 
        var clearance = await mediator.Send(
            new GetMaintenanceClearanceQuery(session.SimulatorId, DateOnly.FromDateTime(session.StartTime)), ct);

        // ── Run FTL Validation ────────────────────────────────────────────────
        var minRest   = double.TryParse(config["TrainingSync:MinRestHours"], out var h) ? h : 10.0;
        var validator = new FtlValidationService(minRest);
        var ftlResult = validator.Validate(session, captain, fo, instructor, clearance);

        if (!ftlResult.IsValid)
        {
            logger.LogWarning("[ValidationGate] Session {Id} BLOCKED. {Count} violation(s).",
                session.SessionId, ftlResult.Violations.Count);
            return new PublishSessionResult(false, ftlResult.Violations.AsReadOnly());
        }

        // ── Transition to SCHEDULED ───────────────────────────────────────────
        session.Status    = SessionStatus.Scheduled;
        session.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);

        logger.LogInformation("[ValidationGate] Session {Id} → SCHEDULED.", session.SessionId);
        return new PublishSessionResult(true, []);
    }
}

// ─────────────────────────────────────────────────────────────────────────────
// CancelSessionHandler
// ─────────────────────────────────────────────────────────────────────────────
public sealed class CancelSessionHandler(SchedulingDbContext db, ILogger<CancelSessionHandler> logger)
    : IRequestHandler<CancelSessionCommand>
{
    public async Task Handle(CancelSessionCommand req, CancellationToken ct)
    {
        var session = await db.Sessions.FindAsync([req.SessionId], ct)
            ?? throw new InvalidOperationException($"Session {req.SessionId} not found.");
        session.Status             = SessionStatus.Cancelled;
        session.CancellationReason = req.Reason;
        session.UpdatedAt          = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        logger.LogInformation("[Scheduling] Session {Id} cancelled. Reason: {Reason}",
            req.SessionId, req.Reason);
    }
}

// ─────────────────────────────────────────────────────────────────────────────
// StartSessionHandler
// ─────────────────────────────────────────────────────────────────────────────
public sealed class StartSessionHandler(SchedulingDbContext db) : IRequestHandler<StartSessionCommand>
{
    public async Task Handle(StartSessionCommand req, CancellationToken ct)
    {
        var session = await db.Sessions.FindAsync([req.SessionId], ct)
            ?? throw new InvalidOperationException($"Session {req.SessionId} not found.");
        if (session.Status != SessionStatus.Scheduled)
            throw new InvalidOperationException(
                $"Only SCHEDULED sessions can be started. Current: {session.Status}.");
        session.Status    = SessionStatus.InProgress;
        session.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
    }
}

// ─────────────────────────────────────────────────────────────────────────────
// CompleteGradingHandler (Lifecycle Step 6)
// ─────────────────────────────────────────────────────────────────────────────
public sealed class CompleteGradingHandler(
    SchedulingDbContext db,
    IPublisher publisher,
    IHubContext<SimPlannerHub> hubContext,
    ILogger<CompleteGradingHandler> logger)
    : IRequestHandler<CompleteGradingCommand, CompleteGradingResult>
{
    public async Task<CompleteGradingResult> Handle(CompleteGradingCommand req, CancellationToken ct)
    {
        var session = await db.Sessions.FirstOrDefaultAsync(s => s.SessionId == req.SessionId, ct);
        if (session is null)
            return new CompleteGradingResult(false, $"Session {req.SessionId} not found.");

        if (session.Status != SessionStatus.InProgress)
            return new CompleteGradingResult(false,
                $"Grading can only be submitted for IN_PROGRESS sessions. Current: '{session.Status}'.");

        session.IsGraded            = true;
        session.GradeStatus         = req.GradeStatus.ToUpperInvariant();
        session.InstructorNotes     = req.InstructorNotes;
        session.TraineeEmployeeCode = req.TraineeEmployeeCode;
        session.Status              = SessionStatus.Completed;
        session.UpdatedAt           = DateTime.UtcNow;

        await db.SaveChangesAsync(ct);
        logger.LogInformation("[Grading] Session {Id} COMPLETED. Grade: {Grade}.",
            session.SessionId, session.GradeStatus);

        await hubContext.Clients.All.SendAsync("SessionGraded", new
        {
            sessionId = session.SessionId,
            status = "Completed",
            gradeStatus = session.GradeStatus,
            syllabusId = session.SyllabusId,
            traineeEmployeeCode = session.TraineeEmployeeCode
        }, ct);

        await publisher.Publish(new TrainingRecordCompletedNotification(
            session.SessionId,
            req.TraineeEmployeeCode,
            session.SyllabusId,
            session.IsGraded,
            session.GradeStatus,
            session.UpdatedAt,
            session.InstructorNotes), ct);

        return new CompleteGradingResult(true, null);
    }
}

// ─────────────────────────────────────────────────────────────────────────────
// SimulatorAOGHandler (Lifecycle Step 5)
// ─────────────────────────────────────────────────────────────────────────────
public sealed class SimulatorAOGHandler(
    SchedulingDbContext db,
    IEmailNotificationService emailService,
    ILogger<SimulatorAOGHandler> logger)
    : INotificationHandler<SimulatorAOGNotification>
{
    public async Task Handle(SimulatorAOGNotification notification, CancellationToken ct)
    {
        logger.LogWarning("[AOG] Simulator {Id} is DOWN. Cascading cancellations.", notification.SimulatorId);

        var affected = await db.Sessions
            .Where(s =>
                s.SimulatorId == notification.SimulatorId &&
                (s.Status == SessionStatus.Scheduled || s.Status == SessionStatus.InProgress) &&
                s.StartTime >= notification.OccurredAt)
            .ToListAsync(ct);

        if (affected.Count == 0)
        {
            logger.LogInformation("[AOG] No active sessions found for Simulator {Id}.", notification.SimulatorId);
            return;
        }

        var reason =
            $"SIMULATOR AOG — {notification.SimulatorName} taken offline at " +
            $"{notification.OccurredAt:yyyy-MM-dd HH:mm} UTC by Engineer {notification.ReportedByEngineerCode}. " +
            $"Fault: {notification.FaultDescription}. Contact Training Admin to reschedule.";

        foreach (var s in affected)
        {
            s.Status             = SessionStatus.Cancelled;
            s.CancellationReason = reason;
            s.UpdatedAt          = DateTime.UtcNow;
        }
        await db.SaveChangesAsync(ct);

        logger.LogWarning("[AOG] Cancelled {Count} session(s) for Simulator {Id}.",
            affected.Count, notification.SimulatorId);

        foreach (var s in affected)
        {
            try
            {
                await emailService.SendSessionCancelledAsync(
                    s.SessionId, s.StartTime, s.EndTime, reason, ct);
            }
            catch (Exception ex)
            {
                // Email failure must never roll back the cancellations
                logger.LogError(ex, "[AOG] Email failed for session {Id}.", s.SessionId);
            }
        }
    }
}
