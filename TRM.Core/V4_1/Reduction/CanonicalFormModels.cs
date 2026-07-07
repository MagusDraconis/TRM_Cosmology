namespace TRM.Core.V4_1.Reduction;

// ── Canonical form models ───────────────────────────────────────

public sealed record CanonicalBlock(
    string Name,
    List<string> Terms,
    double Weight);

public sealed record CanonicalEquationForm(
    string Name,
    string Formula,
    List<CanonicalBlock> Blocks);

public sealed record CanonicalFormFitResult(
    CanonicalEquationForm Form,
    Dictionary<int, double> PredictedScores,
    double Rmse,
    double RSquared,
    double InterpretabilityScore,
    int PredictedOptimum,
    string Status);

public sealed record CanonicalFormAuditResult(
    List<CanonicalFormFitResult> Candidates,
    CanonicalFormFitResult Preferred,
    CanonicalFormFitResult BestFit);

public sealed record CanonicalFormInterpretation(
    string Statement,
    string Classification,
    Dictionary<string, double> Metrics);
