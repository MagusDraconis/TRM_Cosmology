namespace TRM.Core.V4_1.Reduction;

public static class EffectiveCoreEquationInterpretationEngine
{
    public static List<EffectiveCoreEquationInterpretation> Interpret(
        EffectiveCoreEquationAuditResult result)
    {
        var interps = new List<EffectiveCoreEquationInterpretation>();

        interps.Add(new EffectiveCoreEquationInterpretation(
            $"Best equation: {result.Best.Formula} (R²={result.Best.RSquared:F2}, RMSE={result.Best.Rmse:F3}, closure={result.Best.ClosureStatus}).",
            result.Best.RSquared > 0.85 ? "SUPPORTED" : "CONDITIONAL",
            new() { ["r2"] = result.Best.RSquared }));

        interps.Add(new EffectiveCoreEquationInterpretation(
            result.OverallClosure == "CLOSED"
                ? "A compact reduced equation reproduces the current D=3 profile within tested tolerance."
                : result.OverallClosure == "PARTIAL"
                    ? "The reduced equation captures major structure but leaves systematic residuals."
                    : "No single equation reproduces the full D=3 profile.",
            result.OverallClosure == "CLOSED" ? "SUPPORTED" :
            result.OverallClosure == "PARTIAL" ? "CONDITIONAL" : "INCONCLUSIVE",
            new() { ["closure"] = result.Best.RSquared }));

        interps.Add(new EffectiveCoreEquationInterpretation(
            $"Optimum dimension: D={result.Best.PredictedOptimum}.",
            result.Best.PredictedOptimum == 3 ? "SUPPORTED" : "CONDITIONAL",
            new() { ["optimum"] = result.Best.PredictedOptimum }));

        interps.Add(new EffectiveCoreEquationInterpretation(
            $"Outer-shell behavior enters as a leading correction rather than a primary driver.",
            "CONDITIONAL",
            new() { ["n_candidates"] = result.Candidates.Count }));

        return interps;
    }
}
