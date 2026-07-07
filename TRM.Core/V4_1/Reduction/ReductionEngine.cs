namespace TRM.Core.V4_1.Reduction;

/// <summary>
/// Finds minimal mechanism subsets that preserve D=3 advantage.
/// Classification: FRAMEWORK — subset analysis, no claims.
/// </summary>
public static class ReductionEngine
{
    private static readonly Dictionary<string, double> MechanismContributions = new()
    {
        ["spectral"] = 0.35, ["defect"] = 0.30, ["sync"] = 0.25,
        ["isotropy"] = 0.20, ["outer-exponent"] = 0.20,
        ["dispersion"] = 0.15, ["composition"] = 0.15,
        ["causal-cone"] = 0.10,         ["weak-field"] = 0.10, ["d4-favoring"] = 0.05
    };

    // Per-mechanism per-D scores: [D1, D2, D3, D4].
    private static readonly Dictionary<string, double[]> MechanismScores = new()
    {
        ["spectral"] = [0.2, 0.5, 0.8, 0.4],
        ["defect"] = [0.1, 0.4, 0.9, 0.5],
        ["sync"] = [0.3, 0.6, 0.7, 0.4],
        ["isotropy"] = [0.1, 0.3, 0.8, 0.6],
        ["outer-exponent"] = [0.2, 0.5, 0.7, 0.3],
        ["dispersion"] = [0.3, 0.5, 0.6, 0.4],
        ["composition"] = [0.4, 0.5, 0.7, 0.5],
        ["causal-cone"] = [0.3, 0.5, 0.6, 0.5],
        ["weak-field"] = [0.4, 0.5, 0.6, 0.4],
        ["d4-favoring"] = [0.1, 0.2, 0.3, 0.9],
    };

    // Baseline full-system scores.
    private static readonly Dictionary<int, double> BaselineScores = new()
    {
        [1] = 0.20, [2] = 0.55, [3] = 0.85, [4] = 0.45
    };

    public static MinimalCoreResult Evaluate()
    {
        var names = MechanismContributions.Keys.ToList();
        var evaluations = new List<SubsetEvaluationResult>();

        // Evaluate all single-mechanism subsets.
        foreach (var name in names)
            evaluations.Add(EvaluateSubset([name]));

        // Evaluate key pair subsets.
        var top4 = names.Take(4).ToList();
        for (int i = 0; i < top4.Count; i++)
        for (int j = i + 1; j < top4.Count; j++)
            evaluations.Add(EvaluateSubset([top4[i], top4[j]]));

        // Evaluate key triple subsets.
        evaluations.Add(EvaluateSubset([top4[0], top4[1], top4[2]]));
        evaluations.Add(EvaluateSubset([top4[0], top4[1], top4[3]]));
        evaluations.Add(EvaluateSubset([top4[0], top4[2], top4[3]]));

        // Full system.
        evaluations.Add(EvaluateSubset(names));

        // Minimal cores: smallest subsets with D=3 preferred and retention ≥ 0.7.
        var minimalCores = new List<MechanismSubset>();
        foreach (var eval in evaluations
            .Where(e => e.PreferredDim == 3 && e.ScoreRetention >= 0.7)
            .OrderBy(e => e.Subset.Mechanisms.Count))
        {
            bool isMinimal = !evaluations.Any(other =>
                other.Subset.Mechanisms.Count < eval.Subset.Mechanisms.Count &&
                eval.Subset.Mechanisms.All(m => other.Subset.Mechanisms.Contains(m)) &&
                other.PreferredDim == 3 && other.ScoreRetention >= 0.7);
            if (isMinimal) minimalCores.Add(eval.Subset);
        }

        // Critical: mechanisms whose removal causes D=3 loss.
        var critical = new List<string>();
        foreach (var name in names)
        {
            var withoutThis = names.Where(n => n != name).ToList();
            var eval = EvaluateSubset(withoutThis);
            if (eval.PreferredDim != 3 || eval.ScoreRetention < 0.8)
                critical.Add(name);
        }

        var redundant = names.Where(n => !critical.Contains(n)).ToList();

        return new MinimalCoreResult(evaluations, minimalCores, critical, redundant);
    }

    private static SubsetEvaluationResult EvaluateSubset(List<string> mechanisms)
    {
        var scores = new Dictionary<int, double> { [1] = 0, [2] = 0, [3] = 0, [4] = 0 };
        foreach (var m in mechanisms)
        {
            var ms = MechanismScores[m];
            for (int d = 0; d < 4; d++)
                scores[d + 1] += ms[d];
        }
        if (mechanisms.Count > 0)
            for (int d = 1; d <= 4; d++)
                scores[d] /= mechanisms.Count;

        int preferred = scores.MaxBy(kv => kv.Value).Key;
        double retention = scores[3] / BaselineScores[3];
        string status = preferred == 3 && retention >= 0.8 ? "PRESERVED"
            : preferred == 3 ? "WEAKENED" : "LOST";

        return new SubsetEvaluationResult(
            new MechanismSubset($"{mechanisms.Count} mechanisms", mechanisms, scores),
            preferred, scores[3], retention, status);
    }
}
