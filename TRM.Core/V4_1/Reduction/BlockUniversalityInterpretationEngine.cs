namespace TRM.Core.V4_1.Reduction;

public static class BlockUniversalityInterpretationEngine
{
    public static List<BlockUniversalityInterpretation> Interpret(BlockUniversalityResult result)
    {
        var interps = new List<BlockUniversalityInterpretation>();

        int ccFreq = result.FormFrequency.GetValueOrDefault("Core+Correction");
        int total = result.Variants.Count;
        interps.Add(new BlockUniversalityInterpretation(
            $"Core + Correction is preferred in {ccFreq}/{total} variants ({ccFreq * 100 / total}%).",
            ccFreq >= total - 1 ? "SUPPORTED" : ccFreq >= total / 2 ? "CONDITIONAL" : "INCONCLUSIVE",
            new() { ["frequency"] = ccFreq / (double)total }));

        interps.Add(new BlockUniversalityInterpretation(
            result.Stability.CorrectionRemainsSubleading
                ? "The correction block remains subleading under all tested perturbations."
                : "The correction block becomes competitive in some variants.",
            result.Stability.CorrectionRemainsSubleading ? "SUPPORTED" : "CONDITIONAL",
            new() { ["subleading"] = result.Stability.CorrectionRemainsSubleading ? 1 : 0 }));

        interps.Add(new BlockUniversalityInterpretation(
            $"Block weight stability: core σ²={result.Stability.CoreWeightVariance:F3}, " +
            $"correction σ²={result.Stability.CorrectionWeightVariance:F3}.",
            "CONDITIONAL",
            new() { ["core_var"] = result.Stability.CoreWeightVariance,
                     ["correction_var"] = result.Stability.CorrectionWeightVariance }));

        interps.Add(new BlockUniversalityInterpretation(
            result.RobustnessClass == "ROBUST"
                ? "The current canonical structure is robust across the tested variant family."
                : result.RobustnessClass == "WEAKLY-STABLE"
                    ? "The canonical structure is weakly stable — some variants challenge the preferred form."
                    : "The canonical structure is variant-dependent — no single form dominates.",
            result.RobustnessClass == "ROBUST" ? "SUPPORTED" :
            result.RobustnessClass == "WEAKLY-STABLE" ? "CONDITIONAL" : "INCONCLUSIVE",
            new() { ["robustness"] = result.RobustnessClass == "ROBUST" ? 1.0 : 0.5 }));

        return interps;
    }
}
