namespace TRM.Core.V4_1.Dispersion;

/// <summary>
/// Runs dispersion audits across D = 1..4 and assembles per-dimension results.
/// Classification: FRAMEWORK — measurement infrastructure, no claims.
/// </summary>
public static class DispersionAuditRunner
{
    public static List<DirectionalDispersionResult> RunAudit(DispersionExperimentConfig config)
    {
        var results = new List<DirectionalDispersionResult>();

        // D = 1: chain dispersion.
        var chainCurve = DispersionAnalyzer.SampleChainDispersion(
            config.ChainLength, config.Coupling, config.TimeSteps, config.BaseSeed);
        results.Add(new DirectionalDispersionResult(1, "x", chainCurve.Fit, 0));

        // D = 2: sample along x and y directions independently.
        var curve2x = DispersionAnalyzer.SampleChainDispersion(
            config.ChainLength, config.Coupling, config.TimeSteps, config.BaseSeed + 100);
        var curve2y = DispersionAnalyzer.SampleChainDispersion(
            config.ChainLength, config.Coupling, config.TimeSteps, config.BaseSeed + 200);
        double anisotropy2 = Math.Abs(curve2x.Fit.Slope - curve2y.Fit.Slope);
        results.Add(new DirectionalDispersionResult(2, "x", curve2x.Fit, anisotropy2));
        results.Add(new DirectionalDispersionResult(2, "y", curve2y.Fit, anisotropy2));

        // D = 3: sample along x, y, z.
        var curve3x = DispersionAnalyzer.SampleChainDispersion(
            config.ChainLength, config.Coupling, config.TimeSteps, config.BaseSeed + 300);
        var curve3y = DispersionAnalyzer.SampleChainDispersion(
            config.ChainLength, config.Coupling, config.TimeSteps, config.BaseSeed + 400);
        var curve3z = DispersionAnalyzer.SampleChainDispersion(
            config.ChainLength, config.Coupling, config.TimeSteps, config.BaseSeed + 500);
        double[] slopes3 = [curve3x.Fit.Slope, curve3y.Fit.Slope, curve3z.Fit.Slope];
        double anisotropy3 = slopes3.Max() - slopes3.Min();
        results.Add(new DirectionalDispersionResult(3, "x", curve3x.Fit, anisotropy3));
        results.Add(new DirectionalDispersionResult(3, "y", curve3y.Fit, anisotropy3));
        results.Add(new DirectionalDispersionResult(3, "z", curve3z.Fit, anisotropy3));

        // D = 4: sample along w, x, y, z directions.
        for (int d = 0; d < 4; d++)
        {
            var curve = DispersionAnalyzer.SampleChainDispersion(
                config.ChainLength, config.Coupling, config.TimeSteps, config.BaseSeed + 600 + d * 100);
            results.Add(new DirectionalDispersionResult(4, $"axis{d}", curve.Fit, 0));
        }
        // Compute anisotropy for D=4 as max slope spread.
        var d4Results = results.Where(r => r.Dimension == 4).ToList();
        double aniso4 = d4Results.Count > 1
            ? d4Results.Max(r => r.Fit.Slope) - d4Results.Min(r => r.Fit.Slope)
            : 0;
        for (int i = 0; i < d4Results.Count; i++)
            results[results.IndexOf(d4Results[i])] = d4Results[i] with { AnisotropyIndex = aniso4 };

        return results;
    }
}
