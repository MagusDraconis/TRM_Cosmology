using TRM.Core.V4_1.Graphs;

namespace TRM.Core.V4_1.Defects;

/// <summary>
/// Runs defect-response audits across D = 1..4.
/// Classification: FRAMEWORK — measurement infrastructure, no claims.
/// </summary>
public static class DefectAuditRunner
{
    private static GraphTopology BuildGraph(int D, int n) => D switch
    {
        1 => GraphFactory.Chain(n),
        2 => GraphFactory.SquareGrid(n),
        3 => GraphFactory.CubicLattice(n),
        4 => GraphFactory.Hypercubic4D(n),
        _ => throw new ArgumentOutOfRangeException(nameof(D))
    };

    public static List<(DefectResponseResult Result, List<ProfileFitResult> Fits)> RunAudit(
        DefectExperimentConfig config)
    {
        var results = new List<(DefectResponseResult, List<ProfileFitResult>)>();
        for (int D = 1; D <= 4; D++)
        {
            var graph = BuildGraph(D, config.NodesPerDim);
            var response = DefectResponseAnalyzer.MeasureDefectResponse(graph, D, config);

            var fits = new List<ProfileFitResult>
            {
                RadialProfileFitter.FitPowerLaw(response.Profile, 1.0),
                RadialProfileFitter.FitPowerLaw(response.Profile, 2.0),
                RadialProfileFitter.FitExponentialCutoff(response.Profile)
            };

            results.Add((response, fits));
        }
        return results;
    }
}
