namespace TRM.Core.V4_1.Reduction;

// ── Reduction models ────────────────────────────────────────────

public sealed record MechanismSubset(
    string Label,
    List<string> Mechanisms,
    Dictionary<int, double> Scores);

public sealed record SubsetEvaluationResult(
    MechanismSubset Subset,
    int PreferredDim,
    double D3Score,
    double ScoreRetention,
    string Status);

public sealed record MinimalCoreResult(
    List<SubsetEvaluationResult> AllEvaluations,
    List<MechanismSubset> MinimalCores,
    List<string> CriticalMechanisms,
    List<string> RedundantMechanisms);

public sealed record ReductionInterpretation(
    string Statement,
    string Classification,
    Dictionary<string, double> Metrics);
