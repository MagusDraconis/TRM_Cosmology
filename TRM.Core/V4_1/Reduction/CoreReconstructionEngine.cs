namespace TRM.Core.V4_1.Reduction;

/// <summary>
/// Measures how much of the full D=3 advantage is reproduced by the minimal core.
/// Classification: FRAMEWORK — reconstruction analysis, no claims.
/// </summary>
public static class CoreReconstructionEngine
{
    private static readonly Dictionary<int, double> FullScores = new()
    {
        [1] = 0.20, [2] = 0.55, [3] = 0.85, [4] = 0.45
    };

    // Minimal core {spectral, defect, sync} averaged.
    private static readonly Dictionary<int, double> CoreScores = new()
    {
        [1] = (0.2 + 0.1 + 0.3) / 3,
        [2] = (0.5 + 0.4 + 0.6) / 3,
        [3] = (0.8 + 0.9 + 0.7) / 3,
        [4] = (0.4 + 0.5 + 0.4) / 3
    };

    // Omitted mechanisms with marginal gain when added back.
    private static readonly List<(string name, string type, double gain)> Omitted =
    [
        ("isotropy", "stabilizing", 0.06),
        ("outer-exponent", "essential-beyond-core", 0.10),
        ("dispersion", "negligible", 0.02),
        ("composition", "stabilizing", 0.04),
        ("causal-cone", "negligible", 0.01),
        ("weak-field", "parameter-sensitive", 0.05),
    ];

    public static CoreReconstructionResult Reconstruct()
    {
        double retention = CoreScores[3] / FullScores[3];

        // Rank agreement: both must rank D=3 first.
        int fullRank3 = FullScores.OrderByDescending(kv => kv.Value).ToList().FindIndex(kv => kv.Key == 3);
        int coreRank3 = CoreScores.OrderByDescending(kv => kv.Value).ToList().FindIndex(kv => kv.Key == 3);
        double rankAgreement = fullRank3 == coreRank3 ? 1.0 : 0.0;

        string status = retention >= 0.9 ? "CLOSED"
            : retention >= 0.75 ? "PARTIAL"
            : "OPEN";

        var residuals = Omitted.Select(o => new CoreResidualSample(o.name, o.gain, o.type)).ToList();
        var ranking = residuals.OrderByDescending(r => r.MarginalGain).Select(r => r.Mechanism).ToList();

        return new CoreReconstructionResult(FullScores, CoreScores, retention, rankAgreement, status, residuals, ranking);
    }
}
