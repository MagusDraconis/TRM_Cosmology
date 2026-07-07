namespace TRM.Core.V4_1.Defects;

public static class SuperpositionInterpretationEngine
{
    public static List<SuperpositionInterpretation> Interpret(List<MultiDefectResponseResult> results)
    {
        var interps = new List<SuperpositionInterpretation>();

        double avgFarResidual = results.Average(r => r.FarFieldResidual);
        interps.Add(new SuperpositionInterpretation(
            avgFarResidual < 0.1
                ? "Weak-field defect responses combine approximately linearly in the outer-shell regime."
                : "Far-field superposition shows measurable nonlinear cross-terms.",
            avgFarResidual < 0.05 ? "SUPPORTED" : avgFarResidual < 0.2 ? "CONDITIONAL" : "INCONCLUSIVE",
            new() { ["avg_far_residual"] = avgFarResidual }));

        double avgMaxRes = results.Average(r => r.MaxResidual);
        interps.Add(new SuperpositionInterpretation(
            avgMaxRes < 0.5
                ? "Nonlinear cross-terms remain localized near defect cores."
                : "Nonlinear effects extend beyond defect core regions.",
            avgMaxRes < 0.2 ? "SUPPORTED" : avgMaxRes < 0.5 ? "CONDITIONAL" : "INCONCLUSIVE",
            new() { ["avg_max_residual"] = avgMaxRes }));

        // Per-D far-field comparison.
        var bestFar = results.MinBy(r => r.FarFieldResidual)!;
        interps.Add(new SuperpositionInterpretation(
            $"D={bestFar.Dimension} shows lowest far-field composition error ({bestFar.FarFieldResidual:E3}).",
            "CONDITIONAL",
            new() { ["best_D"] = bestFar.Dimension, ["far_residual"] = bestFar.FarFieldResidual }));

        return interps;
    }
}
