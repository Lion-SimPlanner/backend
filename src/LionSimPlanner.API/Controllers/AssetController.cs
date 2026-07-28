using LionSimPlanner.Asset.Application.Commands;
using LionSimPlanner.Asset.Domain.Enums;
using LionSimPlanner.Asset.Infrastructure;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LionSimPlanner.API.Controllers;

[ApiController]
[Route("api/asset")]
public class AssetController(ISender mediator, AssetDbContext db) : ControllerBase
{
    [HttpGet("simulators")]
    [AllowAnonymous]
    public async Task<IActionResult> GetSimulators(CancellationToken ct)
    {
        var sims = await db.Simulators.AsNoTracking()
            .Select(s => new
            {
                id              = s.SimulatorId,
                name            = s.Name,
                bayNumber       = s.BayNumber,
                aircraftType    = s.AircraftType,
                status          = s.Status.ToString(),
                lastChangedAt   = s.LastStatusChangedAt
            })
            .ToListAsync(ct);
        return Ok(sims);
    }

    [HttpGet("engineers")]
    [AllowAnonymous]
    public async Task<IActionResult> GetEngineers(CancellationToken ct)
    {
        var engineers = await db.Engineers.AsNoTracking()
            .Select(e => new
            {
                id              = e.EngineerID,
                employeeCode    = e.EmployeeCode,
                name            = e.FullName,
                clearanceLevel  = e.ClearanceLevel,
                hardwareRatings = e.HardwareRatings,
                shiftStart      = e.ShiftStartTime,
                shiftEnd        = e.ShiftEndTime,
                checkoutTime    = e.CheckoutTime,
                isOnCall        = e.IsOnCall
            })
            .ToListAsync(ct);
        return Ok(engineers);
    }

