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
