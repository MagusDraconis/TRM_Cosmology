namespace TRM.Core.V4_1.Synthesis;

// ── Scaling-law models ──────────────────────────────────────────

public sealed record ScalingLawTerm(
    string Name,
    double Coefficient,
    double StandardError);

public sealed record ScalingLawFitResult(
    string ModelName,
    string Formula,
    List<ScalingLawTerm> Terms,
    double Rmse,
    double RSquared,
    int PredictedOptimumDim);

public sealed record CompromiseLawResult(
    List<ScalingLawFitResult> CandidateFits,
    ScalingLawFitResult BestFit,
    bool IsCompromiseOptimum,
    double ParamSensitivityScore);

public sealed record ScalingLawInterpretation(
    string Statement,
    string Classification,
    Dictionary<string, double> Metrics);
