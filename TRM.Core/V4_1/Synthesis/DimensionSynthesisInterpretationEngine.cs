namespace TRM.Core.V4_1.Synthesis;

public static class DimensionSynthesisInterpretationEngine
{
    public static List<DimensionSynthesisInterpretation> Interpret(
        DimensionSynthesisResult result)
    {
        var interps = new List<DimensionSynthesisInterpretation>();

        var d3 = result.Profiles.First(p => p.Dimension == 3);
        int totalLayers = result.LayerEvidence.Count;

        interps.Add(new DimensionSynthesisInterpretation(
            d3.SupportCount > totalLayers / 2
                ? $"D=3 is repeatedly favored across {d3.SupportCount}/{totalLayers} independent diagnostics."
                : $"D=3 receives support from {d3.SupportCount}/{totalLayers} layers — a plurality but not a majority.",
            d3.SupportCount >= 8 ? "SUPPORTED" : d3.SupportCount >= 5 ? "CONDITIONAL" : "INCONCLUSIVE",
            new() { ["d3_support"] = d3.SupportCount, ["total_layers"] = totalLayers }));

        double supportedCount = result.LayerEvidence.Count(e => e.Classification == "SUPPORTED");
        double conditionalCount = result.LayerEvidence.Count(e => e.Classification == "CONDITIONAL");
        interps.Add(new DimensionSynthesisInterpretation(
            $"Evidence quality: {supportedCount} layers SUPPORTED, {conditionalCount} CONDITIONAL — " +
            (conditionalCount > supportedCount
                ? "most evidence remains conditional, preventing a unique dimension-selection claim."
                : "a substantial fraction of layers reach the SUPPORTED level."),
            supportedCount >= 5 ? "SUPPORTED" : supportedCount >= 2 ? "CONDITIONAL" : "INCONCLUSIVE",
            new() { ["supported"] = supportedCount, ["conditional"] = conditionalCount }));

        if (result.ConflictFlags.Count > 0)
            interps.Add(new DimensionSynthesisInterpretation(
                $"Conflicts detected: {string.Join("; ", result.ConflictFlags)}",
                "INCONCLUSIVE",
                new() { ["conflict_count"] = result.ConflictFlags.Count }));

        interps.Add(new DimensionSynthesisInterpretation(
            $"D=3 advantage score: {result.D3AdvantageScore:F2} (1.0 = unanimous).",
            result.D3AdvantageScore > 0.8 ? "SUPPORTED" :
            result.D3AdvantageScore > 0.5 ? "CONDITIONAL" : "INCONCLUSIVE",
            new() { ["d3_advantage"] = result.D3AdvantageScore }));

        return interps;
    }
}
