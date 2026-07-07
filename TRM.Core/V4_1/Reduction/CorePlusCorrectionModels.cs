namespace TRM.Core.V4_1.Reduction;

// ── Core+ models ─────────────────────────────────────────────────

public sealed record CorePlusCorrectionResult(
    Dictionary<int, double> FullScores,
    Dictionary<int, double> CoreScores,
    Dictionary<int, double> CorePlusScores,
    double CoreRetention,
    double CorePlusRetention,
    double Improvement,
    string ClosureStatus,
    List<ResidualCorrectionSample> Residuals);

public sealed record ResidualCorrectionSample(
    string Mechanism,
    double MarginalGain,
    string Type);

public sealed record CorePlusCorrectionInterpretation(
    string Statement,
    string Classification,
    Dictionary<string, double> Metrics);
