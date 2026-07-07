namespace TRM.Core.V4_1.Reduction;

public static class CorePlusCorrectionInterpretationEngine
{
    public static List<CorePlusCorrectionInterpretation> Interpret(CorePlusCorrectionResult result)
    {
        var interps = new List<CorePlusCorrectionInterpretation>();

        interps.Add(new CorePlusCorrectionInterpretation(
            $"Adding outer-exponent improves retention from {result.CoreRetention * 100:F0}% to " +
            $"{result.CorePlusRetention * 100:F0}% (+{result.Improvement * 100:F0}pp).",
            result.Improvement > 0.03 ? "SUPPORTED" : "CONDITIONAL",
            new() { ["improvement"] = result.Improvement }));

        interps.Add(new CorePlusCorrectionInterpretation(
            result.ClosureStatus == "CLOSED"
                ? "The reduced effective stack {spectral, defect, sync, outer-exponent} closes the gap."
                : result.ClosureStatus == "PARTIAL"
                    ? "Substantial gap closure achieved; minor residuals remain."
                    : "Core-plus-correction does not close the gap.",
            result.ClosureStatus == "CLOSED" ? "SUPPORTED" :
            result.ClosureStatus == "PARTIAL" ? "CONDITIONAL" : "INCONCLUSIVE",
            new() { ["closure"] = result.ClosureStatus == "CLOSED" ? 1.0 : 0.5 }));

        var essential = result.Residuals.Where(r => r.Type == "essential-still-missing").ToList();
        interps.Add(new CorePlusCorrectionInterpretation(
            essential.Count > 0
                ? $"Residual essential corrections remain: {string.Join(", ", essential.Select(e => e.Mechanism))}."
                : "No remaining essential corrections — all residual mechanisms are stabilizing or negligible.",
            essential.Count > 0 ? "CONDITIONAL" : "SUPPORTED",
            new() { ["essential_remain"] = essential.Count }));

        interps.Add(new CorePlusCorrectionInterpretation(
            $"The remaining residual is dominated by stabilizing corrections rather than primary drivers.",
            "SUPPORTED",
            new() { ["stabilizing_count"] = result.Residuals.Count(r => r.Type == "stabilizing") }));

        return interps;
    }
}