    [HttpPost("simulators/{id}/status")]
    [Authorize(Roles = "Engineer")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> SetSimulatorStatus(
        Guid id,
        [FromBody] SetSimulatorStatusRequest request,
        CancellationToken ct)
    {
        var engineerIdClaim   = User.FindFirst("sub")?.Value;
        var engineerCodeClaim = User.FindFirst("employee_code")?.Value ?? "UNKNOWN";
        var engineerId        = engineerIdClaim is not null ? Guid.Parse(engineerIdClaim) : Guid.Empty;

        var result = await mediator.Send(new SetSimulatorStatusCommand(
            id, request.Status, engineerId, engineerCodeClaim,
            request.FaultDescription ?? string.Empty), ct);

        if (!result.Success)
            return NotFound(new { error = result.ErrorMessage });

        var isAog = request.Status == SimulatorStatus.AOG;
        return Ok(new
        {
            simulatorId  = id,
            newStatus    = request.Status.ToString(),
            aogTriggered = isAog,
            message      = isAog
                ? "AOG declared. Affected sessions are being automatically cancelled and crew notified."
                : "Simulator status updated."
        });
    }

    [HttpPost("simulators/{id}/ResolveDefect")]
    [Authorize(Roles = "Engineer")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ResolveDefect(
        Guid id,
        [FromBody] ResolveDefectRequest request,
        CancellationToken ct)
    {
        var engineerIdClaim   = User.FindFirst("sub")?.Value;
        var engineerCodeClaim = User.FindFirst("employee_code")?.Value ?? "UNKNOWN";
        var engineerId        = engineerIdClaim is not null ? Guid.Parse(engineerIdClaim) : Guid.Empty;

        var result = await mediator.Send(new ResolveDefectCommand(
            id,
            request.ResolutionDetails,
            engineerId,
            engineerCodeClaim), ct);

        if (!result.Success)
            return NotFound(new { error = result.ErrorMessage });

        return Ok(new
        {
            simulatorId = id,
            newStatus = SimulatorStatus.Ready.ToString(),
            resolvedAt = result.ResolvedAt,
            verified = true,
            message = "Defect resolved and simulator returned to Ready state."
        });
    }

    [HttpPost("maintenance/ResolveDefect")]
    [Authorize(Roles = "Engineer")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ResolveDefectByBody(
        [FromBody] ResolveDefectByBodyRequest request,
        CancellationToken ct)
    {
        var engineerIdClaim   = User.FindFirst("sub")?.Value;
        var engineerCodeClaim = User.FindFirst("employee_code")?.Value ?? "UNKNOWN";
        var engineerId        = engineerIdClaim is not null ? Guid.Parse(engineerIdClaim) : Guid.Empty;

        var result = await mediator.Send(new ResolveDefectCommand(
            request.SimulatorId,
            request.ResolutionDescription,
            engineerId,
            engineerCodeClaim), ct);

        if (!result.Success)
            return NotFound(new { error = result.ErrorMessage });

        return Ok(new
        {
            simulatorId = request.SimulatorId,
            newStatus = SimulatorStatus.Ready.ToString(),
            resolvedAt = result.ResolvedAt,
            verified = true,
            message = "Defect resolved and simulator returned to Ready state."
        });
    }

    [HttpPost("engineers/{id}/checkout")]
    [Authorize(Roles = "Engineer")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CheckoutEngineer(
        Guid id,
        CancellationToken ct)
    {
        var requestedByEngineerIdClaim = User.FindFirst("sub")?.Value;
        var requestedByEngineerCode = User.FindFirst("employee_code")?.Value ?? "UNKNOWN";
        var requestedByEngineerId = requestedByEngineerIdClaim is not null ? Guid.Parse(requestedByEngineerIdClaim) : Guid.Empty;

        var result = await mediator.Send(new CheckoutEngineerCommand(
            id,
            requestedByEngineerId,
            requestedByEngineerCode), ct);

        if (!result.Success)
            return NotFound(new { error = result.ErrorMessage });

        return Ok(new
        {
            engineerId = id,
            checkoutTime = result.CheckoutTime,
            verified = true
        });
    }

    [HttpPost("maintenance/checklist")]
    [Authorize(Roles = "Engineer")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> SubmitMaintenanceChecklist(
        [FromBody] SubmitChecklistRequest request,
        CancellationToken ct)
    {
        var engineerIdClaim   = User.FindFirst("sub")?.Value;
        var engineerCodeClaim = User.FindFirst("employee_code")?.Value ?? "UNKNOWN";
        var engineerId        = engineerIdClaim is not null ? Guid.Parse(engineerIdClaim) : Guid.Empty;

        var result = await mediator.Send(new SubmitMaintenanceChecklistCommand(
            request.SimulatorId, engineerId, engineerCodeClaim,
            request.ChecklistDate, request.IsCleared,
            request.Notes, request.BlockingReason), ct);

        if (!result.Success)
            return BadRequest(new { error = result.ErrorMessage });

        return Ok(new
        {
            checklistId  = result.ChecklistId,
            simulatorId  = request.SimulatorId,
            date         = request.ChecklistDate,
            isCleared    = request.IsCleared,
            shieldStatus = request.IsCleared
                ? "RAISED — Sessions may now be published for this simulator on this date."
                : "BLOCKED — Engineer must resolve blocking issues before sessions can be published."
        });
    }

    [HttpPost("simulators/{id}/defects")]
    [Authorize(Roles = "Instructor,Admin")]
    [ProducesResponseType(typeof(object), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> SubmitDefectReport(
        Guid id,
        [FromBody] SubmitDefectReportRequest request,
        CancellationToken ct)
    {
        var result = await mediator.Send(new SubmitDefectReportCommand(
            id,
            request.SessionId,
            request.ReportedBy,
            request.SystemAffected,
            request.Severity,
            request.InstructorNotes), ct);

        if (!result.Success)
            return NotFound(new { error = result.ErrorMessage });

        return StatusCode(StatusCodes.Status201Created, new
        {
            defectId    = result.DefectId,
            simulatorId = id,
            severity    = request.Severity,
            status      = "Open",
            message     = request.Severity == "AOG"
                ? "AOG defect reported. Simulator locked immediately."
                : $"{request.Severity} defect reported and logged."
        });
    }

    [HttpGet("defects")]
    [Authorize]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetDefectReports(CancellationToken ct)
    {
        var defects = await db.Defects
            .AsNoTracking()
            .Where(d => d.Status != "Resolved")
            .OrderByDescending(d => d.ReportedAt)
            .Select(d => new
            {
                defectId        = d.DefectId,
                simulatorId     = d.SimulatorId,
                sessionId       = d.SessionId,
                reportedBy      = d.ReportedBy,
                systemAffected  = d.SystemAffected,
                severity        = d.Severity,
                instructorNotes = d.InstructorNotes,
                status          = d.Status,
                resolutionNotes = d.ResolutionNotes,
                reportedAt      = d.ReportedAt,
                resolvedAt      = d.ResolvedAt
            })
            .ToListAsync(ct);

        return Ok(defects);
    }

    [HttpPost("defects/{id}/resolve")]
    [Authorize(Roles = "Engineer")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ResolveDefectReport(
        Guid id,
        [FromBody] ResolveDefectReportRequest request,
        CancellationToken ct)
    {
        var engineerIdClaim   = User.FindFirst("sub")?.Value;
        var engineerCodeClaim = User.FindFirst("employee_code")?.Value ?? "UNKNOWN";
        var engineerId        = engineerIdClaim is not null ? Guid.Parse(engineerIdClaim) : Guid.Empty;

        var result = await mediator.Send(new ResolveDefectReportCommand(
            id,
            request.ResolutionNotes,
            engineerId,
            engineerCodeClaim), ct);

        if (!result.Success)
            return NotFound(new { error = result.ErrorMessage });

        return Ok(new
        {
            defectId   = id,
            resolvedAt = result.ResolvedAt,
            message    = "Defect resolved. Simulator status updated to Ready if AOG was active."
        });
    }
}

public record SetSimulatorStatusRequest(SimulatorStatus Status, string? FaultDescription);
public record ResolveDefectRequest(string ResolutionDetails);
public record ResolveDefectByBodyRequest(Guid SimulatorId, string ResolutionDescription);
public record SubmitChecklistRequest(
    Guid SimulatorId, DateOnly ChecklistDate, bool IsCleared, string Notes, string? BlockingReason);
public record SubmitDefectReportRequest(
    Guid? SessionId, string ReportedBy, string SystemAffected, string Severity, string InstructorNotes);
public record ResolveDefectReportRequest(string ResolutionNotes);

