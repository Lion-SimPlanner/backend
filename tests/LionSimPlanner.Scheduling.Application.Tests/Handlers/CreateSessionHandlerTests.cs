using LionSimPlanner.Scheduling.Application.Commands;
using LionSimPlanner.Scheduling.Domain.Entities;
using LionSimPlanner.Scheduling.Domain.Enums;
using LionSimPlanner.Scheduling.Infrastructure;
using LionSimPlanner.Scheduling.Infrastructure.Handlers;
using LionSimPlanner.Shared.Dtos;
using LionSimPlanner.Shared.Queries;
using FluentAssertions;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;

namespace LionSimPlanner.Scheduling.Application.Tests.Handlers;

public sealed class CreateSessionHandlerTests
{
    private static SchedulingDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<SchedulingDbContext>()
            .UseInMemoryDatabase($"CreateSessionTest-{Guid.NewGuid()}")
            .Options;
        return new SchedulingDbContext(options);
    }

    private static CreateSessionCommand CreateCommand(Guid simulatorId, DateTime start, DateTime end, Guid? captainId = null) => new(
        SimulatorId: simulatorId,
        SessionType: "Recurrent",
        StartTime: start,
        EndTime: end,
        CaptainId: captainId ?? Guid.NewGuid(),
        FirstOfficerId: null,
        InstructorId: Guid.NewGuid(),
        EngineerId: null,
        SyllabusId: "B737_RecurrentTraining",
        TraineeEmployeeCode: "PLT001");

    private static CreateSessionHandler CreateHandler(
        SchedulingDbContext db,
        Mock<ISender>? mediatorMock = null,
        Mock<IConfiguration>? configMock = null,
        Guid? captainId = null)
    {
        mediatorMock ??= new Mock<ISender>();
        configMock ??= new Mock<IConfiguration>();
        configMock.Setup(c => c["TrainingSync:MinRestHours"]).Returns("10.0");

        var now = DateTime.UtcNow;
        var resolvedCaptainId = captainId ?? Guid.NewGuid();

        var captain = new PilotPriorityDto(
            PilotId: resolvedCaptainId,
            EmployeeCode: "PLT001",
            FullName: "Alice Pilot",
            Rank: "Captain",
            IsExternalUser: false,
            NextTrainingDue: now.AddMonths(6),
            RequiredSyllabus: "RecurrentTraining",
            TypeRatings: new List<string> { "B737" }.AsReadOnly(),
            MedicalExpiry: now.AddMonths(12),
            LastDutyEndTime: now.AddDays(-2),
            NextDutyStartTime: now.AddDays(1));

        var instructor = new InstructorValidationData(
            InstructorId: Guid.NewGuid(),
            EmployeeCode: "INS001",
            FullName: "Bob Instructor",
            CertifiedTypes: new List<string> { "B737" }.AsReadOnly(),
            AuthorizedSyllabi: new List<string> { "B737_RecurrentTraining" }.AsReadOnly(),
            LicenseExpiry: now.AddMonths(24),
            LastDutyEndTime: now.AddDays(-2),
            CurrentMonthlyHours: 20,
            MaxMonthlyHours: 100);

        mediatorMock.Setup(m => m.Send(It.IsAny<GetPriorityQueueQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<PilotPriorityDto> { captain }.AsReadOnly());
        mediatorMock.Setup(m => m.Send(It.IsAny<GetInstructorByIdQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(instructor);

        return new CreateSessionHandler(
            db,
            mediatorMock.Object,
            configMock.Object,
            Mock.Of<ILogger<CreateSessionHandler>>());
    }

    [Fact]
    public async Task Handle_PastStartTime_ReturnsViolation()
    {
        var db = CreateDbContext();
        var sut = CreateHandler(db);
        var command = CreateCommand(Guid.NewGuid(), DateTime.UtcNow.AddHours(-2), DateTime.UtcNow);

        var result = await sut.Handle(command, CancellationToken.None);

        result.Success.Should().BeFalse();
        result.SessionId.Should().BeNull();
        result.Violations.Should().Contain(v => v.Contains("past"));
    }

    [Fact]
    public async Task Handle_OverlappingSessionOnSameSimulator_ReturnsViolation()
    {
        var db = CreateDbContext();
        var simulatorId = Guid.NewGuid();
        var start = DateTime.UtcNow.AddDays(1);
        var end = start.AddHours(2);

        db.Sessions.Add(new SimulatorSession
        {
            SessionId = Guid.NewGuid(),
            SimulatorId = simulatorId,
            SessionType = SessionType.Recurrent,
            Status = SessionStatus.Scheduled,
            StartTime = start.AddMinutes(30),
            EndTime = end.AddMinutes(-30),
            SyllabusId = "B737_RecurrentTraining",
        });
        db.SaveChanges();

        var sut = CreateHandler(db);
        var command = CreateCommand(simulatorId, start, end);

        var result = await sut.Handle(command, CancellationToken.None);

        result.Success.Should().BeFalse();
        result.SessionId.Should().BeNull();
        result.Violations.Should().Contain(v => v.Contains("overlapping"));
    }

    [Fact]
    public async Task Handle_NonOverlappingSessionOnSameSimulator_Succeeds()
    {
        var db = CreateDbContext();
        var simulatorId = Guid.NewGuid();
        var start = DateTime.UtcNow.AddDays(1);
        var end = start.AddHours(2);

        db.Sessions.Add(new SimulatorSession
        {
            SessionId = Guid.NewGuid(),
            SimulatorId = simulatorId,
            SessionType = SessionType.Recurrent,
            Status = SessionStatus.Scheduled,
            StartTime = end.AddHours(2),
            EndTime = end.AddHours(4),
            SyllabusId = "B737_RecurrentTraining",
        });
        db.SaveChanges();

        var captainId = Guid.NewGuid();
        var sut = CreateHandler(db, captainId: captainId);
        var command = CreateCommand(simulatorId, start, end, captainId);

        var result = await sut.Handle(command, CancellationToken.None);

        result.Success.Should().BeTrue();
        result.SessionId.Should().NotBeNull();
        result.Violations.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_CancelledOverlappingSession_DoesNotBlockBooking()
    {
        var db = CreateDbContext();
        var simulatorId = Guid.NewGuid();
        var start = DateTime.UtcNow.AddDays(1);
        var end = start.AddHours(2);

        db.Sessions.Add(new SimulatorSession
        {
            SessionId = Guid.NewGuid(),
            SimulatorId = simulatorId,
            SessionType = SessionType.Recurrent,
            Status = SessionStatus.Cancelled,
            StartTime = start.AddMinutes(30),
            EndTime = end.AddMinutes(-30),
            SyllabusId = "B737_RecurrentTraining",
        });
        db.SaveChanges();

        var captainId = Guid.NewGuid();
        var sut = CreateHandler(db, captainId: captainId);
        var command = CreateCommand(simulatorId, start, end, captainId);

        var result = await sut.Handle(command, CancellationToken.None);

        result.Success.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_DifferentSimulatorSameTime_Succeeds()
    {
        var db = CreateDbContext();
        var otherSimulatorId = Guid.NewGuid();
        var start = DateTime.UtcNow.AddDays(1);
        var end = start.AddHours(2);

        var captainId = Guid.NewGuid();
        var sut = CreateHandler(db, captainId: captainId);
        var command = CreateCommand(otherSimulatorId, start, end, captainId);

        var result = await sut.Handle(command, CancellationToken.None);

        result.Success.Should().BeTrue();
    }
}
