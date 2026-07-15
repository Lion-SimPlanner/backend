using LionSimPlanner.Shared.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LionSimPlanner.API.Controllers;

/// <summary>
/// Exposes Personnel module data to the Admin and to the Session Pairing Builder.
/// All data access goes through MediatR — no direct DbContext references here.
/// </summary>
[ApiController]
[Route("api/personnel")]
[Authorize]
public class PersonnelController(ISender mediator) : ControllerBase
{
    /// <summary>
    /// Returns the pilot priority queue sorted by NextTrainingDue ASC.
    /// Used by the Admin's Session Pairing Builder to select the most urgent crew.
    /// Requires Admin role.
    /// </summary>
    /// <param name="syllabusFilter">Optional: filter by required syllabus type.</param>
    /// <param name="typeRating">Optional: filter by aircraft type rating.</param>
    [HttpGet("pilots/priority-queue")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetPriorityQueue(
        [FromQuery] string? syllabusFilter = null,
        [FromQuery] string? typeRating = null,
        CancellationToken ct = default)
    {
        var result = await mediator.Send(
            new GetPriorityQueueQuery(syllabusFilter, typeRating), ct);
        return Ok(result);
    }
}
