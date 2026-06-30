using System;
using System.Collections.Generic;
using System.Text;

namespace TRM.QuantumCore.Planck;


/// <summary>
/// Container for Planck base constants used by TRM quantum/uncertainty experiments.
/// Status: derived (standard SI-to-Planck relations), tested (PlanckConsistencyTests), limitation (no uncertainty-propagation model here).
/// Related tests: TRM.Tests/QuantumTests/PlanckConsistencyTests.cs.
/// Relevant docs: docs/review/TRM_Service_Test_Consolidation.md and docs/review/TRM_Real_Physics_Test_Coverage.md.
/// </summary>
public class PlanckConstants
{
    public double lP { get; }
    public double tP { get; }
    public double mP { get; }

    public PlanckConstants(double lp, double tp, double mp)
    {
        lP = lp;
        tP = tp;
        mP = mp;
    }



    /// <summary>
    /// Builds Planck constants from SI constants (hbar, G, c).
    /// Status: derived + tested.
    /// </summary>
    public static PlanckConstants FromPhysicalConstants()
    {
        double c = PhysicalConstantsSI.c;
        double hbar = PhysicalConstantsSI.hbar;
        double G = PhysicalConstantsSI.G;

        double lP = Math.Sqrt(hbar * G / Math.Pow(c, 3));
        double tP = lP / c;
        double mP = Math.Sqrt(hbar * c / G);

        return new PlanckConstants(lP, tP, mP);
    }
    /// <summary>
    /// Builds Planck-like constants from intrinsic lattice simulation scales.
    /// <paramref name="latticeSpacing"/> and <paramref name="timeTick"/> define c_sim = spacing / timestep.
    /// <paramref name="energyScale"/> is converted to an effective mass scale via E = m c².
    /// </summary>
    public static PlanckConstants FromSimulation(
        double latticeSpacing,
        double timeTick,
        double energyScale)
    {
        if (latticeSpacing <= 0.0)
            throw new ArgumentOutOfRangeException(nameof(latticeSpacing), "Lattice spacing must be positive.");
        if (timeTick <= 0.0)
            throw new ArgumentOutOfRangeException(nameof(timeTick), "Time tick must be positive.");
        if (energyScale <= 0.0)
            throw new ArgumentOutOfRangeException(nameof(energyScale), "Energy scale must be positive.");

        double cSim = latticeSpacing / timeTick;
        double mPSim = energyScale / (cSim * cSim);

        return new PlanckConstants(latticeSpacing, timeTick, mPSim);
    }

}
