using System;
using System.Collections.Generic;
using System.Linq;
using TRM.Core;
using Xunit;
using Xunit.Abstractions;

namespace TRM.Tests.CoreTests;

/// <summary>
/// Deterministic fixation/validation tests for BulletClusterAnalysis2.
/// These tests validate numerical stability, hydrostatic mass positivity,
/// pressure-gradient response, damping behavior, and regime bounds.
/// Status: tested numerically; cluster weighting parameters remain calibrated/phenomenological.
/// </summary>
public class BulletClusterAnalysis2Tests
{
    private readonly ITestOutputHelper _output;

    public BulletClusterAnalysis2Tests(ITestOutputHelper output)
    {
        _output = output;
    }

    [Trait("Category", "PhysicsValidation")]
    [Fact]
    public void HydrostaticMass_Should_Be_Finite_And_Positive_With_DeterministicFixtures()
    {
        var analysis = new BulletClusterAnalysis2();
        var shells = CreateDeterministicShells(
            radiusStartKpc: 100.0,
            radiusStepKpc: 40.0,
            electronDensityBase: 2.0e-3,
            pressureBase: 1.0e-9,
            pressureDeltaPerShell: 2.0e-11,
            reportedMassBase: 8.0e13);

        analysis.CalculateHydrostaticMass(shells);

        for (int i = 1; i < shells.Count - 1; i++)
        {
            double mass = shells[i].CalculatedMass;
            _output.WriteLine($"Hydro mass shell[{i}] : {mass:E}");
            Assert.True(double.IsFinite(mass));
            Assert.True(mass > 0.0);
        }
    }

    [Trait("Category", "PhysicsValidation")]
    [Fact]
    public void UnifiedMass_Should_Respond_To_PressureGradient_And_Damping()
    {
        var analysis = new BulletClusterAnalysis2();

        double rCm = 300.0 * PhysicalConstants.KpcToCm;
        double rho = 2.0e-27;
        double z = 0.3;
        double k = 0.5;

        double dPdrLow = -1.0e-36;   // very calm
        double dPdrHigh = -1.0e-31;  // strongly turbulent

        double massLow = analysis.CalculateMassUnified(rCm, rho, dPdrLow, z, k);
        double massHigh = analysis.CalculateMassUnified(rCm, rho, dPdrHigh, z, k);

        double massNewtonLow = Math.Abs(-(rCm * rCm / (PhysicalConstants.G * rho)) * dPdrLow) / PhysicalConstants.M_Solar;
        double massNewtonHigh = Math.Abs(-(rCm * rCm / (PhysicalConstants.G * rho)) * dPdrHigh) / PhysicalConstants.M_Solar;

        double ratioLow = massLow / massNewtonLow;
        double ratioHigh = massHigh / massNewtonHigh;

        _output.WriteLine($"Unified low-gradient mass : {massLow:E}");
        _output.WriteLine($"Unified high-gradient mass: {massHigh:E}");
        _output.WriteLine($"Low ratio (Unified/Newton): {ratioLow:E}");
        _output.WriteLine($"High ratio(Unified/Newton): {ratioHigh:E}");

        Assert.True(double.IsFinite(massLow) && massLow > 0.0);
        Assert.True(double.IsFinite(massHigh) && massHigh > 0.0);
        Assert.True(double.IsFinite(ratioLow));
        Assert.True(double.IsFinite(ratioHigh));

        // Calm regime should carry stronger TRM coupling than the turbulent regime.
        Assert.True(ratioLow > ratioHigh);

        // Turbulent limit should remain close to Newton.
        Assert.InRange(ratioHigh, 0.95, 1.05);
    }

