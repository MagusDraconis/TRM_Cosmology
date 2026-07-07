namespace TRM.Core.V4_1.Causality;

public sealed record CausalConeInterpretation(
    string Statement,
    string Classification,
    Dictionary<string, double> Metrics);

public static class CausalConeInterpretationEngine
{
    public static List<CausalConeInterpretation> Interpret(List<CausalConeResult> results)
    {
        var interps = new List<CausalConeInterpretation>();

        double avgResidual = results.Average(r => r.ShortestPathResidual);
        interps.Add(new CausalConeInterpretation(
            avgResidual < 5
                ? "Arrival times remain close to shortest-path scaling."
                : "Arrival times show measurable deviation from shortest-path scaling.",
            avgResidual < 2 ? "SUPPORTED" : avgResidual < 10 ? "CONDITIONAL" : "INCONCLUSIVE",
            new() { ["avg_residual"] = avgResidual }));

        double avgWidth = results.Average(r => r.ConeWidth);
        interps.Add(new CausalConeInterpretation(
            avgWidth < 0.3
                ? "Front propagation follows a narrow causal cone."
                : "Front propagation shows measurable broadening.",
            avgWidth < 0.15 ? "SUPPORTED" : avgWidth < 0.5 ? "CONDITIONAL" : "INCONCLUSIVE",
            new() { ["avg_cone_width"] = avgWidth }));

        double maxAniso = results.Max(r => r.Result.AnisotropyIndex);
        interps.Add(new CausalConeInterpretation(
            maxAniso < 0.1
                ? "Directional anisotropy of front propagation is small."
                : "Front propagation shows measurable anisotropy across directions.",
            maxAniso < 0.05 ? "SUPPORTED" : maxAniso < 0.2 ? "CONDITIONAL" : "INCONCLUSIVE",
            new() { ["max_anisotropy"] = maxAniso }));

        return interps;
    }
}
