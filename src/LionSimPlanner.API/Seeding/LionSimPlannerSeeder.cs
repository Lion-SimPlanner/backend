using LionSimPlanner.Asset.Domain.Entities;
using LionSimPlanner.Asset.Infrastructure;
using LionSimPlanner.Personnel.Domain.Entities;
using LionSimPlanner.Personnel.Domain.Enums;
using LionSimPlanner.Personnel.Infrastructure;
using LionSimPlanner.Scheduling.Domain.Entities;
using LionSimPlanner.Scheduling.Domain.Enums;
using LionSimPlanner.Scheduling.Infrastructure;
using Microsoft.EntityFrameworkCore;

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
        new("b2b2b2b2-0002-0002-0002-00000000000b"),
        new("b2b2b2b2-0002-0002-0002-00000000000c"),
        new("b2b2b2b2-0002-0002-0002-00000000000d"),
        new("b2b2b2b2-0002-0002-0002-00000000000e"),
        new("b2b2b2b2-0002-0002-0002-00000000000f"),
        new("b2b2b2b2-0002-0002-0002-000000000010"),
        new("b2b2b2b2-0002-0002-0002-000000000011"),
        new("b2b2b2b2-0002-0002-0002-000000000012"),
        new("b2b2b2b2-0002-0002-0002-000000000013"),
        new("b2b2b2b2-0002-0002-0002-000000000014"),
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
    ];

    public static async Task SeedAsync(
        PersonnelDbContext hr,
        AssetDbContext maint,
        SchedulingDbContext sched,
        ILogger logger)
    {
        await SeedPersonnelAsync(hr, logger);
        await SeedAssetsAsync(maint, logger);
        await SeedSessionsAsync(sched, logger);
    }

    private static async Task SeedPersonnelAsync(PersonnelDbContext db, ILogger logger)
    {
        if (await db.Pilots.AnyAsync()) return;

        var now = DateTime.UtcNow;
        var rng = new Random(42);

        var firstNames = new[]
        {
            "Arjun", "Budi", "Cahyo", "Dinda", "Eko",
            "Fajar", "Gilang", "Hendra", "Irfan", "Joko",
            "Kevin", "Lenny", "Mira", "Nadia", "Oscar",
            "Putra", "Rahmat", "Sari", "Taufik", "Usman"
        };

        var lastNames = new[]
        {
            "Santoso", "Wibowo", "Prasetyo", "Kurniawan", "Hidayat",
            "Nugroho", "Setiawan", "Rahayu", "Susanto", "Hartono",
            "Wijaya", "Saputra", "Halim", "Gunawan", "Lim",
            "Tanaka", "Ibrahim", "Hakim", "Saleh", "Putra"
        };

        var syllabi = new[]
        {
            "InitialTypeRating", "RecurrentTraining", "LineCheck",
            "ProficiencyCheck", "EmergencyAndAbnormal"
        };

        var pilots = new List<Pilot>();
        for (var i = 0; i < 20; i++)
        {
            var isCapt   = i < 12;
            var rank     = isCapt ? PilotRank.Captain : PilotRank.FirstOfficer;
            var prefix   = isCapt ? "Capt." : "F/O";
            var fn       = firstNames[i];
            var ln       = lastNames[(i + 3) % lastNames.Length];
            var ratings  = Fleet
                .OrderBy(_ => rng.Next())
                .Take(isCapt ? 2 : 1)
                .ToList();
            var dueDays  = rng.Next(-5, 90);

            pilots.Add(new Pilot
            {
                PilotId             = PilotIds[i],
                EmployeeCode        = $"LGA-PLT-{i + 1:000}",
                FullName            = $"{prefix} {fn} {ln}",
                CorporateEmail      = $"{fn.ToLowerInvariant()}.{ln.ToLowerInvariant()}@lionair.co.id",
                Rank                = rank,
                TypeRatings         = ratings,
                MedicalExpiry       = now.AddMonths(rng.Next(2, 24)),
                LastTrainingDate    = now.AddDays(-rng.Next(30, 180)),
                NextTrainingDue     = now.AddDays(dueDays),
                RequiredSyllabus    = syllabi[i % syllabi.Length],
                LastDutyEndTime     = now.AddHours(-rng.Next(12, 72)),
                NextDutyStartTime   = now.AddHours(rng.Next(10, 36)),
                CreatedAt           = now,
                UpdatedAt           = now,
            });
        }

        db.Pilots.AddRange(pilots);

        var instructorDefs = new[]
        {
            ("Isamu",  "Nakamura", InstructorRoleLevel.TRE,
                new[] { "B737-800NG", "B737-900ER", "B737 MAX 8", "A320-200", "A320neo" }),
            ("Sarah",  "Okonkwo",  InstructorRoleLevel.TRE,
                new[] { "A330-300", "A330-900neo", "A320-200", "A320neo" }),
            ("David",  "Reeves",   InstructorRoleLevel.TRI,
                new[] { "A320-200", "A320neo", "B737-800NG" }),
            ("Priya",  "Langley",  InstructorRoleLevel.TRI,
                new[] { "B737-800NG", "B737-900ER", "B737 MAX 8" }),
            ("Ahmad",  "Wirawan",  InstructorRoleLevel.SFI,
                new[] { "ATR 72-500", "ATR 72-600" }),
            ("Elena",  "Petrov",   InstructorRoleLevel.SFI,
                new[] { "A330-300", "A330-900neo" }),
        };

        var allSyllabi = new List<string>
        {
            "InitialTypeRating", "RecurrentTraining", "LineCheck",
            "ProficiencyCheck", "EmergencyAndAbnormal", "LowVisibilityOperations"
        };

        var instructors = instructorDefs.Select((def, idx) => new Instructor
        {
            InstructorId        = InstructorIds[idx],
            EmployeeCode        = $"LGA-INS-{idx + 1:000}",
            FullName            = $"Instr. {def.Item1} {def.Item2}",
            CorporateEmail      = $"{def.Item1.ToLowerInvariant()}.{def.Item2.ToLowerInvariant()}@lionair.co.id",
            RoleLevel           = def.Item3,
            CertifiedTypes      = def.Item4.ToList(),
            AuthorizedSyllabi   = allSyllabi,
            LicenseExpiry       = now.AddMonths(rng.Next(6, 36)),
            LastDutyEndTime     = now.AddHours(-rng.Next(8, 48)),
            NextDutyStartTime   = now.AddHours(rng.Next(2, 16)),
            CurrentMonthlyHours = rng.Next(0, 60),
            MaxMonthlyHours     = 100,
            CreatedAt           = now,
            UpdatedAt           = now,
        }).ToList();

        db.Instructors.AddRange(instructors);
        await db.SaveChangesAsync();

        logger.LogInformation("[Seeder] hr schema: {P} pilots, {I} instructors inserted.",
            pilots.Count, instructors.Count);
    }

    private static async Task SeedAssetsAsync(AssetDbContext db, ILogger logger)
    {
        if (await db.Simulators.AnyAsync()) return;

        var now = DateTime.UtcNow;

        var simulators = new[]
        {
            new Simulator
            {
                SimulatorId         = SimulatorIds[0],
                Name                = "Jakarta B737-800NG Full Flight Simulator",
                BayNumber           = "Bay 1",
                AircraftType        = "B737-800NG",
                Status              = "Ready",
                LastStatusChangedAt = now,
                CreatedAt           = now,
                UpdatedAt           = now,
            },
            new Simulator
            {
                SimulatorId         = SimulatorIds[1],
                Name                = "Jakarta A330-900neo Full Flight Simulator",
                BayNumber           = "Bay 2",
                AircraftType        = "A330-900neo",
                Status              = "Ready",
                LastStatusChangedAt = now,
                CreatedAt           = now,
                UpdatedAt           = now,
            },
            new Simulator
            {
                SimulatorId         = SimulatorIds[2],
                Name                = "Jakarta A320neo Full Flight Simulator",
                BayNumber           = "Bay 3",
                AircraftType        = "A320neo",
                Status              = "Ready",
                LastStatusChangedAt = now,
                CreatedAt           = now,
                UpdatedAt           = now,
            },
            new Simulator
            {
                SimulatorId         = SimulatorIds[3],
                Name                = "Jakarta B737 MAX 8 Full Flight Simulator",
                BayNumber           = "Bay 4",
                AircraftType        = "B737 MAX 8",
                Status              = "Down",
                LastStatusChangedAt = now,
                CreatedAt           = now,
                UpdatedAt           = now,
            },
        };

        db.Simulators.AddRange(simulators);

        if (!await db.Engineers.AnyAsync())
        {
            var engineers = new[]
            {
                new Engineer
                {
                    EngineerID      = EngineerIds[0],
                    EmployeeCode    = "LGA-ENG-001",
                    FullName        = "Eng. Marek Kowalski",
                    ClearanceLevel  = "L3",
                    HardwareRatings = new List<string> { "B737-800NG", "B737-900ER", "B737 MAX 8" },
                    ShiftStartTime  = now.Date.AddHours(6),
                    ShiftEndTime    = now.Date.AddHours(14),
                    IsOnCall        = false,
                    CreatedAt       = now,
                    UpdatedAt       = now,
                },
                new Engineer
                {
                    EngineerID      = EngineerIds[1],
                    EmployeeCode    = "LGA-ENG-002",
                    FullName        = "Eng. Felix Adisa",
                    ClearanceLevel  = "L2",
                    HardwareRatings = new List<string> { "A330-300", "A330-900neo" },
                    ShiftStartTime  = now.Date.AddHours(6),
                    ShiftEndTime    = now.Date.AddHours(14),
                    IsOnCall        = false,
                    CreatedAt       = now,
                    UpdatedAt       = now,
                },
                new Engineer
                {
                    EngineerID      = EngineerIds[2],
                    EmployeeCode    = "LGA-ENG-003",
                    FullName        = "Eng. Thomas Brennan",
                    ClearanceLevel  = "L3",
                    HardwareRatings = new List<string> { "A320-200", "A320neo" },
                    ShiftStartTime  = now.Date.AddHours(14),
                    ShiftEndTime    = now.Date.AddHours(22),
                    IsOnCall        = false,
                    CreatedAt       = now,
                    UpdatedAt       = now,
                },
                new Engineer
                {
                    EngineerID      = EngineerIds[3],
                    EmployeeCode    = "LGA-ENG-004",
                    FullName        = "Eng. Putri Ramadhani",
                    ClearanceLevel  = "L1",
                    HardwareRatings = new List<string> { "ATR 72-500", "ATR 72-600" },
                    ShiftStartTime  = now.Date.AddHours(14),
                    ShiftEndTime    = now.Date.AddHours(22),
                    IsOnCall        = true,
                    CreatedAt       = now,
                    UpdatedAt       = now,
                },
            };

            db.Engineers.AddRange(engineers);
        }

        await db.SaveChangesAsync();
        logger.LogInformation("[Seeder] maint schema: simulators and engineers inserted.");
    }

    private static async Task SeedSessionsAsync(SchedulingDbContext db, ILogger logger)
    {
        if (await db.Sessions.AnyAsync()) return;

        var now      = DateTime.UtcNow;
        var baseDate = now.Date;

        var sessions = new List<SimulatorSession>
        {
            MakeSession(SimulatorIds[0], PilotIds[0],  PilotIds[10], InstructorIds[2], EngineerIds[0],
                baseDate.AddDays(0).AddHours(7),  baseDate.AddDays(0).AddHours(11),
                SessionType.Training, SessionStatus.Scheduled, "InitialTypeRating",    "LGA-PLT-001"),

            MakeSession(SimulatorIds[1], PilotIds[1],  PilotIds[11], InstructorIds[0], EngineerIds[1],
                baseDate.AddDays(0).AddHours(13), baseDate.AddDays(0).AddHours(17),
                SessionType.Training, SessionStatus.Scheduled, "RecurrentTraining",    "LGA-PLT-002"),

            MakeSession(SimulatorIds[2], PilotIds[2],  PilotIds[12], InstructorIds[1], EngineerIds[2],
                baseDate.AddDays(1).AddHours(8),  baseDate.AddDays(1).AddHours(12),
                SessionType.CheckRide, SessionStatus.Scheduled, "ProficiencyCheck",    "LGA-PLT-003"),

            MakeSession(SimulatorIds[0], PilotIds[3],  PilotIds[13], InstructorIds[2], EngineerIds[0],
                baseDate.AddDays(1).AddHours(13), baseDate.AddDays(1).AddHours(17),
                SessionType.Training, SessionStatus.Scheduled, "LineCheck",            "LGA-PLT-004"),

            MakeSession(SimulatorIds[1], PilotIds[4],  PilotIds[14], InstructorIds[3], EngineerIds[1],
                baseDate.AddDays(2).AddHours(7),  baseDate.AddDays(2).AddHours(11),
                SessionType.Training, SessionStatus.Scheduled, "EmergencyAndAbnormal", "LGA-PLT-005"),

            MakeSession(SimulatorIds[2], PilotIds[5],  PilotIds[15], InstructorIds[4], EngineerIds[2],
                baseDate.AddDays(2).AddHours(13), baseDate.AddDays(2).AddHours(17),
                SessionType.Training, SessionStatus.Scheduled, "RecurrentTraining",    "LGA-PLT-006"),

            MakeSession(SimulatorIds[0], PilotIds[6],  PilotIds[16], InstructorIds[0], EngineerIds[3],
                baseDate.AddDays(3).AddHours(8),  baseDate.AddDays(3).AddHours(12),
                SessionType.Training, SessionStatus.Scheduled, "InitialTypeRating",    "LGA-PLT-007"),

            MakeSession(SimulatorIds[1], PilotIds[7],  PilotIds[17], InstructorIds[1], EngineerIds[0],
                baseDate.AddDays(3).AddHours(14), baseDate.AddDays(3).AddHours(18),
                SessionType.CheckRide, SessionStatus.Scheduled, "LineCheck",           "LGA-PLT-008"),

            MakeCompletedSession(SimulatorIds[2], PilotIds[8],  PilotIds[18], InstructorIds[2], EngineerIds[1],
                baseDate.AddDays(-2).AddHours(7),  baseDate.AddDays(-2).AddHours(11),
                "RecurrentTraining", "LGA-PLT-009", "PASSED"),

            MakeCompletedSession(SimulatorIds[0], PilotIds[9],  PilotIds[19], InstructorIds[3], EngineerIds[2],
                baseDate.AddDays(-2).AddHours(13), baseDate.AddDays(-2).AddHours(17),
                "ProficiencyCheck",  "LGA-PLT-010", "PASSED"),

            MakeCompletedSession(SimulatorIds[1], PilotIds[0],  PilotIds[10], InstructorIds[4], EngineerIds[3],
                baseDate.AddDays(-1).AddHours(8),  baseDate.AddDays(-1).AddHours(12),
                "LineCheck",         "LGA-PLT-001", "PASSED"),

            MakeCompletedSession(SimulatorIds[2], PilotIds[1],  PilotIds[11], InstructorIds[5], EngineerIds[0],
                baseDate.AddDays(-1).AddHours(14), baseDate.AddDays(-1).AddHours(18),
                "EmergencyAndAbnormal", "LGA-PLT-002", "FAILED"),
        };

        db.Sessions.AddRange(sessions);
        await db.SaveChangesAsync();

        logger.LogInformation("[Seeder] sched schema: {S} sessions inserted.", sessions.Count);
    }

    private static SimulatorSession MakeSession(
        Guid simId, Guid captainId, Guid foId, Guid instructorId, Guid engineerId,
        DateTime start, DateTime end,
        SessionType type, SessionStatus status, string syllabusId, string traineeCode)
    {
        var now = DateTime.UtcNow;
        return new SimulatorSession
        {
            SessionId           = Guid.NewGuid(),
            SimulatorId         = simId,
            CaptainId           = captainId,
            FirstOfficerId      = foId,
            InstructorId        = instructorId,
            EngineerId          = engineerId,
            StartTime           = start,
            EndTime             = end,
            SessionType         = type,
            Status              = status,
            SyllabusId          = syllabusId,
            TraineeEmployeeCode = traineeCode,
            IsGraded            = false,
            GradeStatus         = null,
            InstructorNotes     = string.Empty,
            CreatedAt           = now,
            UpdatedAt           = now,
        };
    }

    private static SimulatorSession MakeCompletedSession(
        Guid simId, Guid captainId, Guid foId, Guid instructorId, Guid engineerId,
        DateTime start, DateTime end,
        string syllabusId, string traineeCode, string gradeStatus)
    {
        var session = MakeSession(simId, captainId, foId, instructorId, engineerId,
            start, end, SessionType.Training, SessionStatus.Completed, syllabusId, traineeCode);
        session.IsGraded       = true;
        session.GradeStatus    = gradeStatus;
        session.InstructorNotes = "Session completed and graded.";
        return session;
    }
}
