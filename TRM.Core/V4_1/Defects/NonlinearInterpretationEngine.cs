namespace TRM.Core.V4_1.Defects;

public static class NonlinearInterpretationEngine
{
    public static List<NonlinearInterpretation> Interpret(
        List<NonlinearCorrectionResult> corrections,
        List<WeakFieldWindowResult> windows)
    {
        var interps = new List<NonlinearInterpretation>();

        double avgEta = corrections.Average(c => Math.Abs(c.MeanEta));
        interps.Add(new NonlinearInterpretation(
            avgEta < 0.2
                ? "The first nonlinear correction is small, consistent with weak-field linear-superposition dominance."
                : "Nonlinear corrections are measurable across the tested parameter range.",
            avgEta < 0.1 ? "SUPPORTED" : avgEta < 0.3 ? "CONDITIONAL" : "INCONCLUSIVE",
            new() { ["avg_eta"] = avgEta }));

        double avgRatio = windows.Average(w => w.Ratio);
        interps.Add(new NonlinearInterpretation(
            avgRatio < 0.5
                ? "Far-field residual is systematically lower than near-field, supporting a clean weak-field window."
                : "Far-field nonlinear contamination remains comparable to near-field levels.",
            avgRatio < 0.3 ? "SUPPORTED" : avgRatio < 0.7 ? "CONDITIONAL" : "INCONCLUSIVE",
            new() { ["avg_near_far_ratio"] = avgRatio }));

        var best = corrections.MaxBy(c => c.WeakFieldScore)!;
        interps.Add(new NonlinearInterpretation(
            $"D={best.Dimension} shows the cleanest weak-field regime (score={best.WeakFieldScore:F2}).",
            "CONDITIONAL",
            new() { ["best_D"] = best.Dimension, ["score"] = best.WeakFieldScore }));

        return interps;
    }
}
