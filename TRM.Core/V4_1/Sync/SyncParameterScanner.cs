using TRM.Core.V4_1.Graphs;

namespace TRM.Core.V4_1.Sync;

/// <summary>
/// Deterministic parameter scanner over graph families and Kuramoto parameters.
/// Classification: FRAMEWORK — produces diagnostic data, no claims.
/// </summary>
public static class SyncParameterScanner
{
    /// <summary>Classify synchronisation regime from order parameter.</summary>
    public static string ClassifyRegime(double Rfinal)
    {
        if (Rfinal > 0.9) return "synced";
        if (Rfinal > 0.5) return "marginal";
        return "unsynced";
    }

    /// <summary>Compute a combined sync quality score from diagnostics.</summary>
    public static double QualityScore(double Rfinal, double recovery, double bridgeSpread)
    {
        // Higher is better: high R, high recovery, low bridge spread.
        double bridgeTerm = bridgeSpread > 0 ? 1.0 / (1.0 + bridgeSpread) : 1.0;
        return (Rfinal + recovery + bridgeTerm) / 3.0;
    }

    /// <summary>Build the graph for a given dimension and size-per-dim.</summary>
    private static GraphTopology BuildGraph(int D, int n)
    {
        return D switch
        {
            1 => GraphFactory.Chain((int)Math.Pow(n, 1)),
            2 => GraphFactory.SquareGrid(n),
            3 => GraphFactory.CubicLattice(n),
            4 => GraphFactory.Hypercubic4D(n),
            _ => throw new ArgumentOutOfRangeException(nameof(D))
        };
    }

    /// <summary>Execute a full parameter scan.</summary>
    public static List<SyncParameterScanResult> Scan(SyncParameterScanConfig config)
    {
        var results = new List<SyncParameterScanResult>();

        foreach (int D in config.Dimensions)
        {
            var graph = BuildGraph(D, config.GraphSizePerDim);
            int N = graph.NodeCount;

            foreach (double K in config.CouplingStrengths)
            foreach (double spread in config.FrequencySpreads)
            {
                int pointSeed = config.BaseSeed + D * 100 + (int)(K * 1000) + (int)(spread * 1000);
                var point = new SyncParameterPoint(D, K, spread, config.Dt, config.Steps, pointSeed);

                var simCfg = new SyncSimulationConfig(N, K, spread, config.Dt, config.Steps)
                    { SeededRuns = config.SeedsPerPoint, RandomSeed = pointSeed };

                var rng = new Random(pointSeed);
                var omega = SyncDiagnostics.GenerateNaturalFrequencies(N, spread, rng);

                // Single run for R_final
                var rng2 = new Random(pointSeed + 1);
                var init = new double[N];
                for (int i = 0; i < N; i++)
                    init[i] = rng2.NextDouble() * 2 * Math.PI;
                var simResult = KuramotoGraphSimulator.Run(graph, simCfg, omega, init);

                // Perturbation recovery
                double recovery = SyncDiagnostics.PerturbationRecovery(graph, simCfg, omega, N / 2);

                // Bridge-band proxy
                var rng3 = new Random(pointSeed + 2);
                double bridgeSpread = SyncDiagnostics.BridgeBandProxy(graph, simCfg, omega, rng3);

                double quality = QualityScore(simResult.FinalOrderParameter, recovery, bridgeSpread);
                string regime = ClassifyRegime(simResult.FinalOrderParameter);

                results.Add(new SyncParameterScanResult(
                    point, simResult.FinalOrderParameter,
                    recovery, bridgeSpread, quality,
                    simResult.IsSynchronized, regime));
            }
        }
        return results;
    }
}
