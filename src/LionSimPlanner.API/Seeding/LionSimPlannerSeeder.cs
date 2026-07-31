using LionSimPlanner.Asset.Domain.Entities;
using LionSimPlanner.Asset.Domain.Enums;
using LionSimPlanner.Asset.Infrastructure;
using LionSimPlanner.Personnel.Domain.Entities;
using LionSimPlanner.Personnel.Domain.Enums;
using LionSimPlanner.Personnel.Infrastructure;
using LionSimPlanner.Scheduling.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace LionSimPlanner.API.Seeding;

public static class LionSimPlannerSeeder
{
    private static readonly string[] AircraftTypes =
    [
        "B737-800NG",
        "B737 MAX 8",
        "A320-200",
        "A320neo",
        "A330-300",
        "ATR 72-600"
    ];

    private static readonly Guid[] SimulatorIds =
    [
        new("a1a1a1a1-0001-0001-0001-000000000001"),
        new("a1a1a1a1-0001-0001-0001-000000000002"),
        new("a1a1a1a1-0001-0001-0001-000000000003"),
        new("a1a1a1a1-0001-0001-0001-000000000004"),
        new("a1a1a1a1-0001-0001-0001-000000000005"),
        new("a1a1a1a1-0001-0001-0001-000000000006"),
    ];

    private static readonly Guid[] PilotIds =
    [
        new("b2b2b2b2-0002-0002-0002-000000000001"),
        new("b2b2b2b2-0002-0002-0002-000000000002"),
        new("b2b2b2b2-0002-0002-0002-000000000003"),
        new("b2b2b2b2-0002-0002-0002-000000000004"),
        new("b2b2b2b2-0002-0002-0002-000000000005"),
        new("b2b2b2b2-0002-0002-0002-000000000006"),
        new("b2b2b2b2-0002-0002-0002-000000000007"),
        new("b2b2b2b2-0002-0002-0002-000000000008"),
        new("b2b2b2b2-0002-0002-0002-000000000009"),
        new("b2b2b2b2-0002-0002-0002-000000000010"),
        new("b2b2b2b2-0002-0002-0002-000000000011"),
        new("b2b2b2b2-0002-0002-0002-000000000012"),
    ];

    private static readonly Guid[] InstructorIds =
    [
        new("c3c3c3c3-0003-0003-0003-000000000001"),
        new("c3c3c3c3-0003-0003-0003-000000000002"),
        new("c3c3c3c3-0003-0003-0003-000000000003"),
        new("c3c3c3c3-0003-0003-0003-000000000004"),
        new("c3c3c3c3-0003-0003-0003-000000000005"),
        new("c3c3c3c3-0003-0003-0003-000000000006"),
    ];

    private static readonly Guid[] EngineerIds =
    [
        new("d4d4d4d4-0004-0004-0004-000000000001"),
        new("d4d4d4d4-0004-0004-0004-000000000002"),
        new("d4d4d4d4-0004-0004-0004-000000000003"),
        new("d4d4d4d4-0004-0004-0004-000000000004"),
        new("d4d4d4d4-0004-0004-0004-000000000005"),
        new("d4d4d4d4-0004-0004-0004-000000000006"),
    ];

    public static async Task SeedAsync(
        PersonnelDbContext hr,
        AssetDbContext maint,
        SchedulingDbContext sched,
        ILogger logger)
    {
        await SeedPersonnelAsync(hr, logger);
        await SeedAssetsAsync(maint, logger);
    }

