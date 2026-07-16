using LionSimPlanner.Personnel.Infrastructure;
using LionSimPlanner.Shared.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LionSimPlanner.API.Controllers;

[ApiController]
[Route("api/personnel")]
public class PersonnelController(ISender mediator, PersonnelDbContext db) : ControllerBase
{
    [HttpGet("pilots/priority-queue")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetPriorityQueue(
        [FromQuery] string? syllabusFilter = null,
        [FromQuery] string? typeRating = null,
        CancellationToken ct = default)
    {
        var result = await mediator.Send(
            new GetPriorityQueueQuery(syllabusFilter, typeRating), ct);
        return Ok(result);
    }

    [HttpGet("instructors")]
    [AllowAnonymous]
    public async Task<IActionResult> GetInstructors(CancellationToken ct)
    {
        var instructors = await db.Instructors.AsNoTracking()
            .OrderBy(i => i.FullName)
            .Select(i => new
            {
                id               = i.InstructorId,
                employeeCode     = i.EmployeeCode,
                fullName         = i.FullName,
                corporateEmail   = i.CorporateEmail,
                roleLevel        = i.RoleLevel.ToString(),
                certifiedTypes   = i.CertifiedTypes,
                authorizedSyllabi= i.AuthorizedSyllabi,
                licenseExpiry    = i.LicenseExpiry,
                lastDutyEndTime  = i.LastDutyEndTime,
                nextDutyStartTime= i.NextDutyStartTime,
                currentMonthlyHours = i.CurrentMonthlyHours,
                maxMonthlyHours  = i.MaxMonthlyHours
            })
            .ToListAsync(ct);
        return Ok(instructors);
    }
}
