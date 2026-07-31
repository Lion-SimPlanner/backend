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
using System.Globalization;

namespace LionSimPlanner.Scheduling.Infrastructure.Handlers;

internal static class SchedulingValidationSettings
{
    public static double GetMinRestHours(IConfiguration config)
    {
        var raw = config["TrainingSync:MinRestHours"];

        if (double.TryParse(raw, NumberStyles.Float, CultureInfo.InvariantCulture, out var parsed))
            return parsed;

        if (double.TryParse(raw, NumberStyles.Float, CultureInfo.CurrentCulture, out parsed))
            return parsed;

        return 10.0;
    }
}

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

        var minRest = SchedulingValidationSettings.GetMinRestHours(config);
        var validator = new FtlValidationService(minRest);
        var ftlResult = validator.Validate(
            session,
            captain,
            fo,
            instructor,
            captain.IsExternalUser,
            fo?.IsExternalUser ?? false);

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
    IEmailNotificationService emailService,
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

        var minRest   = SchedulingValidationSettings.GetMinRestHours(config);
        var validator = new FtlValidationService(minRest);
        var ftlResult = validator.Validate(
            session,
            captain,
            fo,
            instructor,
            captain.IsExternalUser,
            fo?.IsExternalUser ?? false);

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

        // ── Notify assigned crew via email ─────────────────────────────────────
        var recipientEmails = new List<string>();
        if (!string.IsNullOrWhiteSpace(captain.CorporateEmail))
            recipientEmails.Add(captain.CorporateEmail);
        if (fo is not null && !string.IsNullOrWhiteSpace(fo.CorporateEmail))
            recipientEmails.Add(fo.CorporateEmail);

        if (recipientEmails.Count > 0)
        {
            await emailService.SendSessionScheduledAsync(
                session.SessionId,
                session.StartTime,
                session.EndTime,
                session.SimulatorId.ToString(),
                session.SyllabusId,
                recipientEmails,
                ct);
        }

        return new PublishSessionResult(true, []);
    }
}

public sealed class RescheduleSessionHandler(
    SchedulingDbContext db,
    ISender mediator,
    IEmailNotificationService emailService)
    : IRequestHandler<RescheduleSessionCommand, RescheduleSessionResult>
{
    public async Task<RescheduleSessionResult> Handle(RescheduleSessionCommand req, CancellationToken ct)
    {
        var violations = new List<string>();

        if (req.StartTime >= req.EndTime)
            violations.Add("EndTime must be later than StartTime.");

        var session = await db.Sessions.FirstOrDefaultAsync(s => s.SessionId == req.SessionId, ct);
        if (session is null)
            return new RescheduleSessionResult(false, ["Session not found."]);

        if (session.Status != SessionStatus.Scheduled)
            violations.Add($"Only SCHEDULED sessions can be rescheduled. Current status: {session.Status}.");

        var hasOverlap = await db.Sessions.AsNoTracking()
            .Where(s => s.SessionId != req.SessionId)
            .Where(s => s.SimulatorId == session.SimulatorId)
            .Where(s => s.Status != SessionStatus.Cancelled)
            .AnyAsync(s => req.StartTime < s.EndTime && req.EndTime > s.StartTime, ct);

        if (hasOverlap)
            violations.Add("The selected simulator already has an overlapping session in that time window.");

        if (violations.Count > 0)
            return new RescheduleSessionResult(false, violations.AsReadOnly());

        var originalStart = session.StartTime;
        var originalEnd   = session.EndTime;

        session.StartTime = req.StartTime;
        session.EndTime = req.EndTime;
        session.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);

        // ── Notify assigned pilots via email ───────────────────────────────────
        var recipientEmails = new List<string>();
        var queue = await mediator.Send(new GetPriorityQueueQuery(), ct);

        if (session.CaptainId.HasValue)
        {
            var captain = queue.FirstOrDefault(p => p.PilotId == session.CaptainId.Value);
            if (captain is not null && !string.IsNullOrWhiteSpace(captain.CorporateEmail))
                recipientEmails.Add(captain.CorporateEmail);
        }

        if (session.FirstOfficerId.HasValue)
        {
            var fo = queue.FirstOrDefault(p => p.PilotId == session.FirstOfficerId.Value);
            if (fo is not null && !string.IsNullOrWhiteSpace(fo.CorporateEmail))
                recipientEmails.Add(fo.CorporateEmail);
        }

        await emailService.SendSessionRescheduledAsync(
            session.SessionId,
            originalStart,
            originalEnd,
            session.StartTime,
            session.EndTime,
            session.SimulatorId.ToString(),
            session.SyllabusId,
            recipientEmails,
            ct);

        return new RescheduleSessionResult(true, []);
    }
}

// ─────────────────────────────────────────────────────────────────────────────
// CancelSessionHandler
// ─────────────────────────────────────────────────────────────────────────────
public sealed class CancelSessionHandler(
    SchedulingDbContext db,
    ISender mediator,
    IEmailNotificationService emailService,
    ILogger<CancelSessionHandler> logger)
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

        // ── Notify assigned pilots + ops alert list via email ──────────────────
        var recipientEmails = new List<string>();
        var queue = await mediator.Send(new GetPriorityQueueQuery(), ct);

        if (session.CaptainId.HasValue)
        {
            var captain = queue.FirstOrDefault(p => p.PilotId == session.CaptainId.Value);
            if (captain is not null && !string.IsNullOrWhiteSpace(captain.CorporateEmail))
                recipientEmails.Add(captain.CorporateEmail);
        }

        if (session.FirstOfficerId.HasValue)
        {
            var fo = queue.FirstOrDefault(p => p.PilotId == session.FirstOfficerId.Value);
            if (fo is not null && !string.IsNullOrWhiteSpace(fo.CorporateEmail))
                recipientEmails.Add(fo.CorporateEmail);
        }

        await emailService.SendSessionCancelledAsync(
            session.SessionId,
            session.StartTime,
            session.EndTime,
            req.Reason,
            recipientEmails,
            ct);
    }
}

