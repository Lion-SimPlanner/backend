using LionSimPlanner.Scheduling.Domain.Entities;
using LionSimPlanner.Scheduling.Domain.Enums;
using FluentAssertions;

namespace LionSimPlanner.Scheduling.Domain.Tests.Entities;

public sealed class SimulatorSessionTests
{
    private static SimulatorSession CreateDefault() => new()
    {
        SessionId = Guid.NewGuid(),
        SimulatorId = Guid.NewGuid(),
        StartTime = new DateTime(2026, 7, 29, 8, 0, 0, DateTimeKind.Utc),
        EndTime = new DateTime(2026, 7, 29, 10, 0, 0, DateTimeKind.Utc),
        SyllabusId = "B737_RecurrentTraining",
    };

    // ─────────────────────────────────────────────
    //  Defaults
    // ─────────────────────────────────────────────

    [Fact]
    public void Constructor_DefaultStatus_IsDraft()
    {
        var sut = new SimulatorSession();
        sut.Status.Should().Be(SessionStatus.Draft);
    }

    [Fact]
    public void Constructor_DefaultIsGraded_IsFalse()
    {
        var sut = new SimulatorSession();
        sut.IsGraded.Should().BeFalse();
    }

    [Fact]
    public void Constructor_DefaultCreatedAt_IsRecent()
    {
        var sut = new SimulatorSession();
        sut.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    // ─────────────────────────────────────────────
    //  DurationHours
    // ─────────────────────────────────────────────

    [Fact]
    public void DurationHours_TwoHourSession_ReturnsTwo()
    {
        var sut = CreateDefault();
        sut.DurationHours.Should().Be(2.0);
    }

    [Fact]
    public void DurationHours_ThreeHourSession_ReturnsThree()
    {
        var sut = CreateDefault();
        sut.EndTime = sut.StartTime.AddHours(3);
        sut.DurationHours.Should().Be(3.0);
    }

    // ─────────────────────────────────────────────
    //  Valid state transitions
    // ─────────────────────────────────────────────

    [Fact]
    public void TransitionTo_DraftToScheduled_UpdatesStatus()
    {
        var sut = CreateDefault();
        sut.TransitionTo(SessionStatus.Scheduled);
        sut.Status.Should().Be(SessionStatus.Scheduled);
    }

    [Fact]
    public void TransitionTo_ScheduledToInProgress_UpdatesStatus()
    {
        var sut = CreateDefault();
        sut.Status = SessionStatus.Scheduled;
        sut.TransitionTo(SessionStatus.InProgress);
        sut.Status.Should().Be(SessionStatus.InProgress);
    }

    [Fact]
    public void TransitionTo_InProgressToCompleted_UpdatesStatus()
    {
        var sut = CreateDefault();
        sut.Status = SessionStatus.InProgress;
        sut.TransitionTo(SessionStatus.Completed);
        sut.Status.Should().Be(SessionStatus.Completed);
    }

    [Fact]
    public void TransitionTo_ScheduledToCancelled_UpdatesStatus()
    {
        var sut = CreateDefault();
        sut.Status = SessionStatus.Scheduled;
        sut.TransitionTo(SessionStatus.Cancelled);
        sut.Status.Should().Be(SessionStatus.Cancelled);
    }

    [Fact]
    public void TransitionTo_InProgressToCancelled_UpdatesStatus()
    {
        var sut = CreateDefault();
        sut.Status = SessionStatus.InProgress;
        sut.TransitionTo(SessionStatus.Cancelled);
        sut.Status.Should().Be(SessionStatus.Cancelled);
    }

    [Fact]
    public void TransitionTo_DraftToCancelled_UpdatesStatus()
    {
        var sut = CreateDefault();
        sut.TransitionTo(SessionStatus.Cancelled);
        sut.Status.Should().Be(SessionStatus.Cancelled);
    }

    [Fact]
    public void TransitionTo_InProgressToTerminatedEarly_UpdatesStatus()
    {
        var sut = CreateDefault();
        sut.Status = SessionStatus.InProgress;
        sut.TransitionTo(SessionStatus.TerminatedEarly);
        sut.Status.Should().Be(SessionStatus.TerminatedEarly);
    }

    // ─────────────────────────────────────────────
    //  Invalid state transitions
    // ─────────────────────────────────────────────

    [Fact]
    public void TransitionTo_DraftToCompleted_ThrowsInvalidOperation()
    {
        var sut = CreateDefault();
        var act = () => sut.TransitionTo(SessionStatus.Completed);
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Cannot transition*");
    }

    [Fact]
    public void TransitionTo_DraftToInProgress_ThrowsInvalidOperation()
    {
        var sut = CreateDefault();
        var act = () => sut.TransitionTo(SessionStatus.InProgress);
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Cannot transition*");
    }

    [Fact]
    public void TransitionTo_CompletedToAny_ThrowsInvalidOperation()
    {
        var sut = CreateDefault();
        sut.Status = SessionStatus.Completed;
        var act = () => sut.TransitionTo(SessionStatus.InProgress);
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Cannot transition*");
    }

    [Fact]
    public void TransitionTo_CancelledToAny_ThrowsInvalidOperation()
    {
        var sut = CreateDefault();
        sut.Status = SessionStatus.Cancelled;
        var act = () => sut.TransitionTo(SessionStatus.Scheduled);
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Cannot transition*");
    }

    [Fact]
    public void TransitionTo_TerminatedEarlyToAny_ThrowsInvalidOperation()
    {
        var sut = CreateDefault();
        sut.Status = SessionStatus.TerminatedEarly;
        var act = () => sut.TransitionTo(SessionStatus.Completed);
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Cannot transition*");
    }
}
