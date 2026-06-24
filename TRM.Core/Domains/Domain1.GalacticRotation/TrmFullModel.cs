using TRM.Core;

public static class TrmFullModel
{
    public static double ComputeGobs(
        List<RarPoint> galaxy,
        double targetRadius,
        double a0)
    {
        if(galaxy == null || galaxy.Count < 3)
            return 0;

        var ordered = galaxy.OrderBy(p => p.RadiusKpc).ToList();

        double sum = 0;
        double weightSum = 0;

        double driftAccum = 0;
        double driftWeight = 0;


        for(int i = 0; i < ordered.Count - 1; i++)
        {
            var p1 = ordered[i];
            var p2 = ordered[i + 1];

            if(p2.RadiusKpc > targetRadius)
                break;

            double dr = p2.RadiusKpc - p1.RadiusKpc;
            if(dr <= 0 || p1.GbarMs2 <= 0)
                continue;

            double gBase = p1.GbarMs2 + Math.Sqrt(p1.GbarMs2 * a0);

            if(gBase <= 0)
                continue;

            // 🔹 Dynamische Abweichung sammeln
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

        // 🔹 globaler Zustand φ
        double phi = (driftWeight > 0) ? driftAccum / driftWeight : 0;
        double phiEff = Math.Tanh(phi);

        double beta = 0.4;

        return gOrbit * (1.0 + beta * phiEff);
    }
}
public static class EnumerableExtensions
{
    public static IEnumerable<(T First, T Second)> Pairwise<T>(this IList<T> source)
    {
        if(source == null || source.Count < 2)
            yield break;

        for(int i = 0; i < source.Count - 1; i++)
        {
            yield return (source[i], source[i + 1]);
        }
    }
}