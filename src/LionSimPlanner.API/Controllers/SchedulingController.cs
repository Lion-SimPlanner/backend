using LionSimPlanner.Scheduling.Application.Commands;
using LionSimPlanner.Scheduling.Infrastructure;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LionSimPlanner.API.Controllers;

[ApiController]
[Route("api/scheduling")]
public class SchedulingController(ISender mediator, SchedulingDbContext db) : ControllerBase
{
    [HttpGet("sessions")]
    [AllowAnonymous]
    public async Task<IActionResult> GetSessions(CancellationToken ct)
    {
        var sessions = await db.Sessions.AsNoTracking()
            .OrderBy(s => s.StartTime)
            .Select(s => new
            {
                sessionId           = s.SessionId,
                simulatorId         = s.SimulatorId,
                sessionType         = s.SessionType.ToString(),
                status              = s.Status.ToString(),
                startTime           = s.StartTime,
                endTime             = s.EndTime,
                captainId           = s.CaptainId,
                firstOfficerId      = s.FirstOfficerId,
                instructorId        = s.InstructorId,
                engineerId          = s.EngineerId,
                syllabusId          = s.SyllabusId,
                traineeEmployeeCode = s.TraineeEmployeeCode,
                isGraded            = s.IsGraded,
                gradeStatus         = s.GradeStatus,
                instructorNotes     = s.InstructorNotes,
                cancellationReason  = s.CancellationReason
            })
            .ToListAsync(ct);
        return Ok(sessions);
    }

    [HttpPost("sessions")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(object), StatusCodes.Status201Created)]
    public async Task<IActionResult> CreateSession([FromBody] CreateSessionRequest req, CancellationToken ct)
    {
        var id = await mediator.Send(new CreateSessionCommand(
            req.SimulatorId, req.SessionType, req.StartTime, req.EndTime,
            req.CaptainId, req.FirstOfficerId, req.InstructorId, req.EngineerId,
            req.SyllabusId, req.TraineeEmployeeCode), ct);

        return CreatedAtAction(nameof(GetSession), new { id }, new { sessionId = id, status = "Draft" });
    }

    [HttpPut("sessions/{id}/publish")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationGateErrorResponse), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> PublishSession(Guid id, CancellationToken ct)
    {
        var result = await mediator.Send(new PublishSessionCommand(id), ct);
        if (!result.Success)
            return UnprocessableEntity(new ValidationGateErrorResponse(
                "Session publish blocked by Validation Gate. Resolve all violations and retry.",
                result.Violations));

        return Ok(new { sessionId = id, status = "Scheduled",
            message = "Session published. Crew notification emails dispatched." });
    }

    [HttpPut("sessions/{id}/start")]
    [Authorize(Roles = "Admin,Instructor")]
    public async Task<IActionResult> StartSession(Guid id, CancellationToken ct)
    {
        await mediator.Send(new StartSessionCommand(id), ct);
        return Ok(new { sessionId = id, status = "InProgress" });
    }

    [HttpPut("sessions/{id}/grade")]
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
        return Ok(session);
    }
}

public record CreateSessionRequest(
    Guid SimulatorId, string SessionType, DateTime StartTime, DateTime EndTime,
    Guid? CaptainId, Guid? FirstOfficerId, Guid? InstructorId, Guid? EngineerId,
    string SyllabusId, string TraineeEmployeeCode);

public record CompleteGradingRequest(string GradeStatus, string InstructorNotes, string TraineeEmployeeCode);
public record CancelSessionRequest(string Reason);

public record ValidationGateErrorResponse(string Message, IReadOnlyList<string> Violations);
