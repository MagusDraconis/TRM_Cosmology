using TRM.Core.V4_1.Graphs;

namespace TRM.Core.V4_1.Cml;

/// <summary>
/// Runs CML dimension audits across D = 1..4 and produces per-D aggregates.
/// Classification: FRAMEWORK — measurement infrastructure, no claims.
/// </summary>
public static class CmlDimensionAuditRunner
{
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

    public static List<CmlDimensionAuditResult> RunAudit(CmlExperimentConfig baseConfig)
    {
        var results = new List<CmlDimensionAuditResult>();

        for (int D = 1; D <= 4; D++)
        {
            var graph = BuildGraph(D, baseConfig.NodesPerDim);
            var config = new CmlExperimentConfig
            {
                Dimension = D,
                NodesPerDim = baseConfig.NodesPerDim,
                CouplingStrength = baseConfig.CouplingStrength,
                TimeSteps = baseConfig.TimeSteps,
                SeededRuns = baseConfig.SeededRuns,
                BaseSeed = baseConfig.BaseSeed + D * 100
            };

            double bridgeWidth = CmlDiagnostics.BridgeBandWidth(graph, config);

            double sumR = 0, sumRec = 0;
            int synced = 0;
            for (int run = 0; run < config.SeededRuns; run++)
            {
                var rng = new Random(config.BaseSeed + run);
                var r = CoupledMapLatticeSimulator.Run(graph, config, rng);
                sumR += r.OrderParameter;
                sumRec += r.PerturbationRecovery;
                if (r.IsSynchronized) synced++;
            }

            results.Add(new CmlDimensionAuditResult
            {
                Dimension = D,
                MeanOrderParameter = sumR / config.SeededRuns,
                MeanRecovery = sumRec / config.SeededRuns,
                BridgeBandWidth = bridgeWidth,
                SyncedCount = synced,
                TotalRuns = config.SeededRuns
            });
        }
        return results;
    }
}
