using LionSimPlanner.Asset.Domain.Entities;
using LionSimPlanner.Asset.Domain.Enums;
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
                var pilotDutyMode = faker.Random.Int(0, 99);
                var pilotDutyEnd = pilotDutyMode switch
                {
                    <= 34 => now.AddHours(-faker.Random.Double(12, 36)),
                    <= 74 => now.AddHours(-faker.Random.Double(8, 12)),
                    _ => now.AddHours(-faker.Random.Double(0.25, 7.5)),
                };
                var pilotNextDuty = pilotDutyEnd.AddHours(faker.Random.Double(2, 18));
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
                    LastDutyEndTime = pilotDutyEnd,
                    NextDutyStartTime = pilotNextDuty,
                    CreatedAt = now,
                    UpdatedAt = now,
                });
            }
            db.Pilots.AddRange(pilots);
        }

        if (!hasInstructors)
        {
            var instructors = new List<Instructor>();
            var syllabusCatalog = new[]
            {
                "InitialTypeRating",
                "RecurrentTraining",
                "LOFT",
                "CommandUpgrade",
                "OPC",
                "LPC",
                "MCC",
                "CRM",
                "Requalification",
                "Differences"
            };
            var usedInstructorLoadouts = new HashSet<string>(StringComparer.Ordinal);

            for (var i = 0; i < 3; i++)
            {
                var fn = faker.Name.FirstName();
                var ln = faker.Name.LastName();
                List<string> certifiedTypes;
                List<string> authorizedSyllabi;
                string signature;

                do
                {
                    var typeCount = faker.Random.Int(2, Math.Min(6, Fleet.Length));
                    var syllabusCount = faker.Random.Int(2, Math.Min(5, syllabusCatalog.Length));

                    certifiedTypes = faker.Random.Shuffle(Fleet.ToList())
                        .Take(typeCount)
                        .OrderBy(x => x, StringComparer.Ordinal)
                        .ToList();

                    authorizedSyllabi = faker.Random.Shuffle(syllabusCatalog.ToList())
                        .Take(syllabusCount)
                        .OrderBy(x => x, StringComparer.Ordinal)
                        .ToList();

                    signature = string.Join('|', certifiedTypes) + "::" + string.Join('|', authorizedSyllabi);
                }
                while (!usedInstructorLoadouts.Add(signature));

                var instructorDutyMode = faker.Random.Int(0, 99);
                var instructorDutyEnd = instructorDutyMode switch
                {
                    <= 29 => now.AddHours(-faker.Random.Double(12, 30)),
                    <= 69 => now.AddHours(-faker.Random.Double(8, 12)),
                    _ => now.AddHours(-faker.Random.Double(0.25, 7.0)),
                };
                var instructorNextDuty = instructorDutyEnd.AddHours(faker.Random.Double(1.5, 14));
                instructors.Add(new Instructor
                {
                    InstructorId = InstructorIds[i],
                    EmployeeCode = $"LGA-INS-{i + 1:000}",
                    FullName = $"Instr. {fn} {ln}",
                    CorporateEmail = $"instructor{i + 1}@lionair.co.id",
                    RoleLevel = faker.PickRandom(InstructorRoleLevel.SFI, InstructorRoleLevel.TRI, InstructorRoleLevel.TRE),
                    CertifiedTypes = certifiedTypes,
                    AuthorizedSyllabi = authorizedSyllabi,
                    LicenseExpiry = now.AddMonths(24),
                    LastDutyEndTime = instructorDutyEnd,
                    NextDutyStartTime = instructorNextDuty,
                    CurrentMonthlyHours = faker.Random.Int(8, 92),
                    MaxMonthlyHours = 24,
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
                    Status = SimulatorStatus.Ready,
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
                    Status = SimulatorStatus.Ready,
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
                    Status = SimulatorStatus.Ready,
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
                    Status = SimulatorStatus.AOG,
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