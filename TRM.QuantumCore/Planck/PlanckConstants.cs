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
    

}

/// <summary>
/// SI constants used by Planck conversion and related test scenarios.
/// Status: calibrated/defined constants; tested indirectly by Planck consistency tests.
/// </summary>
public static class PhysicalConstantsSI
{

    public const double hbar = 1.054571817e-34;

    public const double G = 6.67430e-11; // SI
    public const double M_Solar = 1.989e30; // kg
    public const double c = 299792458.0; // m/s
    public const double b = 6.9634e8;    // m


    // ✅ NEU: Erde
    public const double M_Earth = 5.972e24;     // kg
    public const double R_Earth = 6.371e6;      // m

    // ✅ optional: typische Orbit-Höhe (low orbit)
    public const double Earth_LowOrbit = R_Earth + 4.0e5;

}