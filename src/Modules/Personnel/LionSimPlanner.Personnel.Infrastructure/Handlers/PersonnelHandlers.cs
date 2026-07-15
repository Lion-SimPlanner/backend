using LionSimPlanner.Personnel.Infrastructure.CmsSync;
using LionSimPlanner.Shared.Dtos;
using LionSimPlanner.Shared.Queries;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace LionSimPlanner.Personnel.Infrastructure.Handlers;

/// <summary>
/// Handles GetPriorityQueueQuery from the Scheduling module.
/// Lives in Personnel.Infrastructure (needs PersonnelDbContext).
/// Returns only PilotPriorityDto — the Personnel domain model never crosses module boundaries.
/// </summary>
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

        if (!string.IsNullOrWhiteSpace(request.TypeRatingFilter))
            query = query.Where(p =>
                p.TypeRatings.Contains(request.TypeRatingFilter));

        var pilots = await query
            .OrderBy(p => p.NextTrainingDue)
            .Select(p => new PilotPriorityDto(
                p.PilotId,
                p.EmployeeCode,
                p.FullName,
                p.Rank.ToString(),
                p.NextTrainingDue,
                p.RequiredSyllabus,
                p.TypeRatings,
                p.MedicalExpiry,
                p.LastDutyEndTime,
                p.NextDutyStartTime))
            .ToListAsync(cancellationToken);

        return pilots.AsReadOnly();
    }
}

/// <summary>
/// Handles GetInstructorByIdQuery from the Scheduling module (defined in Shared).
/// Resolves in Personnel.Infrastructure — MediatR routes the query here at runtime.
/// No Scheduling → Personnel project reference exists; isolation is enforced.
/// </summary>
public sealed class GetInstructorByIdHandler(PersonnelDbContext db)
    : IRequestHandler<GetInstructorByIdQuery, InstructorValidationData?>
{
    public async Task<InstructorValidationData?> Handle(
        GetInstructorByIdQuery request,
        CancellationToken cancellationToken)
    {
        var i = await db.Instructors
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.InstructorId == request.InstructorId, cancellationToken);

        if (i is null) return null;

        return new InstructorValidationData(
            i.InstructorId,
            i.EmployeeCode,
            i.FullName,
            i.CertifiedTypes.AsReadOnly(),
            i.AuthorizedSyllabi.AsReadOnly(),
            i.LicenseExpiry,
            i.LastDutyEndTime,
            i.CurrentMonthlyHours,
            i.MaxMonthlyHours);
    }
}

/// <summary>
/// Handles TrainingRecordCompletedNotification published by Scheduling.
/// POSTs the training record to the external CMS — lifecycle step 6.
/// </summary>
public sealed class HandleTrainingRecordCompletedHandler(
    CmsApiClient cmsClient,
    Microsoft.Extensions.Logging.ILogger<HandleTrainingRecordCompletedHandler> logger)
    : INotificationHandler<LionSimPlanner.Shared.Events.TrainingRecordCompletedNotification>
{
    public async Task Handle(
        LionSimPlanner.Shared.Events.TrainingRecordCompletedNotification notification,
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

        await cmsClient.PostTrainingRecordAsync(payload, cancellationToken);
    }
}