// ─────────────────────────────────────────────────────────────────────────────
// StartSessionHandler
// ─────────────────────────────────────────────────────────────────────────────
public sealed class StartSessionHandler(
    SchedulingDbContext db,
    ISender mediator) : IRequestHandler<StartSessionCommand>
{
    public async Task Handle(StartSessionCommand req, CancellationToken ct)
    {
        var session = await db.Sessions.FindAsync([req.SessionId], ct)
            ?? throw new InvalidOperationException($"Session {req.SessionId} not found.");

        if (session.Status != SessionStatus.Scheduled)
            throw new InvalidOperationException(
                $"Only SCHEDULED sessions can be started. Current: {session.Status}.");

        var simulatorState = await mediator.Send(
            new GetSimulatorOperationalStateQuery(session.SimulatorId), ct);

        if (!simulatorState.Exists)
            throw new InvalidOperationException(
                "Dispatch Blocked — Simulator record was not found.");

        if (!simulatorState.IsOperationalUp)
            throw new InvalidOperationException(
                $"Dispatch Blocked — Simulator is not Up. Current status: {simulatorState.Status}. Resolve maintenance before starting session.");

        var operationDate = DateOnly.FromDateTime(DateTime.UtcNow);
        var clearance = await mediator.Send(
            new GetMaintenanceClearanceQuery(session.SimulatorId, operationDate), ct);

        if (!clearance.IsCleared)
            throw new InvalidOperationException(
                $"Dispatch Blocked — Maintenance Shield not cleared for {operationDate:yyyy-MM-dd}. Reason: {clearance.BlockingReason ?? "No maintenance checklist submitted for this simulator on this date."}");

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
    ISender mediator,
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

        var queue = await mediator.Send(new GetPriorityQueueQuery(), ct);

        foreach (var s in affected)
        {
            var sessionEmails = new List<string>();
            if (s.CaptainId.HasValue)
            {
                var captain = queue.FirstOrDefault(p => p.PilotId == s.CaptainId.Value);
                if (captain is not null && !string.IsNullOrWhiteSpace(captain.CorporateEmail))
                    sessionEmails.Add(captain.CorporateEmail);
            }

            if (s.FirstOfficerId.HasValue)
            {
                var fo = queue.FirstOrDefault(p => p.PilotId == s.FirstOfficerId.Value);
                if (fo is not null && !string.IsNullOrWhiteSpace(fo.CorporateEmail))
                    sessionEmails.Add(fo.CorporateEmail);
            }

            try
            {
                await emailService.SendSessionCancelledAsync(
                    s.SessionId, s.StartTime, s.EndTime, reason, sessionEmails, ct);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "[AOG] Email failed for session {Id}.", s.SessionId);
            }
        }
    }
}

public sealed class TerminateSessionEarlyHandler(
    SchedulingDbContext db,
    IHubContext<SimPlannerHub> hubContext,
    ILogger<TerminateSessionEarlyHandler> logger)
    : IRequestHandler<TerminateSessionEarlyCommand, TerminateSessionEarlyResult>
{
    public async Task<TerminateSessionEarlyResult> Handle(TerminateSessionEarlyCommand req, CancellationToken ct)
    {
        var session = await db.Sessions.FirstOrDefaultAsync(s => s.SessionId == req.SessionId, ct);
        if (session is null)
            return new TerminateSessionEarlyResult(false, $"Session {req.SessionId} not found.");

        if (session.Status != SessionStatus.InProgress && session.Status != SessionStatus.Scheduled)
            return new TerminateSessionEarlyResult(false,
                $"Only InProgress or Scheduled sessions can be terminated early. Current: '{session.Status}'.");

        if (req.ActualEndTime >= session.EndTime)
            return new TerminateSessionEarlyResult(false,
                "ActualEndTime must be earlier than the current scheduled EndTime.");

        if (req.ActualEndTime <= session.StartTime)
            return new TerminateSessionEarlyResult(false,
                "ActualEndTime must be later than the session StartTime.");

        session.OriginalEndTime    = session.EndTime;
        session.EndTime            = req.ActualEndTime;
        session.TerminationReason  = req.Reason;
        session.Status             = SessionStatus.TerminatedEarly;
        session.UpdatedAt          = DateTime.UtcNow;

        await db.SaveChangesAsync(ct);

        logger.LogInformation(
            "[EarlyTermination] Session {Id} terminated early at {ActualEnd}. Reason: {Reason}.",
            session.SessionId, session.EndTime, session.TerminationReason);

        await hubContext.Clients.All.SendAsync("SessionTerminatedEarly", new
        {
            sessionId       = session.SessionId,
            status          = "TerminatedEarly",
            actualEndTime   = session.EndTime,
            originalEndTime = session.OriginalEndTime,
            reason          = session.TerminationReason
        }, ct);

        return new TerminateSessionEarlyResult(true, null);
    }
}
