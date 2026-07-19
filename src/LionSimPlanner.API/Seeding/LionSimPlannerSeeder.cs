using LionSimPlanner.Asset.Domain.Entities;
using LionSimPlanner.Asset.Infrastructure;
using LionSimPlanner.Personnel.Domain.Entities;
using LionSimPlanner.Personnel.Domain.Enums;
using LionSimPlanner.Personnel.Infrastructure;
using LionSimPlanner.Scheduling.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Bogus;
using Microsoft.Extensions.Logging;

namespace LionSimPlanner.API.Seeding;

public static class LionSimPlannerSeeder
{
    private static readonly string[] Fleet =
    [
        "B737-800NG", "B737-900ER", "B737 MAX 8",
        "A320-200", "A320neo",
        "A330-300", "A330-900neo",
        "ATR 72-500", "ATR 72-600"
    ];

    private static readonly Guid[] SimulatorIds =
    [
        new("a1a1a1a1-0001-0001-0001-000000000001"),
        new("a1a1a1a1-0001-0001-0001-000000000002"),
        new("a1a1a1a1-0001-0001-0001-000000000003"),
        new("a1a1a1a1-0001-0001-0001-000000000004"),
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
        new("b2b2b2b2-0002-0002-0002-00000000000a"),
    ];

    private static readonly Guid[] InstructorIds =
    [
        new("c3c3c3c3-0003-0003-0003-000000000001"),
        new("c3c3c3c3-0003-0003-0003-000000000002"),
        new("c3c3c3c3-0003-0003-0003-000000000003"),
    ];

    private static readonly Guid[] EngineerIds =
    [
        new("d4d4d4d4-0004-0004-0004-000000000001"),
        new("d4d4d4d4-0004-0004-0004-000000000002"),
        new("d4d4d4d4-0004-0004-0004-000000000003"),
    ];

    public static async Task SeedAsync(
        PersonnelDbContext hr,
        AssetDbContext maint,
        SchedulingDbContext sched,
        ILogger logger)
    {
        await EnforceOperationalTablesEmptyAsync(maint, sched, logger);
        await SeedPersonnelAsync(hr, logger);
        await SeedAssetsAsync(maint, logger);
    }

    private static async Task EnforceOperationalTablesEmptyAsync(AssetDbContext maint, SchedulingDbContext sched, ILogger logger)
    {
        await sched.Database.ExecuteSqlRawAsync("DELETE FROM sched.simulator_sessions");
        await maint.Database.ExecuteSqlRawAsync("DELETE FROM maint.maintenance_checklists");
        logger.LogInformation("[Seeder] operational tables cleared: sched.simulator_sessions, maint.maintenance_checklists.");
    }

    private static async Task SeedPersonnelAsync(PersonnelDbContext db, ILogger logger)
    {
        var hasPilots = await db.Pilots.AnyAsync();
        var hasInstructors = await db.Instructors.AnyAsync();
        if (hasPilots && hasInstructors) return;

        Randomizer.Seed = new Random(12345);
        var faker = new Faker("en");
        var now = DateTime.UtcNow;
        if (!hasPilots)
        {
            var pilots = new List<Pilot>();
            for (var i = 0; i < 10; i++)
            {
                var isCapt = i < 6;
                var rank = isCapt ? PilotRank.Captain : PilotRank.FirstOfficer;
                var prefix = isCapt ? "Capt." : "F/O";
                var fn = faker.Name.FirstName();
                var ln = faker.Name.LastName();
                pilots.Add(new Pilot
                {
                    PilotId = PilotIds[i],
                    EmployeeCode = $"LGA-PLT-{i + 1:000}",
                    FullName = $"{prefix} {fn} {ln}",
                    CorporateEmail = $"pilot{i + 1}@lionair.co.id",
                    Rank = rank,
                    TypeRatings = Fleet.Take(isCapt ? 2 : 1).ToList(),
                    MedicalExpiry = now.AddMonths(12),
                    LastTrainingDate = now.AddDays(-30),
                    NextTrainingDue = now.AddDays(90),
                    RequiredSyllabus = "InitialTypeRating",
                    LastDutyEndTime = now.AddHours(-24),
                    NextDutyStartTime = now.AddHours(12),
                    CreatedAt = now,
                    UpdatedAt = now,
                });
            }
            db.Pilots.AddRange(pilots);
        }

        if (!hasInstructors)
        {
            var instructors = new List<Instructor>();
            for (var i = 0; i < 3; i++)
            {
                var fn = faker.Name.FirstName();
                var ln = faker.Name.LastName();
                instructors.Add(new Instructor
                {
                    InstructorId = InstructorIds[i],
                    EmployeeCode = $"LGA-INS-{i + 1:000}",
                    FullName = $"Instr. {fn} {ln}",
                    CorporateEmail = $"instructor{i + 1}@lionair.co.id",
                    RoleLevel = InstructorRoleLevel.TRI,
                    CertifiedTypes = Fleet.Take(3).ToList(),
                    AuthorizedSyllabi = new List<string> { "InitialTypeRating", "RecurrentTraining" },
                    LicenseExpiry = now.AddMonths(24),
                    LastDutyEndTime = now.AddHours(-12),
                    NextDutyStartTime = now.AddHours(8),
                    CurrentMonthlyHours = 10,
                    MaxMonthlyHours = 100,
                    CreatedAt = now,
                    UpdatedAt = now,
                });
            }
            db.Instructors.AddRange(instructors);
        }

        await db.SaveChangesAsync();
        logger.LogInformation("[Seeder] hr schema: pilots seeded={PilotsSeeded}, instructors seeded={InstructorsSeeded}.", !hasPilots, !hasInstructors);
    }

