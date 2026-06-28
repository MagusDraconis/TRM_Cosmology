using System;
using System.Collections.Generic;
using System.Linq;

namespace TRM.Core;

public static class TrmAdaptiveRegimeModel
{
    public static double ComputeGobs(
        List<RarPoint> galaxy,
        List<RarPoint> rawGalaxy,
        double targetRadiusKpc,
        double a0)
    {
        // Default-Basiswert aus eurem Sweep:
        // innen spürbar, außen gegen 0
        return ComputeGobs(galaxy, rawGalaxy, targetRadiusKpc, a0, gamma0: 0.8);
    }

    public static double ComputeGobs(
        List<RarPoint> galaxy,
        List<RarPoint> rawGalaxy,
        double targetRadiusKpc,
        double a0,
        double gamma0)
    {
        if (galaxy == null || galaxy.Count < 3)
            return 0;

        if (rawGalaxy == null || rawGalaxy.Count < 3)
            return 0;

        var ordered = galaxy
            .OrderBy(p => p.RadiusKpc)
            .ToList();

        // -------------------------------------------------
        // 1) Beste aktuelle Basis = FullModel
        // -------------------------------------------------
        double gFull = TrmFullModel.ComputeGobs(
            ordered,
            targetRadiusKpc,
            a0
        );

        if (gFull <= 0 || double.IsNaN(gFull) || double.IsInfinity(gFull))
            return 0;

        // -------------------------------------------------
        // 2) Lokaler Referenzterm
        // -------------------------------------------------
        var nearest = ordered
            .OrderBy(p => Math.Abs(p.RadiusKpc - targetRadiusKpc))
            .FirstOrDefault();

        if (nearest == null || nearest.GbarMs2 <= 0)
            return gFull;

        double gLocal = nearest.GbarMs2 + Math.Sqrt(nearest.GbarMs2 * a0);

        if (gLocal <= 0 || double.IsNaN(gLocal) || double.IsInfinity(gLocal))
            return gFull;

        // -------------------------------------------------
        // 3) Radius / Skalenlänge
        // -------------------------------------------------
        double rd = SparcRarAnalysis.EstimateDiskScaleLengthFromProfile(rawGalaxy);
        if (rd <= 0)
            rd = 3.0;

        double x = targetRadiusKpc / rd;

        // -------------------------------------------------
        // 4) Adaptives gamma(r)
        // -------------------------------------------------
        double gammaEff = ComputeAdaptiveGamma(x, gamma0);

        // -------------------------------------------------
        // 5) Lokaler Kontrast relativ zum FullModel
        // -------------------------------------------------
        double ratio = gLocal / gFull;

        // Begrenzen für Stabilität
        ratio = Math.Clamp(ratio, 0.5, 1.5);

        double localContrast = ratio - 1.0;

        // -------------------------------------------------
        // 6) Korrektur
        // -------------------------------------------------
        double correction = 1.0 + gammaEff * localContrast;

        // Sicherheitsbegrenzung
        correction = Math.Clamp(correction, 0.75, 1.25);

        double gFinal = gFull * correction;

        return gFinal;
    }

    private static double ComputeAdaptiveGamma(double x, double gamma0)
    {
        // x = r / Rd
        //
        // Idee:
        // - innen: gamma ~ gamma0
        // - Übergang: weich fallend
        // - außen: gamma -> 0
        //
        // passend zu eurem Sweep:
        // <1 Rd   -> relevant
        // 1-2 Rd  -> relevant
        // 2-4 Rd  -> kleiner werdend
        // >=4 Rd  -> praktisch 0

        if (x <= 1.0)
            return gamma0;

        if (x >= 4.0)
            return 0.0;

        // weicher kubischer Abfall
        double t = (x - 1.0) / 3.0; // 0..1
        double falloff = 1.0 - (t * t * (3.0 - 2.0 * t)); // smoothstep invertiert

        return gamma0 * Math.Clamp(falloff, 0.0, 1.0);
    }


}