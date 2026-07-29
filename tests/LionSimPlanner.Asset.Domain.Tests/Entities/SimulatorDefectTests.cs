using LionSimPlanner.Asset.Domain.Entities;
using FluentAssertions;

namespace LionSimPlanner.Asset.Domain.Tests.Entities;

public sealed class SimulatorDefectTests
{
    // ─────────────────────────────────────────────
    //  Defaults
    // ─────────────────────────────────────────────

    [Fact]
    public void Constructor_DefaultStatus_IsOpen()
    {
        var sut = new SimulatorDefect();
        sut.Status.Should().Be("Open");
    }

    [Fact]
    public void Constructor_DefaultSeverity_IsEmpty()
    {
        var sut = new SimulatorDefect();
        sut.Severity.Should().BeEmpty();
    }

    [Fact]
    public void Constructor_DefaultReportedBy_IsEmpty()
    {
        var sut = new SimulatorDefect();
        sut.ReportedBy.Should().BeEmpty();
    }

    [Fact]
    public void Constructor_DefaultResolutionNotes_IsNull()
    {
        var sut = new SimulatorDefect();
        sut.ResolutionNotes.Should().BeNull();
    }

    [Fact]
    public void Constructor_DefaultResolvedAt_IsNull()
    {
        var sut = new SimulatorDefect();
        sut.ResolvedAt.Should().BeNull();
    }

    [Fact]
    public void Constructor_DefaultReportedAt_IsRecent()
    {
        var sut = new SimulatorDefect();
        sut.ReportedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    // ─────────────────────────────────────────────
    //  Property assignment
    // ─────────────────────────────────────────────

    [Fact]
    public void SetSeverity_StoresCorrectly()
    {
        var sut = new SimulatorDefect { Severity = "AOG" };
        sut.Severity.Should().Be("AOG");
    }

    [Fact]
    public void SetSeverity_MEL_StoresCorrectly()
    {
        var sut = new SimulatorDefect { Severity = "MEL" };
        sut.Severity.Should().Be("MEL");
    }

    [Fact]
    public void SetSeverity_Defect_StoresCorrectly()
    {
        var sut = new SimulatorDefect { Severity = "Defect" };
        sut.Severity.Should().Be("Defect");
    }

    [Fact]
    public void SetSystemAffected_StoresCorrectly()
    {
        var sut = new SimulatorDefect { SystemAffected = "Motion Platform" };
        sut.SystemAffected.Should().Be("Motion Platform");
    }

    [Fact]
    public void SetReportedBy_StoresCorrectly()
    {
        var sut = new SimulatorDefect { ReportedBy = "John Instructor" };
        sut.ReportedBy.Should().Be("John Instructor");
    }

    [Fact]
    public void SetResolutionNotes_StoresCorrectly()
    {
        var sut = new SimulatorDefect { ResolutionNotes = "Replaced actuator" };
        sut.ResolutionNotes.Should().Be("Replaced actuator");
    }

    [Fact]
    public void SetResolvedAt_StoresCorrectly()
    {
        var now = DateTime.UtcNow;
        var sut = new SimulatorDefect { ResolvedAt = now };
        sut.ResolvedAt.Should().Be(now);
    }

    [Fact]
    public void SetStatus_Resolved_StoresCorrectly()
    {
        var sut = new SimulatorDefect { Status = "Resolved" };
        sut.Status.Should().Be("Resolved");
    }

    // ─────────────────────────────────────────────
    //  Status lifecycle
    // ─────────────────────────────────────────────

    [Fact]
    public void MarkResolved_SetsStatusAndTimestamp()
    {
        var sut = new SimulatorDefect();
        sut.Status = "Resolved";
        sut.ResolvedAt = DateTime.UtcNow;

        sut.Status.Should().Be("Resolved");
        sut.ResolvedAt.Should().NotBeNull();
    }
}
