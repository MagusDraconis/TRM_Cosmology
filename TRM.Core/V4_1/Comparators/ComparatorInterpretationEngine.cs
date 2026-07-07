namespace TRM.Core.V4_1.Comparators;

public static class ComparatorInterpretationEngine
{
    public static List<ComparatorInterpretation> Interpret(ComparatorAuditResult result)
    {
        var interps = new List<ComparatorInterpretation>();

        bool nullReproduces = result.NullModels.Any(n => n.ReproducesD3);
        interps.Add(new ComparatorInterpretation(
            nullReproduces
                ? "At least one null model reproduces D=3 preference — signal may not be unique."
                : "The D=3 preference is not reproduced by simple monotonic null models.",
            nullReproduces ? "INCONCLUSIVE" : "SUPPORTED",
            new() { ["null_reproduces"] = nullReproduces ? 1 : 0 }));

        var reduced = result.Variants.Where(v => v.Family == "Reduced").ToList();
        int reducedD3 = reduced.Count(r => r.PreferredDim == 3);
        interps.Add(new ComparatorInterpretation(
            $"{reducedD3}/{reduced.Count} reduced single-mechanism models recover D=3 preference — " +
            (reducedD3 >= 2 ? "some individual mechanisms carry substantial signal."
                : "most reduced models fail to reproduce the full advantage."),
            reducedD3 >= 2 ? "SUPPORTED" : "CONDITIONAL",
            new() { ["reduced_d3"] = reducedD3, ["reduced_total"] = reduced.Count }));

        interps.Add(new ComparatorInterpretation(
            $"D=3 advantage survives in {result.D3SurvivalFraction * 100:F0}% of comparator models " +
            $"(specificity: {result.SpecificityScore:F2}).",
            result.D3SurvivalFraction > 0.7 ? "SUPPORTED" :
            result.D3SurvivalFraction > 0.4 ? "CONDITIONAL" : "INCONCLUSIVE",
            new() { ["survival"] = result.D3SurvivalFraction, ["specificity"] = result.SpecificityScore }));

        interps.Add(new ComparatorInterpretation(
            result.Summary,
            "CONDITIONAL",
            new() { ["summary_score"] = result.D3SurvivalFraction }));

        return interps;
    }
}
