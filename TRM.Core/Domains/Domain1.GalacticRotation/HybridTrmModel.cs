

namespace TRM.Core;
public static class HybridTrmModel
{
    public static double ComputeGobs(
        List<RarPoint> galaxy,
        List<RarPoint> rawGalaxy,
        double targetRadiusKpc,
        double a0)
    {
        if (galaxy == null || galaxy.Count < 3)
            return 0;

        if (rawGalaxy == null || rawGalaxy.Count < 3)
            return 0;

        var ordered = galaxy
            .OrderBy(p => p.RadiusKpc)
            .ToList();

        // -----------------------------
        // 1) Lokaler Term
        // -----------------------------
        var nearest = ordered
            .OrderBy(p => Math.Abs(p.RadiusKpc - targetRadiusKpc))
            .FirstOrDefault();

        if (nearest == null || nearest.GbarMs2 <= 0)
            return 0;

        double gLocal = nearest.GbarMs2 + Math.Sqrt(nearest.GbarMs2 * a0);

        if (gLocal <= 0 || double.IsNaN(gLocal) || double.IsInfinity(gLocal))
            return 0;

        // -----------------------------
        // 2) Synchronisations-Term
        // -----------------------------
        double gSync = TrmFullModel.ComputeGobs(
            ordered,
            targetRadiusKpc,
            a0
        );

        if (gSync <= 0 || double.IsNaN(gSync) || double.IsInfinity(gSync))
            return 0;

        // -----------------------------
        // 3) Übergangsgewicht w(r)
        // -----------------------------
        double rd = SparcRarAnalysis.EstimateDiskScaleLengthFromProfile(rawGalaxy);
        if (rd <= 0)
            rd = 3.0;

        double x = targetRadiusKpc / rd;
        double w = ComputeLocalWeight(x);

        // -----------------------------
        // 4) Hybrid
        // -----------------------------
        double gHybrid = w * gLocal + (1.0 - w) * gSync;

        return gHybrid;
    }

    private static double ComputeLocalWeight(double x)
    {
        // x = r / Rd
        // innen lokal dominant, außen Synchronisation dominant

        if (x <= 1.0)
            return 1.0;

        if (x >= 4.0)
            return 0.0;

        // weicher linearer Übergang
        return 1.0 - ((x - 1.0) / 3.0);
    }

}