    private static async Task SeedAssetsAsync(AssetDbContext db, ILogger logger)
    {
        var hasSimulators = await db.Simulators.AnyAsync();
        var hasEngineers = await db.Engineers.AnyAsync();
        if (hasSimulators && hasEngineers) return;

        var now = DateTime.UtcNow;

        if (!hasSimulators)
        {
            var simulators = new[]
            {
                new Simulator
                {
                    SimulatorId = SimulatorIds[0],
                    Name = "Jakarta B737-800NG Full Flight Simulator",
                    BayNumber = "Bay 1",
                    AircraftType = "B737-800NG",
                    Status = "Ready",
                    LastStatusChangedAt = now,
                    CreatedAt = now,
                    UpdatedAt = now,
                },
                new Simulator
                {
                    SimulatorId = SimulatorIds[1],
                    Name = "Jakarta A330-900neo Full Flight Simulator",
                    BayNumber = "Bay 2",
                    AircraftType = "A330-900neo",
                    Status = "Ready",
                    LastStatusChangedAt = now,
                    CreatedAt = now,
                    UpdatedAt = now,
                },
                new Simulator
                {
                    SimulatorId = SimulatorIds[2],
                    Name = "Jakarta A320neo Full Flight Simulator",
                    BayNumber = "Bay 3",
                    AircraftType = "A320neo",
                    Status = "Ready",
                    LastStatusChangedAt = now,
                    CreatedAt = now,
                    UpdatedAt = now,
                },
                new Simulator
                {
                    SimulatorId = SimulatorIds[3],
                    Name = "Jakarta B737 MAX 8 Full Flight Simulator",
                    BayNumber = "Bay 4",
                    AircraftType = "B737 MAX 8",
                    Status = "Down",
                    LastStatusChangedAt = now,
                    CreatedAt = now,
                    UpdatedAt = now,
                },
            };
            db.Simulators.AddRange(simulators);
        }

        if (!hasEngineers)
        {
            var engineers = new[]
            {
                new LionSimPlanner.Asset.Domain.Entities.Engineer
                {
                    EngineerID = new Guid("d4d4d4d4-0004-0004-0004-000000000001"),
                    EmployeeCode = "LGA-ENG-001",
                    FullName = "Eng. Marek Kowalski",
                    ClearanceLevel = "L3",
                    HardwareRatings = new List<string> { "B737-800NG", "B737-900ER", "B737 MAX 8" },
                    ShiftStartTime = now.Date.AddHours(6),
                    ShiftEndTime = now.Date.AddHours(14),
                    IsOnCall = false,
                    CreatedAt = now,
                    UpdatedAt = now,
                },
                new LionSimPlanner.Asset.Domain.Entities.Engineer
                {
                    EngineerID = new Guid("d4d4d4d4-0004-0004-0004-000000000002"),
                    EmployeeCode = "LGA-ENG-002",
                    FullName = "Eng. Felix Adisa",
                    ClearanceLevel = "L2",
                    HardwareRatings = new List<string> { "A330-300", "A330-900neo" },
                    ShiftStartTime = now.Date.AddHours(6),
                    ShiftEndTime = now.Date.AddHours(14),
                    IsOnCall = false,
                    CreatedAt = now,
                    UpdatedAt = now,
                },
                new LionSimPlanner.Asset.Domain.Entities.Engineer
                {
                    EngineerID = new Guid("d4d4d4d4-0004-0004-0004-000000000003"),
                    EmployeeCode = "LGA-ENG-003",
                    FullName = "Eng. Thomas Brennan",
                    ClearanceLevel = "L3",
                    HardwareRatings = new List<string> { "A320-200", "A320neo" },
                    ShiftStartTime = now.Date.AddHours(14),
                    ShiftEndTime = now.Date.AddHours(22),
                    IsOnCall = false,
                    CreatedAt = now,
                    UpdatedAt = now,
                },
            };

            db.Engineers.AddRange(engineers);
        }

        await db.SaveChangesAsync();
        logger.LogInformation("[Seeder] maint schema: simulators seeded={SimulatorsSeeded}, engineers seeded={EngineersSeeded}.", !hasSimulators, !hasEngineers);
    }
}