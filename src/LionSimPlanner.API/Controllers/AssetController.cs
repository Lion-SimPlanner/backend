using LionSimPlanner.Asset.Application.Commands;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LionSimPlanner.API.Controllers;

/// <summary>
/// Asset/hardware management for the Simulator Engineer role.
/// Setting status to "Down" automatically cascades session cancellations via MediatR.
/// </summary>
[ApiController]
[Route("api/asset")]
[Authorize]
public class AssetController(ISender mediator) : ControllerBase
{
    /// <summary>
    /// [AOG Trigger] Change simulator operational status.
    /// Status = "Down" → publishes SimulatorAOGNotification → bulk-cancels sessions → emails crew.
    /// Requires Engineer role.
    /// </summary>
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

    /// <summary>
    /// [Maintenance Shield] Daily readiness checklist sign-off.
    /// IsCleared=true raises the shield, allowing the Validation Gate to publish sessions.
    /// Requires Engineer role.
    /// </summary>
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

// ── Request DTOs ──────────────────────────────────────────────────────────────
public record SetSimulatorStatusRequest(string Status, string? FaultDescription);
public record SubmitChecklistRequest(
    Guid SimulatorId, DateOnly ChecklistDate, bool IsCleared, string Notes, string? BlockingReason);
