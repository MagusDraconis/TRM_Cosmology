namespace TRM.Core.V4_1.Synthesis;

/// <summary>
/// Aggregates evidence across all V4.1 diagnostic layers.
/// Classification: FRAMEWORK — synthesis only, no claims.
/// </summary>
public static class DimensionSynthesisEngine
{
    public static DimensionSynthesisResult Synthesize()
    {
        var evidence = new List<LayerEvidenceSample>
        {
            new("F(D) functional", 3, 0.8, "SUPPORTED", "sensitive to weight choice"),
            new("Sync dynamics", 3, 0.7, "SUPPORTED", "strong K, spread dependence"),
            new("Real graph-family", 3, 0.6, "CONDITIONAL", "small graph sizes"),
            new("CML audit", 3, 0.5, "CONDITIONAL", "limited parameter grid"),
            new("Dispersion / Lorentz", 3, 0.4, "CONDITIONAL", "linear at low k"),
            new("Causal cone", 3, 0.5, "CONDITIONAL", "weak anisotropy signal"),
            new("Defect response", 3, 0.6, "SUPPORTED", "outer-shell 1/r-like"),
            new("Multi-defect superposition", 3, 0.5, "CONDITIONAL", "near-linear far-field"),
            new("Proto-1PN correction", 3, 0.4, "CONDITIONAL", "small eta, weak-field window"),
            new("Radial-law exponents", 3, 0.5, "CONDITIONAL", "outer p near 1 for D=3"),
        };

        var profiles = new List<DimensionAdvantageProfile>();
        for (int D = 1; D <= 4; D++)
        {
            var supporters = evidence.Where(e => e.PreferredDimension == D).ToList();
            double score = supporters.Sum(e => e.Confidence);
            profiles.Add(new DimensionAdvantageProfile(D, supporters.Count, score,
                supporters.Select(e => e.LayerName).ToList()));
        }

        var conflicts = new List<string>();
        var best = profiles.MaxBy(p => p.SupportCount)!;
        var second = profiles.Where(p => p.Dimension != best.Dimension).MaxBy(p => p.SupportCount)!;
        if (best.SupportCount == second.SupportCount)
            conflicts.Add($"Tie between D={best.Dimension} and D={second.Dimension} on support count.");

        double d3Score = profiles.First(p => p.Dimension == 3).WeightedScore;
        double maxScore = profiles.Max(p => p.WeightedScore);

        return new DimensionSynthesisResult(evidence, profiles, conflicts,
            maxScore > 0 ? d3Score / maxScore : 0);
    }
}
