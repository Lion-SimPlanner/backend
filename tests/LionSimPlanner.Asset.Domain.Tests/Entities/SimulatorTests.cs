using LionSimPlanner.Asset.Domain.Entities;
using LionSimPlanner.Asset.Domain.Enums;
using FluentAssertions;

namespace LionSimPlanner.Asset.Domain.Tests.Entities;

public sealed class SimulatorTests
{
    // ─────────────────────────────────────────────
    //  Defaults
    // ─────────────────────────────────────────────

    [Fact]
    public void Constructor_DefaultStatus_IsReady()
    {
        var sut = new Simulator();
        sut.Status.Should().Be(SimulatorStatus.Ready);
    }

    [Fact]
    public void Constructor_DefaultDefectsCollection_IsEmpty()
    {
        var sut = new Simulator();
        sut.Defects.Should().BeEmpty();
    }

    [Fact]
    public void Constructor_DefaultMaintenanceLogsCollection_IsEmpty()
    {
        var sut = new Simulator();
        sut.MaintenanceLogs.Should().BeEmpty();
    }

    // ─────────────────────────────────────────────
    //  Property assignment
    // ─────────────────────────────────────────────

    [Fact]
    public void SetName_StoresCorrectly()
    {
        var sut = new Simulator { Name = "Sim-07" };
        sut.Name.Should().Be("Sim-07");
    }

    [Fact]
    public void SetBayNumber_StoresCorrectly()
    {
        var sut = new Simulator { BayNumber = "Bay-3" };
        sut.BayNumber.Should().Be("Bay-3");
    }

    [Fact]
    public void SetAircraftType_StoresCorrectly()
    {
        var sut = new Simulator { AircraftType = "B737-800" };
        sut.AircraftType.Should().Be("B737-800");
    }

    [Fact]
    public void SetStatus_StoresCorrectly()
    {
        var sut = new Simulator { Status = SimulatorStatus.AOG };
        sut.Status.Should().Be(SimulatorStatus.AOG);
    }

    // ─────────────────────────────────────────────
    //  ApplyDefect — severity → status mapping
    // ─────────────────────────────────────────────

    [Fact]
    public void ApplyDefect_AOGSeverity_SetsStatusToAOG()
    {
        var sut = new Simulator();
        var defect = new SimulatorDefect { Severity = "AOG" };

        sut.ApplyDefect(defect);

        sut.Status.Should().Be(SimulatorStatus.AOG);
    }

    [Fact]
    public void ApplyDefect_AogLowercase_SetsStatusToAOG()
    {
        var sut = new Simulator();
        var defect = new SimulatorDefect { Severity = "aog" };

        sut.ApplyDefect(defect);

        sut.Status.Should().Be(SimulatorStatus.AOG);
    }

    [Fact]
    public void ApplyDefect_MELSeverity_SetsStatusToMEL()
    {
        var sut = new Simulator();
        var defect = new SimulatorDefect { Severity = "MEL" };

        sut.ApplyDefect(defect);

        sut.Status.Should().Be(SimulatorStatus.MEL);
    }

    [Fact]
    public void ApplyDefect_DefectSeverity_SetsStatusToDefect()
    {
        var sut = new Simulator();
        var defect = new SimulatorDefect { Severity = "Defect" };

        sut.ApplyDefect(defect);

        sut.Status.Should().Be(SimulatorStatus.Defect);
    }

    [Fact]
    public void ApplyDefect_UnknownSeverity_DoesNotChangeStatusFromReady()
    {
        var sut = new Simulator();
        var defect = new SimulatorDefect { Severity = "Unknown" };

        sut.ApplyDefect(defect);

        sut.Status.Should().Be(SimulatorStatus.Ready);
    }

    [Fact]
    public void ApplyDefect_EmptySeverity_DoesNotChangeStatusFromReady()
    {
        var sut = new Simulator();
        var defect = new SimulatorDefect { Severity = "" };

        sut.ApplyDefect(defect);

        sut.Status.Should().Be(SimulatorStatus.Ready);
    }
}
