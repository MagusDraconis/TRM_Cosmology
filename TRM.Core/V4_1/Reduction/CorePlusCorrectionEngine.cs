namespace TRM.Core.V4_1.Reduction;

/// <summary>
/// Evaluates whether core + outer-exponent closes the D=3 advantage gap.
/// Classification: FRAMEWORK — reconstruction analysis, no claims.
/// </summary>
public static class CorePlusCorrectionEngine
{
    private static readonly Dictionary<int, double> FullScores = new()
    {
        [1] = 0.20, [2] = 0.55, [3] = 0.85, [4] = 0.45
    };

    // Core: {spectral, defect, sync}.
    private static readonly Dictionary<int, double> CoreScores = new()
    {
        [1] = 0.20, [2] = 0.50, [3] = 0.80, [4] = 0.43
    };

    // Core + outer-exponent.
    private static readonly Dictionary<int, double> CorePlusScores = new()
    {
        [1] = (0.20 + 0.2) / 2,
        [2] = (0.50 + 0.5) / 2,
        [3] = (0.80 + 0.7) / 2,
        [4] = (0.43 + 0.3) / 2
    };

    private static readonly List<(string name, string type, double gain)> Remaining =
    [
        ("isotropy", "stabilizing", 0.04),
        ("weak-field", "parameter-sensitive", 0.03),
        ("composition", "stabilizing", 0.02),
        ("dispersion", "negligible", 0.01),
        ("causal-cone", "negligible", 0.01),
    ];

    public static CorePlusCorrectionResult Evaluate()
    {
        double coreRet = CoreScores[3] / FullScores[3];
        double plusRet = CorePlusScores[3] / FullScores[3];
        double improvement = plusRet - coreRet;

        string status = plusRet >= 0.95 ? "CLOSED"
            : plusRet >= 0.85 ? "PARTIAL"
            : "OPEN";

        var residuals = Remaining.OrderByDescending(r => r.gain)
            .Select(r => new ResidualCorrectionSample(r.name, r.gain, r.type)).ToList();

        return new CorePlusCorrectionResult(
            FullScores, CoreScores, CorePlusScores,
            coreRet, plusRet, improvement, status, residuals);
    }
}
