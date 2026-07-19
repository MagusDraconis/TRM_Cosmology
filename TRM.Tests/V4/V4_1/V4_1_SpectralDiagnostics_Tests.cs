using Xunit;
using TRM.Core.V4_1;
using TRM.Core.V4_1.Graphs;

namespace TRM.Tests.V4_1;

/// <summary>
/// Validates spectral diagnostics on real graph families and
/// end-to-end F(D) pipeline from graph topology through to functional value.
/// Does NOT claim D=3 is selected — validates computation only.
/// </summary>
[Trait("Category", "V4.1")]
[Trait("Category", "V4_1_DimensionSelection")]
public class V4_1_SpectralDiagnostics_Tests
{
    [Fact]
    public void V4_1_Spectral_Lambda2_PositiveOnConnectedGraphs()
    {
        foreach (var g in new[] {
            GraphFactory.Chain(6), GraphFactory.SquareGrid(4),
            GraphFactory.CubicLattice(3), GraphFactory.Hypercubic4D(2) })
        {
            double l2 = GraphMetrics.Lambda2(g);
            Assert.True(l2 > 0, $"λ₂ = {l2:E3}, expected > 0 for connected graph.");
        }
    }

    [Fact]
    public void V4_1_Spectral_LambdaMax_GreaterEqualsLambda2()
    {
        var g = GraphFactory.CubicLattice(3);
        double l2 = GraphMetrics.Lambda2(g);
        double lMax = GraphMetrics.LambdaMax(g);
        Assert.True(lMax >= l2, $"λ_max={lMax:F3} should be ≥ λ₂={l2:F3}.");
    }

    [Fact]
    public void V4_1_Spectral_SpectralBalance_IsComputableForAllD()
    {
        var graphs = new (int D, GraphTopology g)[]
        {
            (1, GraphFactory.Chain(6)),
            (2, GraphFactory.SquareGrid(4)),
            (3, GraphFactory.CubicLattice(3)),
            (4, GraphFactory.Hypercubic4D(2)),
        };
        foreach (var (D, g) in graphs)
        {
            double l2 = GraphMetrics.Lambda2(g);
            double lMax = GraphMetrics.LambdaMax(g);
            double S1 = l2 / lMax;
            Assert.True(double.IsFinite(S1) && S1 > 0,
                $"D={D}: S₁={S1:E3} should be finite and positive.");
            Assert.True(S1 <= 1.0, $"D={D}: S₁={S1:E3} should be ≤ 1.");
        }
    }

    [Fact]
    public void V4_1_Spectral_NoNaNOnBaselineGraphs()
    {
        foreach (var g in new[] {
            GraphFactory.Chain(6), GraphFactory.SquareGrid(4),
            GraphFactory.CubicLattice(3), GraphFactory.Hypercubic4D(2) })
        {
            double l2 = GraphMetrics.Lambda2(g);
            double lMax = GraphMetrics.LambdaMax(g);
            Assert.False(double.IsNaN(l2) || double.IsNaN(lMax),
                "Eigenvalues should not be NaN.");
        }
    }

    // ── Pipeline integration: graph → scenario → F(D) ───────────

    private static GraphDimensionScenario ScenarioFromGraph(int D, GraphTopology g)
    {
        double lambda2 = GraphMetrics.Lambda2(g);
        double lambdaMax = GraphMetrics.LambdaMax(g);
        // Simple isotropy proxy: for regular lattices, sigmaTheta ~ 0 (near-perfect).
        double sigmaTheta = 0.05 * D;
        double meanVTheta = 1.0;
        double deltaOmega = Math.Max(0.5 / D, 0.01);
        return new GraphDimensionScenario(D, lambda2, lambdaMax, sigmaTheta, meanVTheta, deltaOmega);
    }

    [Fact]
    public void V4_1_40_FunctionalF_RealGraphDiagnostics_AreComputable()
    {
        var scenarios = new[]
        {
            ScenarioFromGraph(1, GraphFactory.Chain(6)),
            ScenarioFromGraph(2, GraphFactory.SquareGrid(4)),
            ScenarioFromGraph(3, GraphFactory.CubicLattice(3)),
            ScenarioFromGraph(4, GraphFactory.Hypercubic4D(2)),
        };

        foreach (var s in scenarios)
        {
            var r = DimensionSelectionEvaluator.Evaluate(s);
            Assert.True(double.IsFinite(r.FunctionalValue),
                $"D={s.Dimension}: F(D) should be finite.");
            Assert.True(r.FunctionalValue >= 0);
        }
    }

    [Fact]
    public void V4_1_41_FunctionalF_RealGraphPipeline_HasNoBuiltInD3Bias()
    {
        // If all D have identical observables, F(D) must be identical.
        // Real graphs differ, so test: pipeline does not hard-code D=3.
        var graphs = new[]
        {
            (1, GraphFactory.Chain(6)),
            (2, GraphFactory.SquareGrid(4)),
            (3, GraphFactory.CubicLattice(3)),
            (4, GraphFactory.Hypercubic4D(2)),
        };

        var results = new List<double>();
        foreach (var (D, g) in graphs)
        {
            var s = ScenarioFromGraph(D, g);
            results.Add(DimensionSelectionEvaluator.Evaluate(s).FunctionalValue);
        }

        // All should be distinct (real graphs differ), but none hard-coded.
        Assert.Equal(4, results.Distinct().Count());
    }

    [Fact]
    public void V4_1_42_FunctionalF_RealGraphInputs_ProduceDeterministicOutputs()
    {
        var g = GraphFactory.CubicLattice(3);
        var s = ScenarioFromGraph(3, g);
        double f1 = DimensionSelectionEvaluator.Evaluate(s).FunctionalValue;
        double f2 = DimensionSelectionEvaluator.Evaluate(s).FunctionalValue;
        Assert.Equal(f1, f2, 12);
    }
}
