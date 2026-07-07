using TRM.Core.V4_1.Sync;

namespace TRM.Core.V4_1.Analysis;

/// <summary>
/// Extracts structured, reviewer-safe conclusions from parameter scan outputs.
/// Classification: FRAMEWORK — analysis only, no claims upgraded.
/// </summary>
public static class DimensionAnalysisEngine
{
    /// <summary>Compute per-dimension aggregates from scan results.</summary>
    public static List<DimensionAnalysisResult> Analyze(List<SyncParameterScanResult> results)
    {
        return results.GroupBy(r => r.Point.Dimension)
            .OrderBy(g => g.Key)
            .Select(g =>
            {
                var list = g.ToList();
                double[] q = list.Select(r => r.SyncQualityScore).ToArray();
                double[] b = list.Select(r => r.BridgeBandSpread).ToArray();
                double[] rec = list.Select(r => r.PerturbationRecovery).ToArray();

                return new DimensionAnalysisResult
                {
                    Dimension = g.Key,
                    MeanQuality = q.Average(),
                    QualityStdDev = Math.Sqrt(q.Average(x => (x - q.Average()) * (x - q.Average()))),
                    MeanBridgeBand = b.Average(),
                    MeanRecovery = rec.Average(),
                    SyncedCount = list.Count(r => r.Regime == "synced"),
                    MarginalCount = list.Count(r => r.Regime == "marginal"),
                    UnsyncedCount = list.Count(r => r.Regime == "unsynced"),
                    TotalPoints = list.Count
                };
            }).ToList();
    }

    /// <summary>Pairwise comparisons between dimensions.</summary>
    public static List<DimensionComparisonSummary> Compare(List<DimensionAnalysisResult> results)
    {
        var comparisons = new List<DimensionComparisonSummary>();
        for (int i = 0; i < results.Count; i++)
        for (int j = i + 1; j < results.Count; j++)
        {
            var a = results[i]; var b = results[j];
            var stmts = new List<AnalysisStatement>();
            double qDiff = a.MeanQuality - b.MeanQuality;
            double bbDiff = a.MeanBridgeBand - b.MeanBridgeBand;

            if (Math.Abs(qDiff) > 0.01)
            {
                string direction = qDiff > 0 ? "higher than" : "lower than";
                stmts.Add(new AnalysisStatement
                {
                    Text = $"D={a.Dimension} mean quality ({a.MeanQuality:F3}) is {qDiff:F3} {direction} D={b.Dimension} ({b.MeanQuality:F3}).",
                    Confidence = Math.Abs(qDiff) > 0.05 ? AnalysisConfidence.Supported : AnalysisConfidence.Conditional,
                    Metrics = new() { ["quality_diff"] = qDiff, ["D_a"] = a.Dimension, ["D_b"] = b.Dimension }
                });
            }
            if (Math.Abs(bbDiff) > 0.001)
                stmts.Add(new AnalysisStatement
                {
                    Text = $"D={a.Dimension} bridge-band spread differs from D={b.Dimension} by {bbDiff:E3}.",
                    Confidence = Math.Abs(bbDiff) > 0.01 ? AnalysisConfidence.Supported : AnalysisConfidence.Conditional,
                    Metrics = new() { ["bridge_diff"] = bbDiff }
                });

            comparisons.Add(new DimensionComparisonSummary
            {
                DimensionA = a.Dimension, DimensionB = b.Dimension,
                MeanQualityDiff = qDiff, BridgeBandDiff = bbDiff, Statements = stmts
            });
        }
        return comparisons;
    }

    /// <summary>Generate top-level structured conclusions from analysis results.</summary>
    public static List<AnalysisStatement> GenerateConclusions(List<DimensionAnalysisResult> results)
    {
        var stmts = new List<AnalysisStatement>();
        if (results.Count == 0) return stmts;

        // Best mean quality
        var best = results.MaxBy(r => r.MeanQuality)!;
        var second = results.Where(r => r.Dimension != best.Dimension).MaxBy(r => r.MeanQuality);
        double margin = second != null ? best.MeanQuality - second.MeanQuality : 0;
        stmts.Add(new AnalysisStatement
        {
            Text = $"D={best.Dimension} shows highest mean sync quality ({best.MeanQuality:F3}) across tested grid.",
            Confidence = margin > 0.03 ? AnalysisConfidence.Supported : AnalysisConfidence.Conditional,
            Metrics = new() { ["best_D"] = best.Dimension, ["best_Q"] = best.MeanQuality, ["margin"] = margin }
        });

        // Narrowest bridge band
        var narrowest = results.MinBy(r => r.MeanBridgeBand)!;
        stmts.Add(new AnalysisStatement
        {
            Text = $"D={narrowest.Dimension} shows narrowest bridge-band spread ({narrowest.MeanBridgeBand:E3}).",
            Confidence = AnalysisConfidence.Conditional,
            Metrics = new() { ["narrowest_D"] = narrowest.Dimension, ["narrowest_bridge"] = narrowest.MeanBridgeBand }
        });

        // Parameter sensitivity
        double maxSpread = results.Max(r => r.QualityStdDev);
        stmts.Add(new AnalysisStatement
        {
            Text = "Results depend sensitively on coupling K and frequency spread values.",
            Confidence = maxSpread > 0.05 ? AnalysisConfidence.Supported : AnalysisConfidence.Conditional,
            Metrics = new() { ["max_stddev"] = maxSpread }
        });

        // Flat check
        if (results.All(r => Math.Abs(r.MeanQuality - results[0].MeanQuality) < 0.01))
            stmts.Add(new AnalysisStatement
            {
                Text = "No dimension clearly preferred — data are flat across tested grid.",
                Confidence = AnalysisConfidence.Inconclusive,
                Metrics = new() { ["flatness"] = results.Max(r => r.QualityStdDev) }
            });

        return stmts;
    }
}
