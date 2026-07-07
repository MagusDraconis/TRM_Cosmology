namespace TRM.Core.V4_1.Reduction;

// ── Core reconstruction models ──────────────────────────────────

public sealed record CoreReconstructionResult(
    Dictionary<int, double> FullScores,
    Dictionary<int, double> CoreScores,
    double ScoreRetention,
    double RankAgreement,
    string ClosureStatus,
    List<CoreResidualSample> Residuals,
    List<string> CorrectionRanking);

public sealed record CoreResidualSample(
    string Mechanism,
    double MarginalGain,
    string CorrectionType);

public sealed record CoreReconstructionInterpretation(
    string Statement,
    string Classification,
    Dictionary<string, double> Metrics);
