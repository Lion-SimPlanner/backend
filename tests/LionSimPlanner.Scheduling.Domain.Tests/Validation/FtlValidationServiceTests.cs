using LionSimPlanner.Scheduling.Domain.Entities;
using LionSimPlanner.Scheduling.Domain.Enums;
using LionSimPlanner.Scheduling.Domain.Validation;
using LionSimPlanner.Shared.Dtos;
using FluentAssertions;

namespace LionSimPlanner.Scheduling.Domain.Tests.Validation;

public sealed class FtlValidationServiceTests
{
    private static readonly DateTime BaseUtc = new(2026, 7, 29, 6, 0, 0, DateTimeKind.Utc);

    private static SimulatorSession CreateSession(DateTime start, DateTime end) => new()
    {
        SessionId = Guid.NewGuid(),
        SimulatorId = Guid.NewGuid(),
        StartTime = start,
        EndTime = end,
        SyllabusId = "B737_RecurrentTraining",
        Status = SessionStatus.Draft,
    };

    private static PilotPriorityDto CreateCaptain(DateTime lastDutyEnd, DateTime medicalExpiry, bool isExternal = false) =>
        new(
            PilotId: Guid.NewGuid(),
            EmployeeCode: "CAP001",
            FullName: "John Captain",
            Rank: "Captain",
            IsExternalUser: isExternal,
            NextTrainingDue: BaseUtc.AddYears(1),
            RequiredSyllabus: "RecurrentTraining",
            TypeRatings: new List<string> { "B737" }.AsReadOnly(),
            MedicalExpiry: medicalExpiry,
            LastDutyEndTime: lastDutyEnd,
            NextDutyStartTime: BaseUtc.AddDays(1));

    private static PilotPriorityDto? CreateFirstOfficer(DateTime lastDutyEnd, DateTime medicalExpiry, bool isExternal = false) =>
        new(
            PilotId: Guid.NewGuid(),
            EmployeeCode: "FO001",
            FullName: "Jane Officer",
            Rank: "FirstOfficer",
            IsExternalUser: isExternal,
            NextTrainingDue: BaseUtc.AddYears(1),
            RequiredSyllabus: "RecurrentTraining",
            TypeRatings: new List<string> { "B737" }.AsReadOnly(),
            MedicalExpiry: medicalExpiry,
            LastDutyEndTime: lastDutyEnd,
            NextDutyStartTime: BaseUtc.AddDays(1));

    private static InstructorValidationData CreateInstructor(
        DateTime lastDutyEnd,
        DateTime licenseExpiry,
        int currentMonthlyHours = 40,
        int maxMonthlyHours = 80,
        IReadOnlyList<string>? certifiedTypes = null,
        IReadOnlyList<string>? authorizedSyllabi = null) =>
        new(
            InstructorId: Guid.NewGuid(),
            EmployeeCode: "INS001",
            FullName: "Bob Instructor",
            CertifiedTypes: certifiedTypes ?? new List<string> { "B737" }.AsReadOnly(),
            AuthorizedSyllabi: authorizedSyllabi ?? new List<string> { "B737_RecurrentTraining" }.AsReadOnly(),
            LicenseExpiry: licenseExpiry,
            LastDutyEndTime: lastDutyEnd,
            CurrentMonthlyHours: currentMonthlyHours,
            MaxMonthlyHours: maxMonthlyHours);

    // ─────────────────────────────────────────────
    //  Valid scenarios
    // ─────────────────────────────────────────────

    [Fact]
    public void Validate_AllChecksPass_ReturnsValid()
    {
        var start = BaseUtc.AddHours(12);
        var session = CreateSession(start, start.AddHours(2));
        var captain = CreateCaptain(start.AddHours(-12), start.AddMonths(1));
        var fo = CreateFirstOfficer(start.AddHours(-12), start.AddMonths(1));
        var instructor = CreateInstructor(start.AddHours(-12), start.AddMonths(1));

        var sut = new FtlValidationService(minRestHours: 10.0);
        var result = sut.Validate(session, captain, fo, instructor, false, false);

        result.IsValid.Should().BeTrue();
        result.Violations.Should().BeEmpty();
    }

