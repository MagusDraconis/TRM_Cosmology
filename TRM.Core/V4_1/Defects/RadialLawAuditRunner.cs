using TRM.Core.V4_1.Graphs;

namespace TRM.Core.V4_1.Defects;

/// <summary>
/// Runs radial-law audits across D = 1..4.
/// Classification: FRAMEWORK — measurement infrastructure, no claims.
/// </summary>
public static class RadialLawAuditRunner
{
    private static GraphTopology BuildGraph(int D, int n) => D switch
    {
        1 => GraphFactory.Chain(n),
        2 => GraphFactory.SquareGrid(n),
        3 => GraphFactory.CubicLattice(n),
        4 => GraphFactory.Hypercubic4D(n),
        _ => throw new ArgumentOutOfRangeException(nameof(D))
    };

    public static List<RadialLawAuditResult> RunAudit(DefectExperimentConfig config)
    {
        var regimeConfig = new RadialRegimeConfig();
        var results = new List<RadialLawAuditResult>();
        for (int D = 1; D <= 4; D++)
        {
            var graph = BuildGraph(D, config.NodesPerDim);
            var regime = RadialRegimeAnalyzer.Analyze(graph, D, config);

            var response = DefectResponseAnalyzer.MeasureDefectResponse(graph, D, config);
            var windowFits = WindowedProfileFitter.FitWindows(response.Profile, regimeConfig);

            results.Add(new RadialLawAuditResult(D, regime, windowFits));
        }
        return results;
    }
}
