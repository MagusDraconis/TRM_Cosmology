namespace TRM.Core;

public static class TrmRadialRegimeModel
{

    // Normalfall: Default-a0 + Default-gamma
    public static double ComputeGobs(
        List<RarPoint> galaxy,
        List<RarPoint> rawGalaxy,
        double targetRadiusKpc)
    {
        return ComputeGobs(
            galaxy,
            rawGalaxy,
            targetRadiusKpc,
            TrmDerivedParameters.GetA0_Ms2(),
            TrmDerivedParameters.GetRegimeGamma()
        );
    }

    // Default-gamma, aber eigenes a0
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
            TrmDerivedParameters.GetRegimeGamma()
        );
    }

    // Default-a0, aber eigenes gamma
    public static double ComputeGobsWithGamma(
        List<RarPoint> galaxy,
        List<RarPoint> rawGalaxy,
        double targetRadiusKpc,
        double gamma)
    {
        return ComputeGobs(
            galaxy,
            rawGalaxy,
            targetRadiusKpc,
            TrmDerivedParameters.GetA0_Ms2(),
            gamma
        );
    }

    public static double ComputeGobs(
        List<RarPoint> galaxy,
        List<RarPoint> rawGalaxy,
        double targetRadiusKpc,
        double a0,
        double gamma)
    {
        if (galaxy == null || galaxy.Count < 3)
            return 0.0;

        if (rawGalaxy == null || rawGalaxy.Count < 3)
            return 0.0;

        var ordered = galaxy
            .OrderBy(p => p.RadiusKpc)
            .ToList();

        double gFull = TrmFullModel.ComputeGobs(
            ordered,
            targetRadiusKpc,
            a0
        );

        if (gFull <= 0 || !double.IsFinite(gFull))
            return 0.0;

        var nearest = ordered
            .OrderBy(p => Math.Abs(p.RadiusKpc - targetRadiusKpc))
            .FirstOrDefault();

        if (nearest == null || nearest.GbarMs2 <= 0)
            return gFull;

        double gLocal = nearest.GbarMs2 + Math.Sqrt(nearest.GbarMs2 * a0);

        if (gLocal <= 0 || !double.IsFinite(gLocal))
            return gFull;

        double rd = EstimateDiskScaleLengthFromProfile(rawGalaxy);
        if (rd <= 0)
            rd = 3.0;

        double x = targetRadiusKpc / rd;
        double innerWeight = TrmDerivedParameters.ComputeInnerWeight(x);

        double ratio = gLocal / gFull;
        ratio = TrmDerivedParameters.ClampLocalToFullRatio(ratio);

        double localContrast = ratio - 1.0;

        double correction = 1.0 + gamma * innerWeight * localContrast;
        correction = TrmDerivedParameters.ClampCorrectionFactor(correction);

        double gFinal = gFull * correction;

        if (!double.IsFinite(gFinal) || gFinal <= 0)
            return 0.0;

        return gFinal;
    }


    private static double EstimateDiskScaleLengthFromProfile(List<RarPoint> galaxyGroup)
    {
        if (galaxyGroup == null || galaxyGroup.Count == 0)
            return 3.0;

        double rMax = galaxyGroup.Max(p => p.RadiusKpc);
        double rd = rMax / 4.5;

        return Math.Clamp(rd, 0.6, 8.0);
    }
}