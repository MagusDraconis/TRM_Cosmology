using TRM.Core.V4_1.Graphs;

namespace TRM.Core.V4_1.Defects;

/// <summary>
/// Runs proto-1PN / nonlinear correction audits across D = 1..4.
/// Classification: FRAMEWORK — measurement infrastructure, no claims.
/// </summary>
public static class NonlinearCorrectionAuditRunner
{
    private static GraphTopology BuildGraph(int D, int n) => D switch
    {
        1 => GraphFactory.Chain(n),
        2 => GraphFactory.SquareGrid(n),
        3 => GraphFactory.CubicLattice(n),
        4 => GraphFactory.Hypercubic4D(n),
        _ => throw new ArgumentOutOfRangeException(nameof(D))
    };

    public static (List<NonlinearCorrectionResult> corrections, List<WeakFieldWindowResult> windows)
        RunAudit(NonlinearCorrectionExperimentConfig config)
    {
        var corrections = new List<NonlinearCorrectionResult>();
        var windows = new List<WeakFieldWindowResult>();
        for (int D = 1; D <= 4; D++)
        {
            var graph = BuildGraph(D, config.NodesPerDim);
            corrections.Add(NonlinearCorrectionAnalyzer.Analyze(graph, D, config));
            windows.Add(NonlinearCorrectionAnalyzer.ComputeWeakFieldWindow(graph, D, config));
        }
        return (corrections, windows);
    }
}
