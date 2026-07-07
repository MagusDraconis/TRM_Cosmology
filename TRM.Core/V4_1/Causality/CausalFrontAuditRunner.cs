using TRM.Core.V4_1.Graphs;

namespace TRM.Core.V4_1.Causality;

public sealed record CausalFrontExperimentConfig(
    int NodesPerDim = 6,
    double CouplingK = 0.3,
    int MaxSteps = 200,
    int BaseSeed = 42);

/// <summary>
/// Runs causal-front audits across D = 1..4.
/// Classification: FRAMEWORK — measurement infrastructure, no claims.
/// </summary>
public static class CausalFrontAuditRunner
{
    private static GraphTopology BuildGraph(int D, int n) => D switch
    {
        1 => GraphFactory.Chain(n),
        2 => GraphFactory.SquareGrid(n),
        3 => GraphFactory.CubicLattice(n),
        4 => GraphFactory.Hypercubic4D(n),
        _ => throw new ArgumentOutOfRangeException(nameof(D))
    };

    public static List<CausalConeResult> RunAudit(CausalFrontExperimentConfig config)
    {
        var results = new List<CausalConeResult>();
        for (int D = 1; D <= 4; D++)
        {
            var graph = BuildGraph(D, config.NodesPerDim);
            int source = 0; // corner node as source
            var front = CausalFrontAnalyzer.MeasureFront(
                graph, D, source, config.CouplingK, config.MaxSteps, config.BaseSeed + D * 100);

            // Cone width: std-dev of speed as fraction of mean.
            double coneWidth = front.MeanFrontSpeed > 0
                ? front.SpeedStdDev / front.MeanFrontSpeed
                : 0;

            // Shortest-path residual: avg |arrival - graphDist| / graphDist.
            double residual = 0;
            int count = 0;
            foreach (var a in front.Arrivals)
            {
                if (a.GraphDistance > 0)
                {
                    residual += Math.Abs(a.ArrivalStep - a.GraphDistance) / (double)a.GraphDistance;
                    count++;
                }
            }
            residual = count > 0 ? residual / count : 0;

            results.Add(new CausalConeResult(front, coneWidth, residual));
        }
        return results;
    }
}
