using TRM.Core.V4_1.Graphs;

namespace TRM.Core.V4_1.Defects;

/// <summary>
/// Analyzes radial response profiles for distinct scaling regimes.
/// Classification: FRAMEWORK — measurement infrastructure, no claims.
/// </summary>
public static class RadialRegimeAnalyzer
{
    public static RadialRegimeResult Analyze(
        GraphTopology graph, int dimension, DefectExperimentConfig config)
    {
        var response = DefectResponseAnalyzer.MeasureDefectResponse(graph, dimension, config);
        var profile = response.Profile;

        var exponentCurve = new List<EffectiveExponentSample>();
        // Compute local effective exponent via adjacent-shell slope.
        for (int i = 0; i < profile.Count - 1; i++)
        {
            double r1 = Math.Max(profile[i].Distance, 1);
            double r2 = Math.Max(profile[i + 1].Distance, 1);
            double a1 = Math.Max(profile[i].MeanAmplitude, 1e-15);
            double a2 = Math.Max(profile[i + 1].MeanAmplitude, 1e-15);
            double p = -(Math.Log(a2) - Math.Log(a1)) / (Math.Log(r2) - Math.Log(r1));
            double error = Math.Abs(profile[i + 1].MeanAmplitude - profile[i].MeanAmplitude);
            exponentCurve.Add(new EffectiveExponentSample(profile[i].Distance, p, error));
        }

        // Inner exponent: average over first N shells.
        int innerCount = Math.Min(config.NodesPerDim / 2, profile.Count);
        double innerExp = exponentCurve.Take(innerCount).Average(e => e.Exponent);

        // Outer exponent: average over last N shells.
        int outerStart = Math.Max(profile.Count / 2, profile.Count - innerCount);
        double outerExp = exponentCurve.Skip(outerStart).Average(e => e.Exponent);

        double crossover = profile.Count > 2
            ? profile[profile.Count / 2].Distance
            : 0;

        string label = Math.Abs(outerExp - 1.0) < 0.5 ? "asymptotic" :
                       Math.Abs(outerExp - innerExp) > 1.0 ? "crossover" : "lattice-dominated";

        return new RadialRegimeResult(dimension, exponentCurve, innerExp, outerExp, crossover, label);
    }
}
