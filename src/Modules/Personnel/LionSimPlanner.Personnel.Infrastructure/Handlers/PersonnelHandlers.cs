using LionSimPlanner.Personnel.Infrastructure.CmsSync;
using LionSimPlanner.Shared.Dtos;
using LionSimPlanner.Shared.Events;
using LionSimPlanner.Shared.Queries;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace LionSimPlanner.Personnel.Infrastructure.Handlers;

public sealed class GetPriorityQueueHandler(PersonnelDbContext db)
    : IRequestHandler<GetPriorityQueueQuery, IReadOnlyList<PilotPriorityDto>>
{
    public async Task<IReadOnlyList<PilotPriorityDto>> Handle(
        GetPriorityQueueQuery request,
        CancellationToken cancellationToken)
    {
        var query = db.Pilots.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(request.SyllabusFilter))
            query = query.Where(p => p.RequiredSyllabus == request.SyllabusFilter);

        var pilots = await query
            .OrderBy(p => p.NextTrainingDue)
            .ToListAsync(cancellationToken);

        if (!string.IsNullOrWhiteSpace(request.TypeRatingFilter))
            pilots = pilots
                .Where(p => p.TypeRatings is not null && p.TypeRatings.Contains(request.TypeRatingFilter))
                .ToList();

        return pilots
            .Select(p => new PilotPriorityDto(
                p.PilotId,
                p.EmployeeCode,
                p.FullName,
                p.Rank.ToString(),
                p.IsExternalUser,
                p.NextTrainingDue,
                p.RequiredSyllabus,
                (p.TypeRatings ?? []).AsReadOnly(),
                p.MedicalExpiry,
                p.LastDutyEndTime,
                p.NextDutyStartTime,
                p.CorporateEmail))
            .ToList()
            .AsReadOnly();
    }
}

public sealed class GetInstructorByIdHandler(PersonnelDbContext db)
    : IRequestHandler<GetInstructorByIdQuery, InstructorValidationData?>
{
    public async Task<InstructorValidationData?> Handle(
        GetInstructorByIdQuery request,
        CancellationToken cancellationToken)
    {
        var instructor = await db.Instructors
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.InstructorId == request.InstructorId, cancellationToken);

        if (instructor is null) return null;

        return new InstructorValidationData(
            instructor.InstructorId,
            instructor.EmployeeCode,
            instructor.FullName,
            instructor.CertifiedTypes.AsReadOnly(),
            instructor.AuthorizedSyllabi.AsReadOnly(),
            instructor.LicenseExpiry,
            instructor.LastDutyEndTime,
            instructor.CurrentMonthlyHours,
            instructor.MaxMonthlyHours);
    }
}

public sealed class HandleTrainingRecordCompletedHandler(
    CmsApiClient cmsClient,
    ILogger<HandleTrainingRecordCompletedHandler> logger)
    : INotificationHandler<TrainingRecordCompletedNotification>
{
    public async Task Handle(
        TrainingRecordCompletedNotification notification,
        CancellationToken cancellationToken)
    {
        logger.LogInformation(
            "[CMS Sync] Received TrainingRecordCompleted for session {SessionId}.",
            notification.SessionId);

        var payload = new CmsTrainingRecordPayload
        {
            SessionId       = notification.SessionId.ToString(),
            EmployeeCode    = notification.EmployeeCode,
            SyllabusId      = notification.SyllabusId,
            IsGraded        = notification.IsGraded,
            GradeStatus     = notification.GradeStatus,
            CompletionDate  = notification.CompletionDate,
            InstructorNotes = notification.InstructorNotes
        };

        try
        {
            await cmsClient.PostTrainingRecordAsync(payload, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "[CMS Sync] Failed to post training record to external CMS for session {SessionId}. Local grading completed successfully.", notification.SessionId);
        }
    }
}
