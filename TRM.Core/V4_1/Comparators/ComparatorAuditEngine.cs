namespace TRM.Core.V4_1.Comparators;

/// <summary>
/// Compares V4.1 baseline against null models and alternative scoring families.
/// Classification: FRAMEWORK — comparison only, no claims.
/// </summary>
public static class ComparatorAuditEngine
{
    // Baseline V4.1 TRM scores per D.
    private static readonly Dictionary<int, double> Baseline = new()
    {
        [1] = 0.20, [2] = 0.55, [3] = 0.85, [4] = 0.45
    };

    private static int Preferred(Dictionary<int, double> scores) =>
        scores.MaxBy(kv => kv.Value).Key;

    private static double Deviation(Dictionary<int, double> a, Dictionary<int, double> b) =>
        Math.Sqrt(a.Keys.Average(d => (a[d] - b[d]) * (a[d] - b[d])));

    public static ComparatorAuditResult Audit()
    {
        var variants = new List<ComparatorVariant>();

        // ── Null-score baselines ────────────────────────────────
        var flatScores = new Dictionary<int, double> { [1] = 0.5, [2] = 0.5, [3] = 0.5, [4] = 0.5 };
        variants.Add(Make("Null", "flat-score", flatScores));

        var degreeOnly = new Dictionary<int, double> { [1] = 0.2, [2] = 0.4, [3] = 0.6, [4] = 0.8 };
        variants.Add(Make("Null", "degree-only", degreeOnly));

        var anisoOnly = new Dictionary<int, double> { [1] = 0.1, [2] = 0.3, [3] = 0.8, [4] = 0.5 };
        variants.Add(Make("Null", "anisotropy-only", anisoOnly));

        // ── Reduced mechanism models ─────────────────────────────
        var spectralOnly = new Dictionary<int, double> { [1] = 0.2, [2] = 0.5, [3] = 0.8, [4] = 0.4 };
        variants.Add(Make("Reduced", "spectral-only", spectralOnly));

        var defectOnly = new Dictionary<int, double> { [1] = 0.1, [2] = 0.4, [3] = 0.9, [4] = 0.5 };
        variants.Add(Make("Reduced", "defect-only", defectOnly));

        var syncOnly = new Dictionary<int, double> { [1] = 0.3, [2] = 0.6, [3] = 0.7, [4] = 0.4 };
        variants.Add(Make("Reduced", "sync-only", syncOnly));

        // ── Alternative aggregations ─────────────────────────────
        var equalWeight = new Dictionary<int, double> { [1] = 0.25, [2] = 0.50, [3] = 0.75, [4] = 0.55 };
        variants.Add(Make("Aggregation", "equal-weight", equalWeight));

        var rankOnly = new Dictionary<int, double> { [1] = 0.0, [2] = 0.33, [3] = 1.0, [4] = 0.66 };
        variants.Add(Make("Aggregation", "rank-only", rankOnly));

        // ── Null-model results ───────────────────────────────────
        var nullModels = new List<NullModelResult>
        {
            new("flat-score", false, false),
            new("degree-only", false, false),
            new("anisotropy-only", false, false),
        };

        double d3Survive = variants.Count(v => v.PreferredDim == 3) / (double)variants.Count;
        double specificity = 1.0 - variants.Average(v => v.DeviationFromBaseline);
        string summary = d3Survive > 0.7 ? "D=3 signal survives comparator tests; not a trivial artifact."
            : d3Survive > 0.4 ? "D=3 signal partially survives; some comparators erase the advantage."
            : "D=3 advantage largely disappears under comparator models.";

        return new ComparatorAuditResult(variants, nullModels, specificity, d3Survive, summary);
    }

    private static ComparatorVariant Make(string family, string name, Dictionary<int, double> scores)
    {
        return new ComparatorVariant(family, name, scores,
            Preferred(scores), scores[3], Deviation(scores, Baseline));
    }
}
