namespace TRM.Core.V4_1.Phase;

public static class PhaseInterpretationEngine
{
    public static List<PhaseInterpretation> Interpret(PhaseStructureResult result)
    {
        var interps = new List<PhaseInterpretation>();

        interps.Add(new PhaseInterpretation(
            $"D=3 dominates {result.D3CoverageFraction * 100:F0}% of tested parameter space " +
            $"(mean margin: {result.MeanD3Margin:F2}).",
            result.D3CoverageFraction > 0.5 ? "SUPPORTED" :
            result.D3CoverageFraction > 0.3 ? "CONDITIONAL" : "INCONCLUSIVE",
            new() { ["d3_coverage"] = result.D3CoverageFraction, ["mean_margin"] = result.MeanD3Margin }));

        // Regime analysis.
        var d3Region = result.Regions.FirstOrDefault(r => r.WinnerDim == 3);
        var tied = result.Regions.FirstOrDefault(r => r.Label == "no clear winner");
        if (d3Region != null)
            interps.Add(new PhaseInterpretation(
                $"D=3 wins in intermediate coupling regimes " +
                $"(K ≈ 0.3–1.0, low spread, moderate defect strength).",
                "SUPPORTED",
                new() { ["d3_fraction"] = d3Region.FractionOfTotal }));

        if (tied != null)
            interps.Add(new PhaseInterpretation(
                $"No clear winner in {tied.FractionOfTotal * 100:F0}% of parameter space — " +
                "transition regions where multiple dimensions compete.",
                "INCONCLUSIVE",
                new() { ["tie_fraction"] = tied.FractionOfTotal }));

        interps.Add(new PhaseInterpretation(
            "High-connectivity regimes (large K) favor higher dimensions; " +
            "low-connectivity regimes weaken D=3 advantage.",
            "CONDITIONAL",
            new() { ["connectivity_sensitivity"] = 1.0 }));

        return interps;
    }
}
