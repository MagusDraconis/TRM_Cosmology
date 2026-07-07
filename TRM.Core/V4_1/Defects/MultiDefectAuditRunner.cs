using TRM.Core.V4_1.Graphs;

namespace TRM.Core.V4_1.Defects;

/// <summary>
/// Runs multi-defect superposition audits across D = 1..4.
/// Classification: FRAMEWORK — measurement infrastructure, no claims.
/// </summary>
public static class MultiDefectAuditRunner
{
    private static GraphTopology BuildGraph(int D, int n) => D switch
    {
        1 => GraphFactory.Chain(n),
        2 => GraphFactory.SquareGrid(n),
        3 => GraphFactory.CubicLattice(n),
        4 => GraphFactory.Hypercubic4D(n),
        _ => throw new ArgumentOutOfRangeException(nameof(D))
    };

    public static List<MultiDefectResponseResult> RunAudit(MultiDefectExperimentConfig config)
    {
        var results = new List<MultiDefectResponseResult>();
        for (int D = 1; D <= 4; D++)
        {
            var graph = BuildGraph(D, config.NodesPerDim);
            var result = MultiDefectAnalyzer.MeasureSuperposition(graph, D, config);
            results.Add(result);
        }
        return results;
    }
}
