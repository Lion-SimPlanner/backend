using LionSimPlanner.Personnel.Infrastructure;
using LionSimPlanner.Scheduling.Application.Commands;
using LionSimPlanner.Scheduling.Infrastructure;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LionSimPlanner.API.Controllers;

[ApiController]
[Route("api/scheduling")]
public class SchedulingController(
    ISender mediator,
    SchedulingDbContext db,
    PersonnelDbContext personnelDb) : ControllerBase
{
    [HttpGet("sessions")]
    [Authorize]
    public async Task<IActionResult> GetSessions(CancellationToken ct)
    {
        var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        var roleClaim = User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value ?? string.Empty;

        var pilots = await personnelDb.Pilots.AsNoTracking().ToDictionaryAsync(p => p.PilotId, p => p.FullName, ct);
        var instructors = await personnelDb.Instructors.AsNoTracking().ToDictionaryAsync(i => i.InstructorId, i => i.FullName, ct);

        var query = db.Sessions.AsNoTracking().AsQueryable();

        if (string.Equals(roleClaim, "Pilot", StringComparison.OrdinalIgnoreCase) && Guid.TryParse(userIdClaim, out var pilotGuid))
        {
            query = query.Where(s => s.CaptainId == pilotGuid || s.FirstOfficerId == pilotGuid);
        }
        else if (string.Equals(roleClaim, "Instructor", StringComparison.OrdinalIgnoreCase) && Guid.TryParse(userIdClaim, out var instructorGuid))
        {
            query = query.Where(s => s.InstructorId == instructorGuid);
        }

        var rawSessions = await query
            .OrderBy(s => s.StartTime)
            .ToListAsync(ct);

        var sessions = rawSessions.Select(s =>
        {
            var cName = s.CaptainId.HasValue && pilots.TryGetValue(s.CaptainId.Value, out var cn) ? cn : null;
            var foName = s.FirstOfficerId.HasValue && pilots.TryGetValue(s.FirstOfficerId.Value, out var fn) ? fn : null;
            var tName = cName ?? foName;
            var tRole = cName != null ? "Captain" : (foName != null ? "First Officer" : null);

            return new
            {
                sessionId           = s.SessionId,
                simulatorId         = s.SimulatorId,
                sessionType         = s.SessionType.ToString(),
                status              = s.Status.ToString(),
                startTime           = s.StartTime,
                endTime             = s.EndTime,
                originalEndTime     = s.OriginalEndTime,
                terminationReason  = s.TerminationReason,
                captainId           = s.CaptainId,
                captainName         = cName,
                firstOfficerId      = s.FirstOfficerId,
                firstOfficerName    = foName,
                traineeName         = tName,
                traineeRole         = tRole,
                instructorId        = s.InstructorId,
                instructorName      = s.InstructorId.HasValue && instructors.TryGetValue(s.InstructorId.Value, out var iName) ? iName : null,
                engineerId          = s.EngineerId,
                syllabusId          = s.SyllabusId,
                traineeEmployeeCode = s.TraineeEmployeeCode,
                isGraded            = s.IsGraded,
                gradeStatus         = s.GradeStatus,
                instructorNotes     = s.InstructorNotes,
                cancellationReason  = s.CancellationReason
            };
        });

        return Ok(sessions);
    }

    [HttpPost("sessions")]
    [HttpPost("/api/sessions")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(object), StatusCodes.Status201Created)]
    public async Task<IActionResult> CreateSession([FromBody] CreateSessionRequest req, CancellationToken ct)
    {
        var captainId = req.CaptainId ?? req.TraineeId;
        var result = await mediator.Send(new CreateSessionCommand(
            req.SimulatorId, req.SessionType, req.StartTime, req.EndTime,
            captainId, req.FirstOfficerId, req.InstructorId, req.EngineerId,
            req.SyllabusId, req.TraineeEmployeeCode), ct);

        if (!result.Success || !result.SessionId.HasValue)
            return BadRequest(new ValidationGateErrorResponse(
                "Session create blocked by Validation Gate. Resolve all violations and retry.",
                result.Violations));

        var id = result.SessionId.Value;
        return CreatedAtAction(nameof(GetSession), new { id }, new { sessionId = id, status = "Draft" });
    }

    [HttpPut("sessions/{id}")]
    [HttpPut("/api/sessions/{id}")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationGateErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> RescheduleSession(Guid id, [FromBody] RescheduleSessionRequest req, CancellationToken ct)
    {
        var result = await mediator.Send(new RescheduleSessionCommand(id, req.StartTime, req.EndTime), ct);

        if (!result.Success)
            return BadRequest(new ValidationGateErrorResponse(
                "Session reschedule blocked by validation checks.",
                result.Violations));

        return Ok(new { sessionId = id, status = "Scheduled", startTime = req.StartTime, endTime = req.EndTime });
    }

    [HttpPatch("sessions/{id}/terminate-early")]
    [HttpPatch("/api/sessions/{id}/terminate-early")]
    [Authorize(Roles = "Admin,Instructor")]
    public async Task<IActionResult> TerminateSessionEarly(Guid id, [FromBody] TerminateSessionEarlyRequest req, CancellationToken ct)
    {
        var result = await mediator.Send(new TerminateSessionEarlyCommand(id, req.ActualEndTime, req.Reason), ct);

        if (!result.Success)
            return BadRequest(new { message = result.ErrorMessage });

        return await GetSession(id, ct);
    }

    [HttpPut("sessions/{id}/publish")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationGateErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> PublishSession(Guid id, CancellationToken ct)
    {
        var result = await mediator.Send(new PublishSessionCommand(id), ct);
        if (!result.Success)
            return BadRequest(new ValidationGateErrorResponse(
                "Session publish blocked by Validation Gate. Resolve all violations and retry.",
                result.Violations));

        return Ok(new { sessionId = id, status = "Scheduled",
            message = "Session published. Crew notification emails dispatched." });
    }

    [HttpPut("sessions/{id}/start")]
    [HttpPatch("sessions/{id}/start")]
    [HttpPatch("/api/sessions/{id}/start")]
    [Authorize(Roles = "Admin,Instructor")]
    public async Task<IActionResult> StartSession(Guid id, CancellationToken ct)
    {
        try
        {
            await mediator.Send(new StartSessionCommand(id), ct);
            return await GetSession(id, ct);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPut("sessions/{id}/grade")]
    [HttpPost("/api/sessions/{id}/grades")]
    [Authorize(Roles = "Instructor")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(object), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> CompleteGrading(
        Guid id, [FromBody] CompleteGradingRequest req, CancellationToken ct)
    {
        var result = await mediator.Send(
            new CompleteGradingCommand(id, req.GradeStatus, req.InstructorNotes, req.TraineeEmployeeCode), ct);

        if (!result.Success)
            return UnprocessableEntity(new { error = result.ErrorMessage });

        return Ok(new { sessionId = id, status = "Completed", cmsSyncTriggered = true });
    }

    [HttpPut("sessions/{id}/cancel")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> CancelSession(
        Guid id, [FromBody] CancelSessionRequest req, CancellationToken ct)
    {
        await mediator.Send(new CancelSessionCommand(id, req.Reason), ct);
        return Ok(new { sessionId = id, status = "Cancelled" });
    }

    [HttpGet("sessions/{id}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetSession(Guid id, CancellationToken ct)
    {
        var session = await db.Sessions.AsNoTracking()
            .FirstOrDefaultAsync(s => s.SessionId == id, ct);
        if (session is null) return NotFound();

        var pilots = await personnelDb.Pilots.AsNoTracking().ToDictionaryAsync(p => p.PilotId, p => p.FullName, ct);
        var instructors = await personnelDb.Instructors.AsNoTracking().ToDictionaryAsync(i => i.InstructorId, i => i.FullName, ct);

        var cName = session.CaptainId.HasValue && pilots.TryGetValue(session.CaptainId.Value, out var cn) ? cn : null;
        var foName = session.FirstOfficerId.HasValue && pilots.TryGetValue(session.FirstOfficerId.Value, out var fn) ? fn : null;
        var tName = cName ?? foName;
        var tRole = cName != null ? "Captain" : (foName != null ? "First Officer" : null);

        return Ok(new
        {
            sessionId           = session.SessionId,
            simulatorId         = session.SimulatorId,
            sessionType         = session.SessionType.ToString(),
            status              = session.Status.ToString(),
            startTime           = session.StartTime,
            endTime             = session.EndTime,
            originalEndTime     = session.OriginalEndTime,
            terminationReason  = session.TerminationReason,
            captainId           = session.CaptainId,
            captainName         = cName,
            firstOfficerId      = session.FirstOfficerId,
            firstOfficerName    = foName,
            traineeName         = tName,
            traineeRole         = tRole,
            instructorId        = session.InstructorId,
            instructorName      = session.InstructorId.HasValue && instructors.TryGetValue(session.InstructorId.Value, out var iName) ? iName : null,
            engineerId          = session.EngineerId,
            syllabusId          = session.SyllabusId,
            traineeEmployeeCode = session.TraineeEmployeeCode,
            isGraded            = session.IsGraded,
            gradeStatus         = session.GradeStatus,
            instructorNotes     = session.InstructorNotes,
            cancellationReason  = session.CancellationReason
        });
    }
}

public record CreateSessionRequest(
    Guid SimulatorId, string SessionType, DateTime StartTime, DateTime EndTime,
    Guid? CaptainId, Guid? FirstOfficerId, Guid? InstructorId, Guid? EngineerId,
    string SyllabusId, string TraineeEmployeeCode, Guid? TraineeId = null, string? TraineeRole = null);

public record RescheduleSessionRequest(DateTime StartTime, DateTime EndTime);
public record TerminateSessionEarlyRequest(DateTime ActualEndTime, string Reason);

public record CompleteGradingRequest(string GradeStatus, string InstructorNotes, string TraineeEmployeeCode);
public record CancelSessionRequest(string Reason);

public record ValidationGateErrorResponse(string Message, IReadOnlyList<string> Violations);

