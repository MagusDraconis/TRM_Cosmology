using System;
using System.Collections.Generic;
using System.Text;

namespace TRM.QuantumCore.Planck;

/// <summary>
/// Derived physical constants reconstructed from a PlanckConstants tuple.
/// Status: derived + tested (speed-of-light and consistency checks), diagnostic in sensitivity scans.
/// Related tests: TRM.Tests/QuantumTests/PlanckConsistencyTests.cs.
/// Relevant docs: docs/review/TRM_Real_Physics_Test_Coverage.md.
/// </summary>
public class DerivedConstants
{
    private readonly PlanckConstants p;

    public DerivedConstants(PlanckConstants planck)
    {
        p = planck;
    }

    public double SpeedOfLight => p.lP / p.tP;

    public double ReducedPlanck => p.mP * p.lP * p.lP / p.tP;

    public double G => (p.lP * p.lP * p.lP) / (p.mP * p.tP * p.tP);
}