    [Trait("Category", "PhysicsValidation")]
    [Fact]
    public void ClusterDiagnostics_Should_Be_Deterministic_Finite_And_Bounded()
    {
        var analysis = new BulletClusterAnalysis2();

        var allClusters = new Dictionary<string, List<AcceptShell>>(StringComparer.OrdinalIgnoreCase)
        {
            ["CALM_CLUSTER"] = CreateDeterministicShells(
                radiusStartKpc: 120.0,
                radiusStepKpc: 35.0,
                electronDensityBase: 2.5e-3,
                pressureBase: 9.0e-10,
                pressureDeltaPerShell: 1.0e-11,
                reportedMassBase: 9.0e13),

            ["TURBULENT_CLUSTER"] = CreateDeterministicShells(
                radiusStartKpc: 120.0,
                radiusStepKpc: 35.0,
                electronDensityBase: 2.5e-3,
                pressureBase: 9.0e-10,
                pressureDeltaPerShell: 3.0e-10,
                reportedMassBase: 9.0e13,
                oscillatePressure: true),

            ["MIXED_CLUSTER"] = CreateDeterministicShells(
                radiusStartKpc: 120.0,
                radiusStepKpc: 35.0,
                electronDensityBase: 2.5e-3,
                pressureBase: 9.0e-10,
                pressureDeltaPerShell: 8.0e-11,
                reportedMassBase: 9.0e13)
        };

        var redshifts = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase)
        {
            ["CALM_CLUSTER"] = 0.12,
            ["TURBULENT_CLUSTER"] = 0.28,
            ["MIXED_CLUSTER"] = 0.45
        };

        var diagnostics = analysis.RunClusterDiagnostics(
            allClusters,
            redshifts,
            pressureThreshold: 6.0e-34,
            C: 1.3195,
            alpha: -0.7589,
            beta: 0.6,
            baselineK: 0.1);

        Assert.Equal(3, diagnostics.Count);

        foreach (var d in diagnostics)
        {
            _output.WriteLine($"{d.Name} | regime={d.Regime} | weight={d.Weight:E} | improvement={d.Improvement:E}");

            Assert.True(double.IsFinite(d.Fz));
            Assert.True(double.IsFinite(d.MaxPressureGradient));
            Assert.True(double.IsFinite(d.Improvement));
            Assert.True(double.IsFinite(d.Weight));
            Assert.True(double.IsFinite(d.Turbulence));
            Assert.True(double.IsFinite(d.Shear));
            Assert.True(double.IsFinite(d.Anisotropy));
            Assert.True(double.IsFinite(d.InertialNorm));
            Assert.True(double.IsFinite(d.DynamicFactor));
            Assert.True(double.IsFinite(d.Ellipticity));

            Assert.InRange(d.Weight, 0.0, 1.0);
            Assert.True(d.MaxPressureGradient >= 0.0);
            Assert.True(d.Improvement > 0.0);
            Assert.Contains(d.Regime, new[] { "TRM-dominant", "Mixed", "Newton-dominant" });
        }

        Assert.Contains(diagnostics, d => d.Regime == "TRM-dominant");
        Assert.Contains(diagnostics, d => d.Regime == "Newton-dominant");
    }

    [Trait("Category", "PhysicsValidation")]
    [Fact]
    public void RegimeClassification_Should_Respect_Bounds()
    {
        Assert.Equal("TRM-dominant", BulletClusterAnalysis2.ClassifyRegime(0.71));
        Assert.Equal("Mixed", BulletClusterAnalysis2.ClassifyRegime(0.70));
        Assert.Equal("Mixed", BulletClusterAnalysis2.ClassifyRegime(0.30));
        Assert.Equal("Newton-dominant", BulletClusterAnalysis2.ClassifyRegime(0.29));
    }

    private static List<AcceptShell> CreateDeterministicShells(
        double radiusStartKpc,
        double radiusStepKpc,
        double electronDensityBase,
        double pressureBase,
        double pressureDeltaPerShell,
        double reportedMassBase,
        bool oscillatePressure = false)
    {
        var shells = new List<AcceptShell>();

        for (int i = 0; i < 8; i++)
        {
            double pressure = pressureBase - i * pressureDeltaPerShell;
            if (oscillatePressure)
            {
                pressure += (i % 2 == 0 ? +1.0 : -1.0) * pressureDeltaPerShell * 0.25;
            }

            // keep pressure positive and deterministic
            pressure = Math.Max(pressure, pressureBase * 0.05);

            shells.Add(new AcceptShell
            {
                RadiusKpc = radiusStartKpc + i * radiusStepKpc,
                RadiusKpc_TRM = 0.0,
                ElectronDensity = Math.Max(electronDensityBase * (1.0 - 0.04 * i), 1.0e-6),
                Pressure = pressure,
                ReportedMass = reportedMassBase * (1.0 + 0.03 * i),
                CalculatedMass = 0.0
            });
        }

        return shells;
    }
}
