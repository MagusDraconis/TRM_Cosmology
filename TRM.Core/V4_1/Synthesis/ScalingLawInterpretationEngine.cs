namespace TRM.Core.V4_1.Synthesis;

public static class ScalingLawInterpretationEngine
{
    public static List<ScalingLawInterpretation> Interpret(CompromiseLawResult result)
    {
        var interps = new List<ScalingLawInterpretation>();

        interps.Add(new ScalingLawInterpretation(
            result.IsCompromiseOptimum
                ? "The current evidence is consistent with a compromise law balancing connectivity gain against coherence loss."
                : "A single dominant mechanism explains the D=3 advantage profile.",
            result.IsCompromiseOptimum ? "CONDITIONAL" : "SUPPORTED",
            new() { ["is_compromise"] = result.IsCompromiseOptimum ? 1 : 0 }));

        interps.Add(new ScalingLawInterpretation(
            $"The fitted optimum lies near D={result.BestFit.PredictedOptimumDim} in the tested regime " +
            $"(R²={result.BestFit.RSquared:F2}, RMSE={result.BestFit.Rmse:F3}).",
            result.BestFit.RSquared > 0.7 ? "SUPPORTED" : "CONDITIONAL",
            new() { ["best_opt_D"] = result.BestFit.PredictedOptimumDim, ["r2"] = result.BestFit.RSquared }));

        double sensitivity = result.ParamSensitivityScore;
        interps.Add(new ScalingLawInterpretation(
            sensitivity < 0.3
                ? "The inferred law shows low parameter sensitivity — coefficients are stable."
                : "The inferred law is parameter-sensitive and should be treated as conditional.",
            sensitivity < 0.2 ? "SUPPORTED" : sensitivity < 0.4 ? "CONDITIONAL" : "INCONCLUSIVE",
            new() { ["sensitivity"] = sensitivity }));

        interps.Add(new ScalingLawInterpretation(
            $"Best model: {result.BestFit.ModelName} ({result.BestFit.Formula}).",
            "CONDITIONAL",
            new() { ["n_candidates"] = result.CandidateFits.Count }));

        return interps;
    }
}
