using TRM.Core.V4_1.Graphs;

namespace TRM.Core.V4_1.Cml;

/// <summary>CML diagnostic utilities. Classification: FRAMEWORK.</summary>
public static class CmlDiagnostics
{
    public static double BridgeBandWidth(
        GraphTopology graph, CmlExperimentConfig config)
    {
        var omegas = new List<double>();
        for (int run = 0; run < config.SeededRuns; run++)
        {
            var rng = new Random(config.BaseSeed + run);
            var result = CoupledMapLatticeSimulator.Run(graph, config, rng);
            omegas.Add(result.CollectiveFrequency);
        }
        if (omegas.Count < 2) return 0;
        double mean = omegas.Average();
        return Math.Sqrt(omegas.Average(o => (o - mean) * (o - mean)));
    }

    public static string ClassifyRegime(double orderParameter)
    {
        if (orderParameter > 0.9) return "synced";
        if (orderParameter > 0.5) return "marginal";
        return "unsynced";
    }
}