    [Fact]
    public void Validate_CaptainRestExactlyAtMinimum_ReturnsValid()
    {
        var start = BaseUtc.AddHours(12);
        var session = CreateSession(start, start.AddHours(2));
        var captain = CreateCaptain(start.AddHours(-10), start.AddMonths(1));
        var instructor = CreateInstructor(start.AddHours(-12), start.AddMonths(1));

        var sut = new FtlValidationService(minRestHours: 10.0);
        var result = sut.Validate(session, captain, null, instructor, false, false);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_NoFirstOfficer_ReturnsValid()
    {
        var start = BaseUtc.AddHours(12);
        var session = CreateSession(start, start.AddHours(2));
        var captain = CreateCaptain(start.AddHours(-12), start.AddMonths(1));
        var instructor = CreateInstructor(start.AddHours(-12), start.AddMonths(1));

        var sut = new FtlValidationService(minRestHours: 10.0);
        var result = sut.Validate(session, captain, null, instructor, false, false);

        result.IsValid.Should().BeTrue();
    }

    // ─────────────────────────────────────────────
    //  Captain rest violations
    // ─────────────────────────────────────────────

    [Fact]
    public void Validate_CaptainRestBelowMinimum_ReturnsViolation()
    {
        var start = BaseUtc.AddHours(12);
        var session = CreateSession(start, start.AddHours(2));
        var captain = CreateCaptain(start.AddHours(-9.5), start.AddMonths(1));
        var instructor = CreateInstructor(start.AddHours(-12), start.AddMonths(1));

        var sut = new FtlValidationService(minRestHours: 10.0);
        var result = sut.Validate(session, captain, null, instructor, false, false);

        result.IsValid.Should().BeFalse();
        result.Violations.Should().Contain(v => v.Contains("FTL Rest Violation") && v.Contains("Captain"));
    }

    [Fact]
    public void Validate_SkipCaptainChecks_NoCaptainViolation()
    {
        var start = BaseUtc.AddHours(12);
        var session = CreateSession(start, start.AddHours(2));
        var captain = CreateCaptain(start.AddHours(-9.5), start.AddMonths(1));
        var instructor = CreateInstructor(start.AddHours(-12), start.AddMonths(1));

        var sut = new FtlValidationService(minRestHours: 10.0);
        var result = sut.Validate(session, captain, null, instructor, true, false);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_CaptainMedicalExpired_ReturnsViolation()
    {
        var start = BaseUtc.AddHours(12);
        var session = CreateSession(start, start.AddHours(2));
        var captain = CreateCaptain(start.AddHours(-12), start.AddDays(-1));
        var instructor = CreateInstructor(start.AddHours(-12), start.AddMonths(1));

        var sut = new FtlValidationService(minRestHours: 10.0);
        var result = sut.Validate(session, captain, null, instructor, false, false);

        result.IsValid.Should().BeFalse();
        result.Violations.Should().Contain(v => v.Contains("Medical Certificate Expired") && v.Contains("Captain"));
    }

    // ─────────────────────────────────────────────
    //  First officer violations
    // ─────────────────────────────────────────────

    [Fact]
    public void Validate_FirstOfficerRestBelowMinimum_ReturnsViolation()
    {
        var start = BaseUtc.AddHours(12);
        var session = CreateSession(start, start.AddHours(2));
        var captain = CreateCaptain(start.AddHours(-12), start.AddMonths(1));
        var fo = CreateFirstOfficer(start.AddHours(-8), start.AddMonths(1));
        var instructor = CreateInstructor(start.AddHours(-12), start.AddMonths(1));

        var sut = new FtlValidationService(minRestHours: 10.0);
        var result = sut.Validate(session, captain, fo, instructor, false, false);

        result.IsValid.Should().BeFalse();
        result.Violations.Should().Contain(v => v.Contains("FTL Rest Violation") && v.Contains("First Officer"));
    }

    [Fact]
    public void Validate_SkipFirstOfficerChecks_NoFirstOfficerViolation()
    {
        var start = BaseUtc.AddHours(12);
        var session = CreateSession(start, start.AddHours(2));
        var captain = CreateCaptain(start.AddHours(-12), start.AddMonths(1));
        var fo = CreateFirstOfficer(start.AddHours(-8), start.AddMonths(1));
        var instructor = CreateInstructor(start.AddHours(-12), start.AddMonths(1));

        var sut = new FtlValidationService(minRestHours: 10.0);
        var result = sut.Validate(session, captain, fo, instructor, false, true);

        result.IsValid.Should().BeTrue();
    }

    // ─────────────────────────────────────────────
    //  Instructor violations
    // ─────────────────────────────────────────────

    [Fact]
    public void Validate_InstructorRestBelowMinimum_ReturnsViolation()
    {
        var start = BaseUtc.AddHours(12);
        var session = CreateSession(start, start.AddHours(2));
        var captain = CreateCaptain(start.AddHours(-12), start.AddMonths(1));
        var instructor = CreateInstructor(start.AddHours(-9), start.AddMonths(1));

        var sut = new FtlValidationService(minRestHours: 10.0);
        var result = sut.Validate(session, captain, null, instructor, false, false);

        result.IsValid.Should().BeFalse();
        result.Violations.Should().Contain(v => v.Contains("FTL Rest Violation") && v.Contains("Instructor"));
    }

    [Fact]
    public void Validate_InstructorMonthlyHoursExceeded_ReturnsViolation()
    {
        var start = BaseUtc.AddHours(12);
        var session = CreateSession(start, start.AddHours(4));
        var captain = CreateCaptain(start.AddHours(-12), start.AddMonths(1));
        var instructor = CreateInstructor(
            start.AddHours(-12), start.AddMonths(1),
            currentMonthlyHours: 78, maxMonthlyHours: 80);

        var sut = new FtlValidationService(minRestHours: 10.0);
        var result = sut.Validate(session, captain, null, instructor, false, false);

        result.IsValid.Should().BeFalse();
        result.Violations.Should().Contain(v => v.Contains("Instructor Monthly Hours Cap Exceeded"));
    }

    // ─────────────────────────────────────────────
    //  Certification and syllabus violations
    // ─────────────────────────────────────────────

    [Fact]
    public void Validate_TypeCertificationMismatch_ReturnsViolation()
    {
        var start = BaseUtc.AddHours(12);
        var session = CreateSession(start, start.AddHours(2));
        var captain = CreateCaptain(start.AddHours(-12), start.AddMonths(1));
        var instructor = CreateInstructor(
            start.AddHours(-12), start.AddMonths(1),
            certifiedTypes: new List<string> { "A320" }.AsReadOnly());

        var sut = new FtlValidationService(minRestHours: 10.0);
        var result = sut.Validate(session, captain, null, instructor, false, false);

        result.IsValid.Should().BeFalse();
        result.Violations.Should().Contain(v => v.Contains("Type Certification Mismatch"));
    }

    [Fact]
    public void Validate_SyllabusAuthorizationMissing_ReturnsViolation()
    {
        var start = BaseUtc.AddHours(12);
        var session = CreateSession(start, start.AddHours(2));
        var captain = CreateCaptain(start.AddHours(-12), start.AddMonths(1));
        var instructor = CreateInstructor(
            start.AddHours(-12), start.AddMonths(1),
            certifiedTypes: new List<string> { "B737" }.AsReadOnly(),
            authorizedSyllabi: new List<string> { "A320_InitialTypeRating" }.AsReadOnly());

        var sut = new FtlValidationService(minRestHours: 10.0);
        var result = sut.Validate(session, captain, null, instructor, false, false);

        result.IsValid.Should().BeFalse();
        result.Violations.Should().Contain(v => v.Contains("Syllabus Authorization Missing"));
    }

    [Fact]
    public void Validate_ExternalSession_SkipsCertificationChecks()
    {
        var start = BaseUtc.AddHours(12);
        var session = CreateSession(start, start.AddHours(2));
        session.SyllabusId = "External";
        var captain = CreateCaptain(start.AddHours(-12), start.AddMonths(1));
        var instructor = CreateInstructor(
            start.AddHours(-12), start.AddMonths(1),
            certifiedTypes: new List<string> { "A320" }.AsReadOnly(),
            authorizedSyllabi: new List<string> { "A320_InitialTypeRating" }.AsReadOnly());

        var sut = new FtlValidationService(minRestHours: 10.0);
        var result = sut.Validate(session, captain, null, instructor, false, false);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_ExternalCaptain_SkipsCertificationChecks()
    {
        var start = BaseUtc.AddHours(12);
        var session = CreateSession(start, start.AddHours(2));
        var captain = CreateCaptain(start.AddHours(-12), start.AddMonths(1), isExternal: true);
        var instructor = CreateInstructor(
            start.AddHours(-12), start.AddMonths(1),
            certifiedTypes: new List<string> { "A320" }.AsReadOnly(),
            authorizedSyllabi: new List<string> { "A320_InitialTypeRating" }.AsReadOnly());

        var sut = new FtlValidationService(minRestHours: 10.0);
        var result = sut.Validate(session, captain, null, instructor, false, false);

        result.IsValid.Should().BeTrue();
    }

    // ─────────────────────────────────────────────
    //  Instructor license
    // ─────────────────────────────────────────────

    [Fact]
    public void Validate_InstructorLicenseExpired_ReturnsViolation()
    {
        var start = BaseUtc.AddHours(12);
        var session = CreateSession(start, start.AddHours(2));
        var captain = CreateCaptain(start.AddHours(-12), start.AddMonths(1));
        var instructor = CreateInstructor(start.AddHours(-12), start.AddDays(-5));

        var sut = new FtlValidationService(minRestHours: 10.0);
        var result = sut.Validate(session, captain, null, instructor, false, false);

        result.IsValid.Should().BeFalse();
        result.Violations.Should().Contain(v => v.Contains("Instructor License Expired"));
    }

    // ─────────────────────────────────────────────
    //  Multiple violations
    // ─────────────────────────────────────────────

    [Fact]
    public void Validate_MultipleViolations_ReturnsAllViolations()
    {
        var start = BaseUtc.AddHours(12);
        var session = CreateSession(start, start.AddHours(4));
        var captain = CreateCaptain(start.AddHours(-9), start.AddDays(-1));
        var fo = CreateFirstOfficer(start.AddHours(-8), start.AddMonths(1));
        var instructor = CreateInstructor(
            start.AddHours(-7), start.AddDays(-3),
            currentMonthlyHours: 79, maxMonthlyHours: 80,
            certifiedTypes: new List<string> { "A320" }.AsReadOnly(),
            authorizedSyllabi: new List<string> { "A320_InitialTypeRating" }.AsReadOnly());

        var sut = new FtlValidationService(minRestHours: 10.0);
        var result = sut.Validate(session, captain, fo, instructor, false, false);

        result.IsValid.Should().BeFalse();
        result.Violations.Should().HaveCount(v => v >= 5);
        result.Violations.Should().Contain(v => v.Contains("Captain") && v.Contains("Rest"));
        result.Violations.Should().Contain(v => v.Contains("Captain") && v.Contains("Medical"));
        result.Violations.Should().Contain(v => v.Contains("First Officer") && v.Contains("Rest"));
        result.Violations.Should().Contain(v => v.Contains("Instructor") && v.Contains("Rest"));
        result.Violations.Should().Contain(v => v.Contains("Instructor") && v.Contains("License Expired"));
    }
}
