namespace TRM.Core.V4_1;

/// <summary>
/// Evaluates the dimensional selection functional F(D) from raw graph observables.
///
/// F(D) = a₁·S₁ + a₂·S₂ + a₃·S₃
///   S₁ = λ₂ / λ_max
///   S₂ = 1 − σ_θ / ⟨v_θ⟩
///   S₃ = 1 / ΔΩ
///
/// Classification: FRAMEWORK — computation only; does not assert D = 3.
/// </summary>
public static class DimensionSelectionEvaluator
{
    /// <summary>
    /// Evaluates F(D) with user-supplied weights.
    /// All weights must be non-negative.
    /// </summary>
    public static DimensionSelectionResult Evaluate(
        GraphDimensionScenario scenario,
        double a1, double a2, double a3)
    {
        if (a1 < 0 || a2 < 0 || a3 < 0)
            throw new ArgumentOutOfRangeException("Weights must be non-negative.");

        double S1 = scenario.Lambda2 / scenario.LambdaMax;
        double S2 = 1.0 - scenario.SigmaTheta / scenario.MeanVTheta;
        double S3 = 1.0 / scenario.DeltaOmega;

        double F = a1 * S1 + a2 * S2 + a3 * S3;

        return new DimensionSelectionResult(
            scenario.Dimension, S1, S2, S3, F, a1, a2, a3);
    }

    /// <summary>
    /// Evaluates F(D) with equal weights (1, 1, 1). Convenience overload.
    /// </summary>
    public static DimensionSelectionResult Evaluate(GraphDimensionScenario scenario) =>
        Evaluate(scenario, 1.0, 1.0, 1.0);

    /// <summary>
    /// Evaluates F(D) with normalized weights: a1 + a2 + a3 = 1.
    /// If sum is zero, normalisation is skipped (all weights = 0).
    /// </summary>
    public static DimensionSelectionResult EvaluateNormalized(
        GraphDimensionScenario scenario,
        double a1, double a2, double a3)
    {
        if (a1 < 0 || a2 < 0 || a3 < 0)
            throw new ArgumentOutOfRangeException("Weights must be non-negative.");

        double sum = a1 + a2 + a3;
        if (sum > 0)
        {
            a1 /= sum; a2 /= sum; a3 /= sum;
        }
        return Evaluate(scenario, a1, a2, a3);
    }

    /// <summary>
    /// Returns the maximum F(D) value and its dimension from a set of scenarios,
    /// using given weights.
    /// </summary>
    public static (int bestDimension, double bestF) FindBest(
        IReadOnlyList<GraphDimensionScenario> scenarios,
        double a1, double a2, double a3)
    {
        if (scenarios == null || scenarios.Count == 0)
            throw new ArgumentException("At least one scenario required.");

        int bestD = scenarios[0].Dimension;
        double bestF = Evaluate(scenarios[0], a1, a2, a3).FunctionalValue;

        foreach (var s in scenarios.Skip(1))
        {
            double f = Evaluate(s, a1, a2, a3).FunctionalValue;
            if (f > bestF) { bestF = f; bestD = s.Dimension; }
        }
        return (bestD, bestF);
    }
}
