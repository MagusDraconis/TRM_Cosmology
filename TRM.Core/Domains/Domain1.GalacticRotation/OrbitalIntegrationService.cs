using System;
using System.Collections.Generic;
using System.Text;
using TRM.Core.Shared;

namespace TRM.Core.Domains.Domain1.GalacticRotation;

public static class OrbitalIntegrationService
{
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
