namespace TRM.Core.V4_1.Reduction;

public static class CoreReconstructionInterpretationEngine
{
    public static List<CoreReconstructionInterpretation> Interpret(CoreReconstructionResult result)
    {
        var interps = new List<CoreReconstructionInterpretation>();

        interps.Add(new CoreReconstructionInterpretation(
            $"The minimal core reproduces {result.ScoreRetention * 100:F0}% of the full D=3 advantage score " +
            $"(status: {result.ClosureStatus}).",
            result.ClosureStatus == "CLOSED" ? "SUPPORTED" :
            result.ClosureStatus == "PARTIAL" ? "CONDITIONAL" : "INCONCLUSIVE",
            new() { ["retention"] = result.ScoreRetention }));

        var essential = result.Residuals.Where(r => r.CorrectionType == "essential-beyond-core").ToList();
        var stabilizing = result.Residuals.Where(r => r.CorrectionType == "stabilizing").ToList();
        interps.Add(new CoreReconstructionInterpretation(
            essential.Count > 0
                ? $"Some mechanisms ({string.Join(", ", essential.Select(e => e.Mechanism))}) are essential beyond the core."
                : "No omitted mechanisms are essential beyond the core.",
            essential.Count > 0 ? "CONDITIONAL" : "SUPPORTED",
            new() { ["essential_count"] = essential.Count, ["stabilizing_count"] = stabilizing.Count }));

        interps.Add(new CoreReconstructionInterpretation(
            $"Top correction: {result.CorrectionRanking[0]} (gain: {result.Residuals.First(r => r.Mechanism == result.CorrectionRanking[0]).MarginalGain:F2}).",
            "CONDITIONAL",
            new() { ["top_correction_gain"] = result.Residuals.Max(r => r.MarginalGain) }));

        interps.Add(new CoreReconstructionInterpretation(
            result.ClosureStatus == "PARTIAL"
                ? "The full-stack profile cannot be reduced to the minimal core without substantial residual loss."
                : "The minimal core largely captures the full D=3 advantage — remaining mechanisms are stabilizing.",
            result.ClosureStatus == "PARTIAL" ? "CONDITIONAL" : "SUPPORTED",
            new() { ["closure"] = result.ClosureStatus == "CLOSED" ? 1.0 : result.ClosureStatus == "PARTIAL" ? 0.5 : 0 }));

        return interps;
    }
}
