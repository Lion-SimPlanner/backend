using LionSimPlanner.Scheduling.Application.Commands;
using LionSimPlanner.Scheduling.Domain.Entities;
using LionSimPlanner.Scheduling.Domain.Enums;
using LionSimPlanner.Scheduling.Infrastructure;
using LionSimPlanner.Scheduling.Infrastructure.Handlers;
using LionSimPlanner.Shared.Events;
using LionSimPlanner.Shared.Hubs;
using FluentAssertions;
using MediatR;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;

namespace LionSimPlanner.Scheduling.Application.Tests.Handlers;

public sealed class CompleteGradingHandlerTests
{
    private static SchedulingDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<SchedulingDbContext>()
            .UseInMemoryDatabase($"CompleteGradingTest-{Guid.NewGuid()}")
            .Options;
        return new SchedulingDbContext(options);
    }

    private static CompleteGradingCommand CreateCommand(Guid sessionId, string grade = "PASSED") => new(
        SessionId: sessionId,
        GradeStatus: grade,
        InstructorNotes: "Well executed.",
        TraineeEmployeeCode: "PLT001");

    private static SimulatorSession SeedInProgressSession(SchedulingDbContext db)
    {
        var session = new SimulatorSession
        {
            SessionId = Guid.NewGuid(),
            SimulatorId = Guid.NewGuid(),
            Status = SessionStatus.InProgress,
            StartTime = DateTime.UtcNow.AddHours(-2),
            EndTime = DateTime.UtcNow,
            SyllabusId = "B737_RecurrentTraining",
            CaptainId = Guid.NewGuid(),
            InstructorId = Guid.NewGuid(),
            IsGraded = false,
        };
        db.Sessions.Add(session);
        db.SaveChanges();
        return session;
    }

    // ─────────────────────────────────────────────
    //  Successful grading
    // ─────────────────────────────────────────────

    [Fact]
    public async Task Handle_ValidInProgressSession_UpdatesStatusToCompleted()
    {
        var db = CreateDbContext();
        var session = SeedInProgressSession(db);
        var command = CreateCommand(session.SessionId);

        var publisherMock = new Mock<IPublisher>();
        var hubContextMock = new Mock<IHubContext<SimPlannerHub>>();
        var hubClientsMock = new Mock<IHubClients>();
        var clientProxyMock = new Mock<IClientProxy>();
        hubClientsMock.Setup(c => c.All).Returns(clientProxyMock.Object);
        hubContextMock.Setup(h => h.Clients).Returns(hubClientsMock.Object);

        var sut = new CompleteGradingHandler(
            db,
            publisherMock.Object,
            hubContextMock.Object,
            Mock.Of<ILogger<CompleteGradingHandler>>());

        var result = await sut.Handle(command, CancellationToken.None);

        result.Success.Should().BeTrue();

        var updated = await db.Sessions.FindAsync(session.SessionId);
        updated!.Status.Should().Be(SessionStatus.Completed);
        updated.IsGraded.Should().BeTrue();
        updated.GradeStatus.Should().Be("PASSED");
        updated.InstructorNotes.Should().Be("Well executed.");
        updated.TraineeEmployeeCode.Should().Be("PLT001");
    }

    [Fact]
    public async Task Handle_ValidSession_PublishesTrainingRecordCompletedNotification()
    {
        var db = CreateDbContext();
        var session = SeedInProgressSession(db);
        var command = CreateCommand(session.SessionId, "FAILED");

        TrainingRecordCompletedNotification? captured = null;
        var publisherMock = new Mock<IPublisher>();
        publisherMock
            .Setup(p => p.Publish(It.IsAny<TrainingRecordCompletedNotification>(), It.IsAny<CancellationToken>()))
            .Callback<INotification, CancellationToken>((n, _) => captured = (TrainingRecordCompletedNotification)n)
            .Returns(Task.CompletedTask);

        var hubContextMock = new Mock<IHubContext<SimPlannerHub>>();
        var hubClientsMock = new Mock<IHubClients>();
        var clientProxyMock = new Mock<IClientProxy>();
        hubClientsMock.Setup(c => c.All).Returns(clientProxyMock.Object);
        hubContextMock.Setup(h => h.Clients).Returns(hubClientsMock.Object);

        var sut = new CompleteGradingHandler(
            db,
            publisherMock.Object,
            hubContextMock.Object,
            Mock.Of<ILogger<CompleteGradingHandler>>());

        await sut.Handle(command, CancellationToken.None);

        captured.Should().NotBeNull();
        captured!.SessionId.Should().Be(session.SessionId);
        captured.EmployeeCode.Should().Be("PLT001");
        captured.GradeStatus.Should().Be("FAILED");
        captured.SyllabusId.Should().Be("B737_RecurrentTraining");
        captured.IsGraded.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_ValidSession_SendsSessionGradedSignalR()
    {
        var db = CreateDbContext();
        var session = SeedInProgressSession(db);
        var command = CreateCommand(session.SessionId);

        var publisherMock = new Mock<IPublisher>();
        var hubContextMock = new Mock<IHubContext<SimPlannerHub>>();
        var hubClientsMock = new Mock<IHubClients>();
        var clientProxyMock = new Mock<IClientProxy>();
        hubClientsMock.Setup(c => c.All).Returns(clientProxyMock.Object);
        hubContextMock.Setup(h => h.Clients).Returns(hubClientsMock.Object);

        var sut = new CompleteGradingHandler(
            db,
            publisherMock.Object,
            hubContextMock.Object,
            Mock.Of<ILogger<CompleteGradingHandler>>());

        await sut.Handle(command, CancellationToken.None);

        var updated = await db.Sessions.FindAsync(session.SessionId);
        updated!.Status.Should().Be(SessionStatus.Completed);
        updated.GradeStatus.Should().Be("PASSED");
    }

    // ─────────────────────────────────────────────
    //  Error cases
    // ─────────────────────────────────────────────

    [Fact]
    public async Task Handle_SessionNotFound_ReturnsFailure()
    {
        var db = CreateDbContext();
        var command = CreateCommand(Guid.NewGuid());

        var sut = new CompleteGradingHandler(
            db,
            Mock.Of<IPublisher>(),
            Mock.Of<IHubContext<SimPlannerHub>>(),
            Mock.Of<ILogger<CompleteGradingHandler>>());

        var result = await sut.Handle(command, CancellationToken.None);

        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Contain("not found");
    }

    [Fact]
    public async Task Handle_SessionNotInProgress_ReturnsFailure()
    {
        var db = CreateDbContext();
        var session = new SimulatorSession
        {
            SessionId = Guid.NewGuid(),
            SimulatorId = Guid.NewGuid(),
            Status = SessionStatus.Draft,
        };
        db.Sessions.Add(session);
        db.SaveChanges();

        var command = CreateCommand(session.SessionId);

        var sut = new CompleteGradingHandler(
            db,
            Mock.Of<IPublisher>(),
            Mock.Of<IHubContext<SimPlannerHub>>(),
            Mock.Of<ILogger<CompleteGradingHandler>>());

        var result = await sut.Handle(command, CancellationToken.None);

        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Contain("IN_PROGRESS");
    }

    [Fact]
    public async Task Handle_CompletedSession_ReturnsFailure()
    {
        var db = CreateDbContext();
        var session = new SimulatorSession
        {
            SessionId = Guid.NewGuid(),
            SimulatorId = Guid.NewGuid(),
            Status = SessionStatus.Completed,
        };
        db.Sessions.Add(session);
        db.SaveChanges();

        var command = CreateCommand(session.SessionId);

        var sut = new CompleteGradingHandler(
            db,
            Mock.Of<IPublisher>(),
            Mock.Of<IHubContext<SimPlannerHub>>(),
            Mock.Of<ILogger<CompleteGradingHandler>>());

        var result = await sut.Handle(command, CancellationToken.None);

        result.Success.Should().BeFalse();
    }
}
