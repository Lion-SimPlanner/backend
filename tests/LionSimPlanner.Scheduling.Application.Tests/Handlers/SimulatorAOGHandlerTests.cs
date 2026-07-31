using LionSimPlanner.Notifications;
using LionSimPlanner.Scheduling.Domain.Entities;
using LionSimPlanner.Scheduling.Domain.Enums;
using LionSimPlanner.Scheduling.Infrastructure;
using LionSimPlanner.Scheduling.Infrastructure.Handlers;
using LionSimPlanner.Shared.Events;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;

namespace LionSimPlanner.Scheduling.Application.Tests.Handlers;

public sealed class SimulatorAOGHandlerTests
{
    private static SchedulingDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<SchedulingDbContext>()
            .UseInMemoryDatabase($"AOGTest-{Guid.NewGuid()}")
            .Options;
        return new SchedulingDbContext(options);
    }

    private static SimulatorAOGNotification CreateNotification(Guid simulatorId) => new(
        SimulatorId: simulatorId,
        SimulatorName: "Sim-07",
        ReportedByEngineerCode: "ENG001",
        FaultDescription: "Motion system hydraulic leak",
        OccurredAt: DateTime.UtcNow);

    private static SimulatorSession CreateSession(Guid simulatorId, SessionStatus status, DateTime start)
    {
        var twoHrs = TimeSpan.FromHours(2);
        return new SimulatorSession
        {
            SessionId = Guid.NewGuid(),
            SimulatorId = simulatorId,
            Status = status,
            StartTime = start,
            EndTime = start.Add(twoHrs),
            SyllabusId = "B737_RecurrentTraining",
        };
    }

    // ─────────────────────────────────────────────
    //  Cancellation logic
    // ─────────────────────────────────────────────

    [Fact]
    public async Task Handle_AOGDeclared_CancelsScheduledSessions()
    {
        var simId = Guid.NewGuid();
        var now = DateTime.UtcNow;
        var db = CreateDbContext();
        db.Sessions.Add(CreateSession(simId, SessionStatus.Scheduled, now.AddHours(1)));
        db.Sessions.Add(CreateSession(simId, SessionStatus.Scheduled, now.AddDays(1)));
        db.SaveChanges();

        var emailMock = new Mock<IEmailNotificationService>();
        var sut = new SimulatorAOGHandler(
            db, emailMock.Object, Mock.Of<ILogger<SimulatorAOGHandler>>());

        await sut.Handle(CreateNotification(simId), CancellationToken.None);

        var affected = await db.Sessions
            .Where(s => s.SimulatorId == simId && s.Status == SessionStatus.Cancelled)
            .ToListAsync();
        affected.Should().HaveCount(2);
        affected.Should().AllSatisfy(s => s.CancellationReason.Should().Contain("AOG"));
    }

    [Fact]
    public async Task Handle_AOGDeclared_CancelsInProgressSessions()
    {
        var simId = Guid.NewGuid();
        var now = DateTime.UtcNow;
        var db = CreateDbContext();
        db.Sessions.Add(CreateSession(simId, SessionStatus.InProgress, now.AddHours(1)));
        db.SaveChanges();

        var emailMock = new Mock<IEmailNotificationService>();
        var sut = new SimulatorAOGHandler(
            db, emailMock.Object, Mock.Of<ILogger<SimulatorAOGHandler>>());

        await sut.Handle(CreateNotification(simId), CancellationToken.None);

        var cancelled = await db.Sessions
            .FirstOrDefaultAsync(s => s.SimulatorId == simId);
        cancelled!.Status.Should().Be(SessionStatus.Cancelled);
    }

    [Fact]
    public async Task Handle_AOGDeclared_DoesNotCancelPastSessions()
    {
        var simId = Guid.NewGuid();
        var now = DateTime.UtcNow;
        var db = CreateDbContext();
        db.Sessions.Add(CreateSession(simId, SessionStatus.Scheduled, now.AddDays(-1)));
        db.SaveChanges();

        var emailMock = new Mock<IEmailNotificationService>();
        var sut = new SimulatorAOGHandler(
            db, emailMock.Object, Mock.Of<ILogger<SimulatorAOGHandler>>());

        await sut.Handle(CreateNotification(simId), CancellationToken.None);

        var session = await db.Sessions.FirstAsync();
        session.Status.Should().Be(SessionStatus.Scheduled);
    }

    [Fact]
    public async Task Handle_AOGDeclared_DoesNotAffectOtherSimulators()
    {
        var simA = Guid.NewGuid();
        var simB = Guid.NewGuid();
        var now = DateTime.UtcNow;
        var db = CreateDbContext();
        db.Sessions.Add(CreateSession(simA, SessionStatus.Scheduled, now.AddHours(1)));
        db.Sessions.Add(CreateSession(simB, SessionStatus.Scheduled, now.AddHours(1)));
        db.SaveChanges();

        var emailMock = new Mock<IEmailNotificationService>();
        var sut = new SimulatorAOGHandler(
            db, emailMock.Object, Mock.Of<ILogger<SimulatorAOGHandler>>());

        await sut.Handle(CreateNotification(simA), CancellationToken.None);

        var otherSession = await db.Sessions.FirstAsync(s => s.SimulatorId == simB);
        otherSession.Status.Should().Be(SessionStatus.Scheduled);
    }

    [Fact]
    public async Task Handle_NoAffectedSessions_CompletesSilently()
    {
        var simId = Guid.NewGuid();
        var now = DateTime.UtcNow;
        var db = CreateDbContext();
        db.Sessions.Add(CreateSession(simId, SessionStatus.Completed, now.AddHours(-2)));
        db.SaveChanges();

        var emailMock = new Mock<IEmailNotificationService>();
        var sut = new SimulatorAOGHandler(
            db, emailMock.Object, Mock.Of<ILogger<SimulatorAOGHandler>>());

        var act = async () => await sut.Handle(CreateNotification(simId), CancellationToken.None);

        await act.Should().NotThrowAsync();
    }

    // ─────────────────────────────────────────────
    //  Email notifications
    // ─────────────────────────────────────────────

    [Fact]
    public async Task Handle_AOGDeclared_SendsCancellationEmailForEachAffectedSession()
    {
        var simId = Guid.NewGuid();
        var now = DateTime.UtcNow;
        var db = CreateDbContext();
        db.Sessions.Add(CreateSession(simId, SessionStatus.Scheduled, now.AddHours(1)));
        db.Sessions.Add(CreateSession(simId, SessionStatus.Scheduled, now.AddDays(1)));
        db.SaveChanges();

        var emailMock = new Mock<IEmailNotificationService>();
        var sut = new SimulatorAOGHandler(
            db, emailMock.Object, Mock.Of<ILogger<SimulatorAOGHandler>>());

        await sut.Handle(CreateNotification(simId), CancellationToken.None);

        emailMock.Verify(
            e => e.SendSessionCancelledAsync(
                It.IsAny<Guid>(),
                It.IsAny<DateTime>(),
                It.IsAny<DateTime>(),
                It.IsAny<string>(),
                null,
                It.IsAny<CancellationToken>()),
            Times.Exactly(2));
    }

    [Fact]
    public async Task Handle_EmailThrows_DoesNotPropagateException()
    {
        var simId = Guid.NewGuid();
        var now = DateTime.UtcNow;
        var db = CreateDbContext();
        db.Sessions.Add(CreateSession(simId, SessionStatus.Scheduled, now.AddHours(1)));
        db.SaveChanges();

        var emailMock = new Mock<IEmailNotificationService>();
        emailMock
            .Setup(e => e.SendSessionCancelledAsync(
                It.IsAny<Guid>(), It.IsAny<DateTime>(), It.IsAny<DateTime>(),
                It.IsAny<string>(), null, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("SMTP failure"));

        var sut = new SimulatorAOGHandler(
            db, emailMock.Object, Mock.Of<ILogger<SimulatorAOGHandler>>());

        var act = async () => await sut.Handle(CreateNotification(simId), CancellationToken.None);

        await act.Should().NotThrowAsync();
    }
}
