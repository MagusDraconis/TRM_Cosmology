using TRM.Core;
using TRM.Core.Shared;

public static class TrmFullModel
{

    public static double ComputeGobs(
    List<RarPoint> galaxy,
    double targetRadiusKpc)
    {
        return ComputeGobs(
            galaxy,
            targetRadiusKpc,
            TrmDerivedParameters.GetA0_Ms2()
        );
    }

    public static double ComputeGobs(
                List<RarPoint> galaxy,
                double targetRadiusKpc,
                double a0)
    {
        if (galaxy == null || galaxy.Count < 3)
            return 0.0;

        var ordered = galaxy
            .OrderBy(p => p.RadiusKpc)
            .ToList();

        double sum = 0.0;
        double weightSum = 0.0;

        double driftAccum = 0.0;
        double driftWeight = 0.0;

        double minWeightFloor = TrmDerivedParameters.GetMinimumIntegrationWeight();
        double beta = TrmDerivedParameters.GetPhiBeta();

        for (int i = 0; i < ordered.Count - 1; i++)
        {
            var p1 = ordered[i];
            var p2 = ordered[i + 1];

            if (p2.RadiusKpc > targetRadiusKpc)
                break;

            double dr = p2.RadiusKpc - p1.RadiusKpc;
            if (dr <= 0)
                continue;

            if (p1.GbarMs2 <= 0)
                continue;

            // -----------------------------
            // 1) Lokaler Basiswert
            // -----------------------------
            double gBase = ComputeLocalBaseResponse(p1.GbarMs2, a0);
            if (gBase <= 0 || !double.IsFinite(gBase))
                continue;

            // -----------------------------
            // 2) Dynamische Drift sammeln
            // -----------------------------
            if (p1.GobsMs2 > 0 && double.IsFinite(p1.GobsMs2))
            {
                double residual = Math.Log10(p1.GobsMs2) - Math.Log10(gBase);
                driftAccum += residual * dr;
                driftWeight += dr;
            }

            // -----------------------------
            // 3) Orbit-Gewichtung
            // -----------------------------
            double weight = 1.0 / Math.Sqrt(gBase + minWeightFloor);

            sum += gBase * weight * dr;
            weightSum += weight * dr;
        }

        if (weightSum <= 0)
            return 0.0;

        // -----------------------------
        // 4) Orbit-Mittel
        // -----------------------------
        double gOrbit = sum / weightSum;

        // -----------------------------
        // 5) Globaler Zustand φ
        // -----------------------------
        double phi = 0.0;
        if (driftWeight > 0)
            phi = driftAccum / driftWeight;

        double phiEff = Math.Tanh(phi);

        // -----------------------------
        // 6) Finales Full-Model
        // -----------------------------
        double gFinal = gOrbit * (1.0 + beta * phiEff);

        if (!double.IsFinite(gFinal) || gFinal <= 0)
            return 0.0;

        return gFinal;
    }



    private static double ComputeLocalBaseResponse(double gBarMs2, double a0)
    {
        if (gBarMs2 <= 0 || a0 <= 0)
            return 0.0;

        return gBarMs2 + Math.Sqrt(gBarMs2 * a0);
    }

    
}
