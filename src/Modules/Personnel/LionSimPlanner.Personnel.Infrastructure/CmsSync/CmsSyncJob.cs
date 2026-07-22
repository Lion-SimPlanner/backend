using LionSimPlanner.Personnel.Domain.Entities;
using LionSimPlanner.Personnel.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Quartz;

namespace LionSimPlanner.Personnel.Infrastructure.CmsSync;

/// <summary>
/// Quartz.NET job that runs at midnight daily to synchronize pilot and instructor
/// rosters from the external CMS into the local hr schema cache.
///
/// This is how Lion SimPlanner stays current without becoming a system of record
/// for personnel data — it always defers to the CMS as the authoritative source.
///
/// CRON: "0 0 0 * * ?" = every day at 00:00:00 server time.
/// </summary>
[DisallowConcurrentExecution]
public sealed class CmsSyncJob(
    CmsApiClient cmsClient,
    PersonnelDbContext db,
    IConfiguration config,
    ILogger<CmsSyncJob> logger) : IJob
{
    public async Task Execute(IJobExecutionContext context)
    {
        var ct = context.CancellationToken;
        logger.LogInformation("[CmsSyncJob] Starting daily roster sync at {Time}", DateTime.UtcNow);

        try
        {
            await SyncPilotsAsync(ct);
            await SyncInstructorsAsync(ct);
            logger.LogInformation("[CmsSyncJob] Daily roster sync completed successfully.");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "[CmsSyncJob] Daily roster sync failed.");
            throw new JobExecutionException(ex, refireImmediately: false);
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    private async Task SyncPilotsAsync(CancellationToken ct)
    {
        var cmsPilots = await cmsClient.GetPilotsAsync(ct);
        var nextTrainingDueDays = config.GetValue<int>("TrainingSync:NextTrainingDueDays", 180);

        logger.LogInformation("[CmsSyncJob] Syncing {Count} pilots from CMS.", cmsPilots.Count);

        foreach (var cmsRecord in cmsPilots)
        {
            var existing = await db.Pilots
                .FirstOrDefaultAsync(p => p.EmployeeCode == cmsRecord.EmployeeCode, ct);

            if (existing is null)
            {
                // New pilot — insert
                var pilot = MapCmsToPilot(cmsRecord, nextTrainingDueDays);
                db.Pilots.Add(pilot);
                logger.LogDebug("[CmsSyncJob] Inserting new pilot: {Code}", cmsRecord.EmployeeCode);
            }
            else
            {
                // Existing pilot — update CMS-owned fields only
                UpdatePilotFromCms(existing, cmsRecord, nextTrainingDueDays);
                logger.LogDebug("[CmsSyncJob] Updated pilot: {Code}", cmsRecord.EmployeeCode);
            }
        }

        await db.SaveChangesAsync(ct);
        logger.LogInformation("[CmsSyncJob] Pilot sync saved.");
    }

    private async Task SyncInstructorsAsync(CancellationToken ct)
    {
        var cmsInstructors = await cmsClient.GetInstructorsAsync(ct);
        logger.LogInformation("[CmsSyncJob] Syncing {Count} instructors from CMS.", cmsInstructors.Count);

        foreach (var cmsRecord in cmsInstructors)
        {
            var existing = await db.Instructors
                .FirstOrDefaultAsync(i => i.EmployeeCode == cmsRecord.EmployeeCode, ct);

            if (existing is null)
            {
                db.Instructors.Add(MapCmsToInstructor(cmsRecord));
                logger.LogDebug("[CmsSyncJob] Inserting new instructor: {Code}", cmsRecord.EmployeeCode);
            }
            else
            {
                UpdateInstructorFromCms(existing, cmsRecord);
                logger.LogDebug("[CmsSyncJob] Updated instructor: {Code}", cmsRecord.EmployeeCode);
            }
        }

        await db.SaveChangesAsync(ct);
        logger.LogInformation("[CmsSyncJob] Instructor sync saved.");
    }

    // ── Mapping helpers ───────────────────────────────────────────────────────

    private static Pilot MapCmsToPilot(CmsPilotRecord r, int nextDueDays)
    {
        var lastTraining = r.LastTrainingDate ?? DateTime.UtcNow.AddDays(-nextDueDays + 1);
        return new Pilot
        {
            PilotId            = Guid.NewGuid(),
            EmployeeCode       = r.EmployeeCode,
            FullName           = r.FullName,
            CorporateEmail     = r.CorporateEmail,
            IsExternalUser     = false,
            Rank               = Enum.Parse<PilotRank>(r.Rank, ignoreCase: true),
            TypeRatings        = r.TypeRatings,
            MedicalExpiry      = r.MedicalExpiry,
            LastTrainingDate   = lastTraining,
            NextTrainingDue    = lastTraining.AddDays(nextDueDays),
            RequiredSyllabus   = r.RequiredSyllabus,
            FtlStatus          = "CLEAR",
            LastDutyEndTime    = r.LastDutyEndTime,
            NextDutyStartTime  = r.NextDutyStartTime,
            CreatedAt          = DateTime.UtcNow,
            UpdatedAt          = DateTime.UtcNow
        };
    }

    private static void UpdatePilotFromCms(Pilot existing, CmsPilotRecord r, int nextDueDays)
    {
        var lastTraining = r.LastTrainingDate ?? existing.LastTrainingDate;
        existing.FullName          = r.FullName;
        existing.CorporateEmail    = r.CorporateEmail ?? existing.CorporateEmail;
        existing.IsExternalUser    = false;
        existing.Rank              = Enum.Parse<PilotRank>(r.Rank, ignoreCase: true);
        existing.TypeRatings       = r.TypeRatings;
        existing.MedicalExpiry     = r.MedicalExpiry;
        existing.LastTrainingDate  = lastTraining;
        existing.NextTrainingDue   = lastTraining.AddDays(nextDueDays);
        existing.RequiredSyllabus  = r.RequiredSyllabus ?? existing.RequiredSyllabus;
        existing.FtlStatus         = "CLEAR";
        existing.LastDutyEndTime   = r.LastDutyEndTime;
        existing.NextDutyStartTime = r.NextDutyStartTime;
        existing.UpdatedAt         = DateTime.UtcNow;
    }

    private static Instructor MapCmsToInstructor(CmsInstructorRecord r)
    {
        return new Instructor
        {
            InstructorId         = Guid.NewGuid(),
            EmployeeCode         = r.EmployeeCode,
            FullName             = r.FullName,
            CorporateEmail       = r.CorporateEmail ?? string.Empty,
            RoleLevel            = Enum.Parse<InstructorRoleLevel>(r.RoleLevel, ignoreCase: true),
            CertifiedTypes       = r.CertifiedTypes,
            AuthorizedSyllabi    = r.AuthorizedSyllabi,
            LicenseExpiry        = r.LicenseExpiry,
            LastDutyEndTime      = r.LastDutyEndTime,
            NextDutyStartTime    = r.NextDutyStartTime,
            CurrentMonthlyHours  = r.CurrentMonthlyHours,
            MaxMonthlyHours      = r.MaxMonthlyHours,
            CreatedAt            = DateTime.UtcNow,
            UpdatedAt            = DateTime.UtcNow
        };
    }

    private static void UpdateInstructorFromCms(Instructor existing, CmsInstructorRecord r)
    {
        existing.FullName            = r.FullName;
        existing.CorporateEmail      = r.CorporateEmail ?? existing.CorporateEmail;
        existing.RoleLevel           = Enum.Parse<InstructorRoleLevel>(r.RoleLevel, ignoreCase: true);
        existing.CertifiedTypes      = r.CertifiedTypes;
        existing.AuthorizedSyllabi   = r.AuthorizedSyllabi;
        existing.LicenseExpiry       = r.LicenseExpiry;
        existing.LastDutyEndTime     = r.LastDutyEndTime;
        existing.NextDutyStartTime   = r.NextDutyStartTime;
        existing.CurrentMonthlyHours = r.CurrentMonthlyHours;
        existing.MaxMonthlyHours     = r.MaxMonthlyHours;
        existing.UpdatedAt           = DateTime.UtcNow;
    }
}
