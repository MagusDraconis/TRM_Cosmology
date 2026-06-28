using System;
using System.Collections.Generic;
using System.Text;
using TRM.Core.Shared;

namespace TRM.Core.Domains.Domain1.GalacticRotation;

/// <summary>
/// Orbit-integrated TRM acceleration service for SPARC/RAR analyses.
/// Status: tested (via OrbitalIntegratedTests), calibrated (beta and weighting behavior), not derived yet (heuristic correction layer), diagnostic (used for model comparison sweeps).
/// Related tests: TRM.Tests/CoreTests/OrbitalIntegratedTests.cs, TRM.Tests/CoreTests/RarRelationTests.cs.
/// Relevant docs: docs/review/TRM_Service_Test_Consolidation.md and docs/review/TRM_Real_Physics_Test_Coverage.md.
/// </summary>
public static class OrbitalIntegrationService
{
    /// <summary>
    /// Computes orbit-integrated effective acceleration including global drift-state correction.
    /// Status: tested + calibrated.
    /// </summary>
    public static double ComputeIntegratedG(
        List<RarPoint> galaxy,
        double targetRadius,
        double a0)
    {
        if(galaxy == null || galaxy.Count < 3)
            return 0;

        var ordered = galaxy
            .OrderBy(p => p.RadiusKpc)
            .ToList();

        double sum = 0.0;
        double weightSum = 0.0;

        double driftAccum = 0.0;
        double driftWeight = 0.0;

        foreach (var (p1, p2) in ordered.Pairwise())
        {
            if (p2.RadiusKpc > targetRadius)
                break;

            double dr = p2.RadiusKpc - p1.RadiusKpc;
            if(dr <= 0)
                continue;

            if(p1.GbarMs2 <= 0)
                continue;

            double gBase = p1.GbarMs2 + Math.Sqrt(p1.GbarMs2 * a0);

            if(gBase <= 0)
                continue;

            // 🔥 Dynamische Abweichung nur sammeln
            if(p1.GobsMs2 > 0)
            {
                double res = Math.Log10(p1.GobsMs2) - Math.Log10(gBase);

                driftAccum += res * dr;
                driftWeight += dr;
            }

            double weight = 1.0 / Math.Sqrt(gBase + 1e-20);

            sum += gBase * weight * dr;
            weightSum += weight * dr;
        }

        if(weightSum <= 0)
            return 0;

        double gOrbit = sum / weightSum;

        // =========================
        // 🔥 GLOBALER φ-Zustand
        // =========================
        double phi = 0;

        if(driftWeight > 0)
            phi = driftAccum / driftWeight;

        // ✅ stabilisieren
        double phiEff = Math.Tanh(phi);

        double beta = 0.4;

        // ✅ Anwendung NACH Integration
        double gFinal = gOrbit * (1.0 + beta * phiEff);

        return gFinal;
    }


    /// <summary>
    /// Computes orbit-integrated acceleration without post-integration global correction.
    /// Status: tested (baseline comparator in orbit/full/regime tests), diagnostic.
    /// </summary>
    public static double ComputeIntegratedG_OrbitOnly(
    List<RarPoint> galaxy,
    double targetRadius,
    double a0)
    {
        if (galaxy == null || galaxy.Count < 3)
            return 0;

        var ordered = galaxy
            .OrderBy(p => p.RadiusKpc)
            .ToList();

        double sum = 0.0;
        double weightSum = 0.0;

        for (int i = 0; i < ordered.Count - 1; i++)
        {
            var p1 = ordered[i];
            var p2 = ordered[i + 1];

            if (p2.RadiusKpc > targetRadius)
                break;

            double dr = p2.RadiusKpc - p1.RadiusKpc;
            if (dr <= 0)
                continue;

            if (p1.GbarMs2 <= 0)
                continue;

            double gBase = p1.GbarMs2 + Math.Sqrt(p1.GbarMs2 * a0);
            if (gBase <= 0)
                continue;

            double weight = 1.0 / Math.Sqrt(gBase + 1e-20);

            sum += gBase * weight * dr;
            weightSum += weight * dr;
        }

        if (weightSum <= 0)
            return 0;

        return sum / weightSum;
    }
}
