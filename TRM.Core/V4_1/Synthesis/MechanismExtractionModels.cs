namespace TRM.Core.V4_1.Synthesis;

// ── Mechanism extraction models ─────────────────────────────────

public sealed record MechanismContributionSample(
    string MechanismName,
    int PreferredDim,
    double ContributionScore,
    double Confidence,
    bool IsParameterSensitive);

public sealed record MechanismTradeoffResult(
    string MechanismA,
    string MechanismB,
    bool IsSynergistic,
    string Note);

public sealed record MechanismExtractionResult(
    List<MechanismContributionSample> Contributions,
    List<MechanismTradeoffResult> Tradeoffs,
    double D3AdvantageFromTop3,
    string WinStyle,
    List<string> FragileMechanisms);

public sealed record MechanismInterpretation(
    string Statement,
    string Classification,
    Dictionary<string, double> Metrics);