    private static async Task SeedPersonnelAsync(PersonnelDbContext db, ILogger logger)
    {
        var now = DateTime.UtcNow;

        if (await db.Pilots.AnyAsync())
        {
            logger.LogInformation("[Seeder] hr.pilots already has data — skipping pilot seed.");
        }
        else
        {
            // 12 Trainees (2 per aircraft type, strict Trainee naming)
            var pilots = new List<Pilot>();
            for (var i = 0; i < 12; i++)
            {
                var planeType = AircraftTypes[i / 2];
                pilots.Add(new Pilot
                {
                    PilotId = PilotIds[i],
                    EmployeeCode = $"LGA-PLT-{i + 1:000}",
                    FullName = $"Trainee Pilot {i + 1}",
                    CorporateEmail = $"pilot{i + 1}@lionair.co.id",
                    Rank = PilotRank.FirstOfficer,
                    TypeRatings = [planeType],
                    MedicalExpiry = now.AddMonths(12),
                    LastTrainingDate = now.AddDays(-30),
                    NextTrainingDue = now.AddDays(90),
                    RequiredSyllabus = "InitialTypeRating",
                    LastDutyEndTime = now.AddHours(-16),
                    NextDutyStartTime = now.AddHours(8),
                    CreatedAt = now,
                    UpdatedAt = now,
                });
            }
            db.Pilots.AddRange(pilots);
        }

        if (await db.Instructors.AnyAsync())
        {
            logger.LogInformation("[Seeder] hr.instructors already has data — skipping instructor seed.");
        }
        else
        {
            // 6 Instructors (1 per aircraft type)
            var instructors = new List<Instructor>();
            for (var i = 0; i < 6; i++)
            {
                var planeType = AircraftTypes[i];
                instructors.Add(new Instructor
                {
                    InstructorId = InstructorIds[i],
                    EmployeeCode = $"LGA-INS-{i + 1:000}",
                    FullName = $"Instr. Instructor {i + 1}",
                    CorporateEmail = $"instructor{i + 1}@lionair.co.id",
                    RoleLevel = InstructorRoleLevel.TRI,
                    CertifiedTypes = [planeType],
                    AuthorizedSyllabi = ["InitialTypeRating", "RecurrentTraining", "LOFT", "OPC", "LPC"],
                    LicenseExpiry = now.AddMonths(24),
                    LastDutyEndTime = now.AddHours(-16),
                    NextDutyStartTime = now.AddHours(8),
                    CurrentMonthlyHours = 20,
                    MaxMonthlyHours = 100,
                    CreatedAt = now,
                    UpdatedAt = now,
                });
            }
            db.Instructors.AddRange(instructors);
        }

        await db.SaveChangesAsync();
        logger.LogInformation("[Seeder] hr schema checked (pilots/instructors).");
    }

    private static async Task SeedAssetsAsync(AssetDbContext db, ILogger logger)
    {
        var now = DateTime.UtcNow;

        if (await db.Simulators.AnyAsync())
        {
            logger.LogInformation("[Seeder] maint.simulators already has data — skipping simulator seed.");
        }
        else
        {
            // 6 Simulators (1 per aircraft type, all 'Ready')
            var simulators = new List<Simulator>();
            for (var i = 0; i < 6; i++)
            {
                var planeType = AircraftTypes[i];
                simulators.Add(new Simulator
                {
                    SimulatorId = SimulatorIds[i],
                    Name = $"Jakarta {planeType} Full Flight Simulator",
                    BayNumber = $"Bay {i + 1}",
                    AircraftType = planeType,
                    Status = SimulatorStatus.Ready,
                    LastStatusChangedAt = now,
                    CreatedAt = now,
                    UpdatedAt = now,
                });
            }
            db.Simulators.AddRange(simulators);
        }

        if (await db.Engineers.AnyAsync())
        {
            logger.LogInformation("[Seeder] maint.engineers already has data — skipping engineer seed.");
        }
        else
        {
            // 6 Engineers (1-to-1 with 6 simulators)
            var engineers = new List<Engineer>();
            for (var i = 0; i < 6; i++)
            {
                var planeType = AircraftTypes[i];
                engineers.Add(new Engineer
                {
                    EngineerID = EngineerIds[i],
                    EmployeeCode = $"LGA-ENG-{i + 1:000}",
                    FullName = $"Engineer {i + 1}",
                    ClearanceLevel = "L3",
                    HardwareRatings = [planeType],
                    ShiftStartTime = now.Date.AddHours(6),
                    ShiftEndTime = now.Date.AddHours(22),
                    IsOnCall = false,
                    CreatedAt = now,
                    UpdatedAt = now,
                });
            }
            db.Engineers.AddRange(engineers);
        }

        await db.SaveChangesAsync();
        logger.LogInformation("[Seeder] maint schema checked (simulators/engineers).");
    }
}