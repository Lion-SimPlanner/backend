using LionSimPlanner.Asset.Application.Commands;
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
                status          = s.Status,
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

        var isAog = string.Equals(request.Status, "Down", StringComparison.OrdinalIgnoreCase);
        return Ok(new
        {
            simulatorId  = id,
            newStatus    = request.Status,
            aogTriggered = isAog,
            message      = isAog
                ? "AOG declared. Affected sessions are being automatically cancelled and crew notified."
                : "Simulator status updated."
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
}

public record SetSimulatorStatusRequest(string Status, string? FaultDescription);
public record SubmitChecklistRequest(
    Guid SimulatorId, DateOnly ChecklistDate, bool IsCleared, string Notes, string? BlockingReason);
