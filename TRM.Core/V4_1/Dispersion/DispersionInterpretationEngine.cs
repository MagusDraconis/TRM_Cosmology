namespace TRM.Core.V4_1.Dispersion;

/// <summary>
/// Generates structured interpretations from dispersion audit results.
/// Classification: FRAMEWORK — interpretation only, no claims.
/// </summary>
public static class DispersionInterpretationEngine
{
    public static List<DispersionInterpretation> Interpret(
        List<DirectionalDispersionResult> results)
    {
        var interps = new List<DispersionInterpretation>();

        // Overall linearity.
        double avgError = results.Average(r => r.Fit.FitError);
        double avgSlope = results.Average(r => r.Fit.Slope);
        interps.Add(new DispersionInterpretation(
            avgError < 0.1
                ? "Low-k dispersion is approximately linear within the tested window."
                : "Low-k dispersion shows measurable deviation from linearity.",
            avgError < 0.05 ? "SUPPORTED" : avgError < 0.2 ? "CONDITIONAL" : "INCONCLUSIVE",
            new() { ["avg_fit_error"] = avgError, ["avg_slope"] = avgSlope }));

        // Anisotropy.
        double maxAniso = results.Where(r => r.AnisotropyIndex > 0).Max(r => r.AnisotropyIndex);
        interps.Add(new DispersionInterpretation(
            maxAniso < 0.1
                ? "Directional anisotropy remains small across tested graph families."
                : "Directional anisotropy is measurable under the tested setup.",
            maxAniso < 0.05 ? "SUPPORTED" : maxAniso < 0.2 ? "CONDITIONAL" : "INCONCLUSIVE",
            new() { ["max_anisotropy"] = maxAniso }));

        // D-dependence.
        var byDim = results.GroupBy(r => r.Dimension)
            .ToDictionary(g => g.Key, g => g.Average(r => r.Fit.FitError));
        double errorSpread = byDim.Values.Max() - byDim.Values.Min();
        interps.Add(new DispersionInterpretation(
            errorSpread > 0.05
                ? "Higher-dimensional graphs show broader deviation from linearity."
                : "Dispersion errors are similar across dimensions.",
            errorSpread > 0.1 ? "SUPPORTED" : errorSpread > 0.03 ? "CONDITIONAL" : "INCONCLUSIVE",
            new() { ["error_spread"] = errorSpread }));

        return interps;
    }
}
