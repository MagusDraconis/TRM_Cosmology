using System;
using System.Collections.Generic;
using System.Linq;

namespace TRM.Core;

public static class TrmDualRegimeModel
{
    public static double ComputeGobs(
        List<RarPoint> galaxy,
        List<RarPoint> rawGalaxy,
        double targetRadiusKpc,
        double a0)
    {
        return ComputeGobs(
            galaxy,
            rawGalaxy,
            targetRadiusKpc,
            a0,
            transitionCenter: 2.0,   // Übergang bei ~2 Rd
            transitionWidth: 1.0     // weiche Übergangsbreite
        );
    }

    public static double ComputeGobs(
        List<RarPoint> galaxy,
        List<RarPoint> rawGalaxy,
        double targetRadiusKpc,
        double a0,
        double transitionCenter,
        double transitionWidth)
    {
        if (galaxy == null || galaxy.Count < 3)
            return 0;

        if (rawGalaxy == null || rawGalaxy.Count < 3)
            return 0;

        var ordered = galaxy
            .OrderBy(p => p.RadiusKpc)
            .ToList();

        // ----------------------------------------
        // 1) Inneres Modell: lokal / massengeprägt
        // ----------------------------------------
        var nearest = ordered
            .OrderBy(p => Math.Abs(p.RadiusKpc - targetRadiusKpc))
            .FirstOrDefault();

        if (nearest == null || nearest.GbarMs2 <= 0)
            return 0;

        double gInner = nearest.GbarMs2 + Math.Sqrt(nearest.GbarMs2 * a0);

        if (gInner <= 0 || double.IsNaN(gInner) || double.IsInfinity(gInner))
            return 0;

        // ----------------------------------------
        // 2) Äußeres Modell: synchronisiert / orbitintegriert
        // ----------------------------------------
        double gOuter = TrmFullModel.ComputeGobs(
            ordered,
            targetRadiusKpc,
            a0
        );

        if (gOuter <= 0 || double.IsNaN(gOuter) || double.IsInfinity(gOuter))
            return 0;

        // ----------------------------------------
        // 3) Radius / Rd
        // ----------------------------------------
        double rd = SparcRarAnalysis.EstimateDiskScaleLengthFromProfile(rawGalaxy);
        if (rd <= 0)
            rd = 3.0;

        double x = targetRadiusKpc / rd;

        // ----------------------------------------
        // 4) Übergangsgewicht
        //    innen -> 1
        //    außen -> 0
        // ----------------------------------------
        double w = ComputeBlendWeight(
            x,
            transitionCenter,
            transitionWidth
        );

        // ----------------------------------------
        // 5) Dual-Regime-Kombination
        // ----------------------------------------
        double gDual = w * gInner + (1.0 - w) * gOuter;

        return gDual;
    }

    private static double ComputeBlendWeight(
        double x,
        double center,
        double width)
    {
        // x = r / Rd
        // center = Übergangszentrum
        // width = Breite des Übergangs

        if (width <= 0)
            width = 1.0;

        // Logistische Übergangsfunktion:
        // innen (x << center) => w ~ 1
        // außen (x >> center) => w ~ 0
        double z = (x - center) / width;

        double w = 1.0 / (1.0 + Math.Exp(2.0 * z));

        return Math.Clamp(w, 0.0, 1.0);
    }

}
