using LionSimPlanner.Personnel.Infrastructure;
using LionSimPlanner.Personnel.Domain.Entities;
using LionSimPlanner.Personnel.Domain.Enums;
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
    [HttpPost("/api/pilots/external")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateExternalPilot([FromBody] CreateExternalPilotRequest request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.FullName))
            return BadRequest(new { error = "FullName is required." });

        var employeeCode = await GenerateExternalEmployeeCode(ct);
        var now = DateTime.UtcNow;

        var pilot = new Pilot
        {
            PilotId = Guid.NewGuid(),
            EmployeeCode = employeeCode,
            FullName = request.FullName.Trim(),
            CorporateEmail = string.IsNullOrWhiteSpace(request.Email) ? string.Empty : request.Email.Trim(),
            CompanyName = string.IsNullOrWhiteSpace(request.CompanyName) ? null : request.CompanyName.Trim(),
            ContactNumber = string.IsNullOrWhiteSpace(request.ContactNumber) ? null : request.ContactNumber.Trim(),
            IsExternalUser = true,
            FtlStatus = null,
            Rank = PilotRank.FirstOfficer,
            TypeRatings = new List<string>(),
            MedicalExpiry = now.AddYears(5),
            LastTrainingDate = now,
            NextTrainingDue = now.AddYears(1),
            RequiredSyllabus = "External",
            LastDutyEndTime = now.AddHours(-24),
            NextDutyStartTime = now,
            CreatedAt = now,
            UpdatedAt = now,
        };

        db.Pilots.Add(pilot);
        await db.SaveChangesAsync(ct);

        return Created($"/api/pilots/{pilot.PilotId}", new
        {
            pilotId = pilot.PilotId,
            employeeCode = pilot.EmployeeCode,
            fullName = pilot.FullName,
            rank = pilot.Rank.ToString(),
            isExternalUser = pilot.IsExternalUser,
            nextTrainingDue = pilot.NextTrainingDue,
            requiredSyllabus = pilot.RequiredSyllabus,
            typeRatings = pilot.TypeRatings ?? new List<string>(),
            medicalExpiry = pilot.MedicalExpiry,
            lastDutyEndTime = pilot.LastDutyEndTime,
            nextDutyStartTime = pilot.NextDutyStartTime,
            corporateEmail = pilot.CorporateEmail,
            companyName = pilot.CompanyName,
            contactNumber = pilot.ContactNumber
        });
    }

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

    private async Task<string> GenerateExternalEmployeeCode(CancellationToken ct)
    {
        for (var i = 0; i < 10; i++)
        {
            var candidate = $"EXT-{Guid.NewGuid():N}"[..12].ToUpperInvariant();
            var exists = await db.Pilots.AsNoTracking().AnyAsync(p => p.EmployeeCode == candidate, ct);
            if (!exists) return candidate;
        }

        throw new InvalidOperationException("Unable to generate unique external employee code.");
    }
}

public record CreateExternalPilotRequest(string FullName, string? Email, string? ContactNumber, string? CompanyName);
