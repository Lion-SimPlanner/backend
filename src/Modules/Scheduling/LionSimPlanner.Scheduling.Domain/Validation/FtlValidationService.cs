using LionSimPlanner.Scheduling.Domain.Entities;
using LionSimPlanner.Shared.Dtos;

namespace LionSimPlanner.Scheduling.Domain.Validation;

public sealed class FtlValidationService
{
    private readonly TimeSpan _minRestPeriod;

    public FtlValidationService(double minRestHours = 10.0)
    {
        _minRestPeriod = TimeSpan.FromHours(minRestHours);
    }

    public FtlValidationResult Validate(
        SimulatorSession session,
        PilotPriorityDto captain,
        PilotPriorityDto? firstOfficer,
        InstructorValidationData instructor,
        bool skipCaptainRegulatoryChecks,
        bool skipFirstOfficerRegulatoryChecks)
    {
        var result = new FtlValidationResult();

        if (!skipCaptainRegulatoryChecks)
        {
            var captainRest = session.StartTime - captain.LastDutyEndTime;
            if (captainRest < _minRestPeriod)
                result.AddViolation(
                    $"FTL Rest Violation — Captain {captain.FullName} ({captain.EmployeeCode}): " +
                    $"Only {captainRest.TotalHours:F1}h rest since last duty end " +
                    $"({captain.LastDutyEndTime:yyyy-MM-dd HH:mm} UTC). " +
                    $"Minimum required: {_minRestPeriod.TotalHours:F0}h. " +
                    $"Earliest eligible start: {captain.LastDutyEndTime.Add(_minRestPeriod):yyyy-MM-dd HH:mm} UTC.");

            if (captain.MedicalExpiry < session.StartTime)
                result.AddViolation(
                    $"Medical Certificate Expired — Captain {captain.FullName} ({captain.EmployeeCode}): " +
                    $"Medical expired {captain.MedicalExpiry:yyyy-MM-dd}. Renewal required before assignment.");
        }

        if (firstOfficer is not null)
        {
            if (!skipFirstOfficerRegulatoryChecks)
            {
                var foRest = session.StartTime - firstOfficer.LastDutyEndTime;
                if (foRest < _minRestPeriod)
                    result.AddViolation(
                        $"FTL Rest Violation — First Officer {firstOfficer.FullName} ({firstOfficer.EmployeeCode}): " +
                        $"Only {foRest.TotalHours:F1}h rest since last duty end " +
                        $"({firstOfficer.LastDutyEndTime:yyyy-MM-dd HH:mm} UTC). " +
                        $"Minimum required: {_minRestPeriod.TotalHours:F0}h. " +
                        $"Earliest eligible start: {firstOfficer.LastDutyEndTime.Add(_minRestPeriod):yyyy-MM-dd HH:mm} UTC.");

                if (firstOfficer.MedicalExpiry < session.StartTime)
                    result.AddViolation(
                        $"Medical Certificate Expired — First Officer {firstOfficer.FullName} " +
                        $"({firstOfficer.EmployeeCode}): Medical expired {firstOfficer.MedicalExpiry:yyyy-MM-dd}.");
            }
        }

        var instrRest = session.StartTime - instructor.LastDutyEndTime;
        if (instrRest < _minRestPeriod)
            result.AddViolation(
                $"FTL Rest Violation — Instructor {instructor.FullName} ({instructor.EmployeeCode}): " +
                $"Only {instrRest.TotalHours:F1}h rest since last duty end " +
                $"({instructor.LastDutyEndTime:yyyy-MM-dd HH:mm} UTC). " +
                $"Minimum required: {_minRestPeriod.TotalHours:F0}h.");

        var sessionH       = (int)Math.Ceiling(session.DurationHours);
        var projectedHours = instructor.CurrentMonthlyHours + sessionH;
        if (projectedHours > instructor.MaxMonthlyHours)
            result.AddViolation(
                $"Instructor Monthly Hours Cap Exceeded — {instructor.FullName} ({instructor.EmployeeCode}): " +
                $"Current this month: {instructor.CurrentMonthlyHours}h + session {sessionH}h = {projectedHours}h " +
                $"exceeds cap of {instructor.MaxMonthlyHours}h. " +
                $"Remaining capacity: {instructor.MaxMonthlyHours - instructor.CurrentMonthlyHours}h.");

        var isExternalSession = string.Equals(session.SyllabusId, "External", StringComparison.OrdinalIgnoreCase)
            || skipCaptainRegulatoryChecks
            || (firstOfficer is not null && skipFirstOfficerRegulatoryChecks)
            || captain.IsExternalUser
            || (firstOfficer?.IsExternalUser ?? false);

        if (!isExternalSession)
        {
            var parts = session.SyllabusId.Split('_');
            var syllabusPrefix = parts[0];
            var baseSyllabus = parts.Length > 1 ? parts[^1] : session.SyllabusId;

            var hasTypeCert = instructor.CertifiedTypes.Any(t =>
                string.Equals(t, syllabusPrefix, StringComparison.OrdinalIgnoreCase));

            if (!hasTypeCert)
            {
                result.AddViolation(
                    $"Type Certification Mismatch — Instructor {instructor.FullName} ({instructor.EmployeeCode}): " +
                    $"Not certified on aircraft type '{syllabusPrefix}' (from syllabus '{session.SyllabusId}'). " +
                    $"Holds certifications for: {string.Join(", ", instructor.CertifiedTypes)}.");
            }

            var hasSyllabusAuth = instructor.AuthorizedSyllabi.Any(s =>
                string.Equals(s, session.SyllabusId, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(s, baseSyllabus, StringComparison.OrdinalIgnoreCase));

            if (!hasSyllabusAuth)
            {
                result.AddViolation(
                    $"Syllabus Authorization Missing — Instructor {instructor.FullName} ({instructor.EmployeeCode}): " +
                    $"Not authorized for syllabus '{session.SyllabusId}'. " +
                    $"Authorized syllabi: {string.Join(", ", instructor.AuthorizedSyllabi)}.");
            }
        }

        if (instructor.LicenseExpiry < session.StartTime)
            result.AddViolation(
                $"Instructor License Expired — {instructor.FullName} ({instructor.EmployeeCode}): " +
                $"License expired {instructor.LicenseExpiry:yyyy-MM-dd}. " +
                $"Must be valid on session date {session.StartTime:yyyy-MM-dd}.");

        return result;
    }
}
