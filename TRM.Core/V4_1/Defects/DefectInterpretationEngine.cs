namespace TRM.Core.V4_1.Defects;

public sealed record DefectInterpretation(
    string Statement,
    string Classification,
    Dictionary<string, double> Metrics);

public static class DefectInterpretationEngine
{
    public static List<DefectInterpretation> Interpret(
        List<(DefectResponseResult Result, List<ProfileFitResult> Fits)> audit)
    {
        var interps = new List<DefectInterpretation>();

        foreach (var (result, fits) in audit)
        {
            var bestFit = fits.MinBy(f => f.Rmse)!;
            double r2 = bestFit.RSquared;
            interps.Add(new DefectInterpretation(
                r2 > 0.8
                    ? $"D={result.Dimension}: late-time defect response consistent with {bestFit.Model} (R²={r2:F2})."
                    : $"D={result.Dimension}: no single power law clearly preferred (best R²={r2:F2}).",
                r2 > 0.9 ? "SUPPORTED" : r2 > 0.6 ? "CONDITIONAL" : "INCONCLUSIVE",
                new() { ["D"] = result.Dimension, ["R2"] = r2, ["rmse"] = bestFit.Rmse }));
        }

        // Cross-D comparison.
        var bestD = audit.MinBy(a => a.Fits.Min(f => f.Rmse))!;
        interps.Add(new DefectInterpretation(
            $"D={bestD.Result.Dimension} shows best overall fit quality across candidate profiles.",
            "CONDITIONAL",
            new() { ["best_D"] = bestD.Result.Dimension }));

        return interps;
    }
}
