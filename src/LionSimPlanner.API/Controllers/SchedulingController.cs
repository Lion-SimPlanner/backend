using LionSimPlanner.Scheduling.Application.Commands;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LionSimPlanner.API.Controllers;

/// <summary>
/// Core scheduling lifecycle endpoints. Maps directly to lifecycle steps 3-6 from the spec.
/// </summary>
[ApiController]
[Route("api/scheduling")]
[Authorize]
public class SchedulingController(ISender mediator) : ControllerBase
{
    /// <summary>[Step 3] Create DRAFT session. Validation Gate not triggered yet.</summary>
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

    /// <summary>
    /// [Step 4] THE VALIDATION GATE — attempts DRAFT → SCHEDULED.
    /// Returns HTTP 422 with a structured violations list if any FTL check fails.
    /// Never returns a generic error — each violation is specific and human-readable.
    /// </summary>
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

    /// <summary>[Step 5] Mark session as IN_PROGRESS when operations begin.</summary>
    [HttpPut("sessions/{id}/start")]
    [Authorize(Roles = "Admin,Instructor")]
    public async Task<IActionResult> StartSession(Guid id, CancellationToken ct)
    {
        await mediator.Send(new StartSessionCommand(id), ct);
        return Ok(new { sessionId = id, status = "InProgress" });
    }

    /// <summary>
    /// [Step 6] Instructor submits digital grading form.
    /// Triggers COMPLETED transition + CMS sync via MediatR notification (no direct CMS call here).
    /// </summary>
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

    /// <summary>Admin manually cancels a session.</summary>
    [HttpPut("sessions/{id}/cancel")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> CancelSession(
        Guid id, [FromBody] CancelSessionRequest req, CancellationToken ct)
    {
        await mediator.Send(new CancelSessionCommand(id, req.Reason), ct);
        return Ok(new { sessionId = id, status = "Cancelled" });
    }

    /// <summary>Get session details (all roles).</summary>
    [HttpGet("sessions/{id}")]
    public IActionResult GetSession(Guid id) =>
        Ok(new { sessionId = id, note = "Full query handler — wire up GetSessionByIdQuery as needed." });
}

// ── Request / Response DTOs ───────────────────────────────────────────────────
public record CreateSessionRequest(
    Guid SimulatorId, string SessionType, DateTime StartTime, DateTime EndTime,
    Guid? CaptainId, Guid? FirstOfficerId, Guid? InstructorId, Guid? EngineerId,
    string SyllabusId, string TraineeEmployeeCode);

public record CompleteGradingRequest(string GradeStatus, string InstructorNotes, string TraineeEmployeeCode);
public record CancelSessionRequest(string Reason);

/// <summary>
/// HTTP 422 body returned by the Validation Gate.
/// Each Violation is a self-contained, actionable string — the frontend renders each as a distinct error card.
/// </summary>
public record ValidationGateErrorResponse(string Message, IReadOnlyList<string> Violations);
