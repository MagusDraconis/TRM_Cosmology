using Microsoft.VisualStudio.TestPlatform.Utilities;
using System;
using System.Collections.Generic;
using System.Text;
using TRM.QuantumCore.Planck;
using TRM.Simulations.Experiments;
using Xunit.Abstractions;

namespace TRM.Tests.QuantumTests;

/// <summary>
/// Consistency tests for Planck base/derived constants and scan behavior.
/// Status: tested (core consistency), exploratory/diagnostic (multi-scan export).
/// Related implementation: TRM.QuantumCore/Planck/PlanckConstants.cs and DerivedConstants.cs.
/// Related docs: docs/review/TRM_Real_Physics_Test_Coverage.md.
/// </summary>
public class PlanckConsistencyTests
{
    private readonly ITestOutputHelper _output;
    public PlanckConsistencyTests(ITestOutputHelper output)
    {
        _output = output;
    }

    [Trait("Category", "CoreRegression")]
    [Fact]
    public void PlanckDerivedConstantsMatchReality()
    {
        var p = PlanckConstants.FromPhysicalConstants();
        var d = new DerivedConstants(p);

        _output.WriteLine($"d.SpeedOfLight: {d.SpeedOfLight}");

        Assert.InRange(d.SpeedOfLight, 2.9e8, 3.1e8);

    }
    [Fact]
    public void PlanckDerivedConstantsSensitivity_Test()
    {
        var p = PlanckConstants.FromPhysicalConstants();
        var p2 = new PlanckConstants(
                p.lP * 1.001,
                    p.tP,
                    p.mP

            );
        var d = new DerivedConstants(p);
        var d2 = new DerivedConstants(p2);

        _output.WriteLine($"c vorher: {d.SpeedOfLight}");
        _output.WriteLine($"c nachher: {d2.SpeedOfLight}");

        _output.WriteLine($"ħ vorher: {d.ReducedPlanck}");
        _output.WriteLine($"ħ nachher: {d2.ReducedPlanck}");

        _output.WriteLine($"G vorher: {d.G}");
        _output.WriteLine($"G nachher: {d2.G}");


        Assert.InRange(d.SpeedOfLight, 2.9e8, 3.1e8);
    }

    [Trait("Category", "CoreRegression")]
    [Fact]
    public void PlanckFromSimulation_Should_Propagate_Through_DerivedConstants_And_Match_SI_Baseline()
    {
        var siPlanck = PlanckConstants.FromPhysicalConstants();
        var siDerived = new DerivedConstants(siPlanck);

        double latticeSpacing = siPlanck.lP;
        double timeTick = siPlanck.tP;
        double energyScale = siPlanck.mP * siDerived.SpeedOfLight * siDerived.SpeedOfLight;

        var simPlanck = PlanckConstants.FromSimulation(latticeSpacing, timeTick, energyScale);
        var simDerived = new DerivedConstants(simPlanck);

        double cSim = simDerived.SpeedOfLight;
        double gSim = simDerived.G;

        double cSi = siDerived.SpeedOfLight;
        double gSi = siDerived.G;

        double cRelError = Math.Abs(cSim - cSi) / cSi;
        double gRelError = Math.Abs(gSim - gSi) / gSi;

        _output.WriteLine($"c_sim: {cSim:E12}");
        _output.WriteLine($"c_si : {cSi:E12}");
        _output.WriteLine($"G_sim: {gSim:E12}");
        _output.WriteLine($"G_si : {gSi:E12}");
        _output.WriteLine($"c rel error: {cRelError:E6}");
        _output.WriteLine($"G rel error: {gRelError:E6}");

        Assert.True(cRelError < 1e-12);
        Assert.True(gRelError < 1e-12);
    }

    [Trait("Category", "CoreRegression")]
    [Fact]
    public void PlanckFromSimulation_Should_Report_Emergent_Deltas_Against_SI()
    {
        var siPlanck = PlanckConstants.FromPhysicalConstants();
        var siDerived = new DerivedConstants(siPlanck);

        double latticeSpacing = siPlanck.lP * 1.01;
        double timeTick = siPlanck.tP * 0.995;
        double energyScale = siPlanck.mP * siDerived.SpeedOfLight * siDerived.SpeedOfLight * 0.99;

        var simPlanck = PlanckConstants.FromSimulation(latticeSpacing, timeTick, energyScale);
        var simDerived = new DerivedConstants(simPlanck);

        double cSim = simDerived.SpeedOfLight;
        double gSim = simDerived.G;

        double cSi = siDerived.SpeedOfLight;
        double gSi = siDerived.G;

        double cRelDelta = Math.Abs(cSim - cSi) / cSi;
        double gRelDelta = Math.Abs(gSim - gSi) / gSi;

        _output.WriteLine($"emergent c_sim: {cSim:E12} | c_si: {cSi:E12} | rel delta: {cRelDelta:E6}");
        _output.WriteLine($"emergent G_sim: {gSim:E12} | G_si: {gSi:E12} | rel delta: {gRelDelta:E6}");

        Assert.True(cRelDelta > 0.0);
        Assert.True(gRelDelta > 0.0);
        Assert.True(cRelDelta < 0.05);
        Assert.True(gRelDelta < 0.10);
    }


    [Trait("Category", "LongRunning")]
    [Fact]
    public void PlanckMultiScan_Run()
    {
        var baseP = PlanckConstants.FromPhysicalConstants();
        var scan = new PlanckMultiScan(baseP);

        var results = scan.Run(10000, 0.01); // ±1% Variation

        CsvExporter.Export("planck_scan.csv", results);

        Assert.NotEmpty(results);
    }

}
