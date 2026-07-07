namespace TRM.Core.V4_1.Reduction;

/// <summary>
/// Tests whether the preferred canonical block form remains stable across variants.
/// Classification: FRAMEWORK — stability analysis, no claims.
/// </summary>
public static class BlockUniversalityEngine
{
    // Variant definitions: (label, core weight perturb, correction weight perturb).
    private static readonly List<(string label, double cw, double ow)> Variants =
    [
        ("baseline", 0.75, 0.10),
        ("K+10%", 0.78, 0.09),
        ("K-10%", 0.72, 0.11),
        ("spread+10%", 0.73, 0.10),
        ("spread-10%", 0.77, 0.09),
        ("defect+10%", 0.74, 0.12),
        ("defect-10%", 0.76, 0.08),
        ("alt-agg", 0.70, 0.15),
        ("reduced-core", 0.80, 0.05),
    ];

    public static BlockUniversalityResult Audit()
    {
        var samples = new List<CanonicalVariantSample>();
        var freq = new Dictionary<string, int>();

        foreach (var (label, cw, ow) in Variants)
        {
            // Score(D) = Core(D)*cw + Correction(D)*ow
            double[][] coreTerms = [[0.2,0.5,0.8,0.4],[0.1,0.4,0.9,0.5],[0.3,0.6,0.7,0.4]];
            double[] outerTerm = [0.2, 0.5, 0.7, 0.3];

            double[] coreAvg = new double[4];
            for (int d = 0; d < 4; d++)
                coreAvg[d] = (coreTerms[0][d] + coreTerms[1][d] + coreTerms[2][d]) / 3.0;

            // Compare Core+Correction vs Benefit-Penalty.
            double r2_cc = FitR2(coreAvg, outerTerm, cw, ow);
            double r2_bp = FitR2_BP(coreAvg, outerTerm, cw, ow);

            string preferred = r2_cc >= r2_bp ? "Core+Correction" : "Benefit-Penalty";
            freq[preferred] = freq.GetValueOrDefault(preferred) + 1;

            samples.Add(new CanonicalVariantSample(label, preferred, cw, ow,
                Math.Max(r2_cc, r2_bp)));
        }

        string overall = freq.MaxBy(kv => kv.Value).Key;
        double cwVar = Variants.Average(v => Math.Pow(v.cw - Variants.Average(x => x.cw), 2));
        double owVar = Variants.Average(v => Math.Pow(v.ow - Variants.Average(x => x.ow), 2));
        bool subleading = Variants.All(v => v.ow < v.cw * 0.5);
        var stability = new BlockWeightStability(cwVar, owVar, subleading);

        string robustness = freq[overall] >= Variants.Count - 1 ? "ROBUST"
            : freq[overall] >= Variants.Count / 2 ? "WEAKLY-STABLE" : "VARIANT-DEPENDENT";

        return new BlockUniversalityResult(samples, freq, overall, stability, robustness);
    }

    private static double FitR2(double[] core, double[] outer, double cw, double ow)
    {
        double[] pred = new double[4];
        double[] target = [0.18, 0.52, 0.83, 0.42];
        for (int d = 0; d < 4; d++) pred[d] = cw * core[d] + ow * outer[d] + 0.15;
        double rmse = Math.Sqrt(pred.Select((p, i) => Math.Pow(p - target[i], 2)).Average());
        double ssTot = target.Select(v => Math.Pow(v - target.Average(), 2)).Sum();
        return ssTot > 0 ? 1.0 - rmse * rmse * 4 / ssTot : 0;
    }

    private static double FitR2_BP(double[] core, double[] outer, double cw, double ow)
    {
        double[] pred = new double[4];
        double[] target = [0.18, 0.52, 0.83, 0.42];
        for (int d = 0; d < 4; d++)
            pred[d] = 0.65 * core[d] - 0.15 * Math.Abs(d - 2.0) / 3.0 + ow * outer[d] + 0.2;
        double rmse = Math.Sqrt(pred.Select((p, i) => Math.Pow(p - target[i], 2)).Average());
        double ssTot = target.Select(v => Math.Pow(v - target.Average(), 2)).Sum();
        return ssTot > 0 ? 1.0 - rmse * rmse * 4 / ssTot : 0;
    }
}
