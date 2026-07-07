namespace TRM.Core.V4_1.Reduction;

// ── Effective-core equation models ──────────────────────────────

public sealed record EffectiveCoreTerm(
    string Name,
    double Coefficient,
    double StdError);

public sealed record EffectiveCoreEquationFitResult(
    string Formula,
    List<EffectiveCoreTerm> Terms,
    Dictionary<int, double> PredictedScores,
    double Rmse,
    double RSquared,
    int PredictedOptimum,
    string ClosureStatus);

public sealed record EffectiveCoreEquationAuditResult(
    List<EffectiveCoreEquationFitResult> Candidates,
    EffectiveCoreEquationFitResult Best,
    double BestRSquared,
    string OverallClosure);

public sealed record EffectiveCoreEquationInterpretation(
    string Statement,
    string Classification,
    Dictionary<string, double> Metrics);
