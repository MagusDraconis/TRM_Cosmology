using Xunit;
using TRM.Core.V4_1;

namespace TRM.Tests.V4_1;

/// <summary>
/// Benchmark-style tests for F(D) dimensional selection.
/// Validates the computation pipeline on controlled synthetic inputs.
/// Does NOT validate physical truth — only correct behaviour of the framework.
///
/// Maps to: TRM_V4_1_Sync_Stability_Dimension_Selection.md §5.7.
/// </summary>
[Trait("Category", "V4.1")]
[Trait("Category", "V4_1_DimensionSelection")]
public class V4_1_DimensionSelection_Benchmark_Tests
{
    // ── Synthetic baseline scenarios ─────────────────────────────
    // Physically ordered: D=1 worst, D=3 best, D=4 slightly worse.
    private static readonly GraphDimensionScenario[] BaselineScenarios =
    [
        // D=1: chain — poor spectral balance, terrible isotropy
        new(1, lambda2: 0.05, lambdaMax: 2.0, sigmaTheta: 0.8, meanVTheta: 1.0, deltaOmega: 1.0),
        // D=2: sheet — moderate
        new(2, lambda2: 0.15, lambdaMax: 4.0, sigmaTheta: 0.3, meanVTheta: 1.0, deltaOmega: 0.4),
        // D=3: cubic — optimal (dominant across all weight regimes)
        new(3, lambda2: 0.50, lambdaMax: 6.0, sigmaTheta: 0.05, meanVTheta: 1.0, deltaOmega: 0.10),
        // D=4: hypercubic — oversaturated
        new(4, lambda2: 0.60, lambdaMax: 8.0, sigmaTheta: 0.08, meanVTheta: 1.0, deltaOmega: 0.35),
    ];

    [Fact]
    public void V4_1_20_FunctionalF_BaselineScenario_PeaksAtD3()
    {
        var (bestD, _) = DimensionSelectionEvaluator.FindBest(BaselineScenarios, 1, 1, 1);
        Assert.Equal(3, bestD);
    }

    [Fact]
    public void V4_1_21_FunctionalF_Sub3D_IsPenalizedByLowSpectralBalance()
    {
        var r1 = DimensionSelectionEvaluator.Evaluate(BaselineScenarios[0]); // D=1
        var r3 = DimensionSelectionEvaluator.Evaluate(BaselineScenarios[2]); // D=3
        Assert.True(r1.SpectralBalance < r3.SpectralBalance,
            $"D=1 S₁={r1.SpectralBalance:F3} should be less than D=3 S₁={r3.SpectralBalance:F3}");
    }

    [Fact]
    public void V4_1_22_FunctionalF_Super3D_IsPenalizedByBroadBridgeBand()
    {
        var r3 = DimensionSelectionEvaluator.Evaluate(BaselineScenarios[2]); // D=3
        var r4 = DimensionSelectionEvaluator.Evaluate(BaselineScenarios[3]); // D=4
        Assert.True(r4.BridgeBandSharpness < r3.BridgeBandSharpness,
            $"D=4 S₃={r4.BridgeBandSharpness:F3} should be less than D=3 S₃={r3.BridgeBandSharpness:F3}");
    }

    [Fact]
    public void V4_1_23_FunctionalF_OrderingMatchesSyntheticPhysicsExpectation()
    {
        double f1 = DimensionSelectionEvaluator.Evaluate(BaselineScenarios[0]).FunctionalValue;
        double f2 = DimensionSelectionEvaluator.Evaluate(BaselineScenarios[1]).FunctionalValue;
        double f3 = DimensionSelectionEvaluator.Evaluate(BaselineScenarios[2]).FunctionalValue;
        double f4 = DimensionSelectionEvaluator.Evaluate(BaselineScenarios[3]).FunctionalValue;

        // D=1 < D=2 < D=3  (monotonic improvement)
        Assert.True(f1 < f2, $"F(1)={f1:F3} should be < F(2)={f2:F3}");
        Assert.True(f2 < f3, $"F(2)={f2:F3} should be < F(3)={f3:F3}");

        // D=4 drops below D=3 (oversaturation penalty)
        Assert.True(f4 < f3, $"F(4)={f4:F3} should be < F(3)={f3:F3}");
    }

    // ── Weight robustness ────────────────────────────────────────

    [Theory]
    [InlineData(1, 0, 0)]
    [InlineData(0, 1, 0)]
    [InlineData(0, 0, 1)]
    [InlineData(0.5, 0.5, 0)]
    [InlineData(0.5, 0, 0.5)]
    [InlineData(0, 0.5, 0.5)]
    [InlineData(1.0/3.0, 1.0/3.0, 1.0/3.0)]
    public void V4_1_24_FunctionalF_NormalizedWeights_AreAccepted(double a1, double a2, double a3)
    {
        var result = DimensionSelectionEvaluator.EvaluateNormalized(
            BaselineScenarios[2], a1, a2, a3);
        Assert.True(result.FunctionalValue >= 0);

        // Normalized weights should sum to ~1 (unless all zero).
        double sum = result.A1 + result.A2 + result.A3;
        if (a1 + a2 + a3 > 0)
            Assert.Equal(1.0, sum, 12);
    }

    [Fact]
    public void V4_1_25_FunctionalF_NegativeWeight_IsRejected()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            DimensionSelectionEvaluator.Evaluate(BaselineScenarios[0], -0.1, 0.5, 0.5));
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            DimensionSelectionEvaluator.EvaluateNormalized(BaselineScenarios[0], 1, -1, 1));
    }

    [Fact]
    public void V4_1_26_FunctionalF_UnnormalizedWeights_NormalizedCorrectly()
    {
        // Raw weights (2, 3, 5) should normalise to (0.2, 0.3, 0.5).
        var result = DimensionSelectionEvaluator.EvaluateNormalized(
            BaselineScenarios[2], 2, 3, 5);
        Assert.Equal(0.2, result.A1, 12);
        Assert.Equal(0.3, result.A2, 12);
        Assert.Equal(0.5, result.A3, 12);
        Assert.Equal(1.0, result.A1 + result.A2 + result.A3, 12);
    }

    [Fact]
    public void V4_1_27_FunctionalF_D3Peak_IsRobustAcrossBaselineWeightGrid()
    {
        double[][] grid =
        [
            [1, 0, 0], [0, 1, 0], [0, 0, 1],
            [0.5, 0.5, 0], [0.5, 0, 0.5], [0, 0.5, 0.5],
            [1.0/3.0, 1.0/3.0, 1.0/3.0],
        ];

        foreach (var w in grid)
        {
            double a1 = w[0], a2 = w[1], a3 = w[2];
            var (bestD, _) = DimensionSelectionEvaluator.FindBest(
                BaselineScenarios, a1, a2, a3);
            Assert.Equal(3, bestD);
        }
    }

    // ── Falsification-oriented tests ─────────────────────────────

    [Fact]
    public void V4_1_28_FunctionalF_CounterexampleScenario_CanDefeatD3()
    {
        // D=4 with artificially narrow deltaOmega can beat baseline D=3.
        var d3 = BaselineScenarios[2];
        var d4Counter = new GraphDimensionScenario(4,
            d3.Lambda2, d3.LambdaMax,
            d3.SigmaTheta, d3.MeanVTheta,
            deltaOmega: 0.05); // narrower than D=3

        double f3 = DimensionSelectionEvaluator.Evaluate(d3).FunctionalValue;
        double f4 = DimensionSelectionEvaluator.Evaluate(d4Counter).FunctionalValue;
        Assert.True(f4 > f3,
            $"Counterexample D=4 F={f4:F3} should exceed D=3 F={f3:F3}.");
    }

    [Fact]
    public void V4_1_29_FunctionalF_Framework_IsFalsifiable()
    {
        // If D=2 artificially beats D=3, the framework reports it — no built-in bias.
        var d2Counter = new GraphDimensionScenario(2,
            lambda2: 0.99, lambdaMax: 1.0,
            sigmaTheta: 0.001, meanVTheta: 1.0,
            deltaOmega: 0.01);

        var d3 = BaselineScenarios[2];
        double f2 = DimensionSelectionEvaluator.Evaluate(d2Counter).FunctionalValue;
        double f3 = DimensionSelectionEvaluator.Evaluate(d3).FunctionalValue;

        Assert.True(f2 > f3,
            $"Falsifiable: D=2 F={f2:F3} should beat D=3 F={f3:F3} given artificial inputs.");
    }

    [Fact]
    public void V4_1_30_FunctionalF_NoBuiltInBiasForD3()
    {
        // Equal observables across all D → F(D) must be identical.
        var obs = new[]
        {
            new GraphDimensionScenario(2, 0.5, 5.0, 0.1, 1.0, 0.5),
            new GraphDimensionScenario(3, 0.5, 5.0, 0.1, 1.0, 0.5),
            new GraphDimensionScenario(4, 0.5, 5.0, 0.1, 1.0, 0.5),
        };

        double[] values = obs.Select(o =>
            DimensionSelectionEvaluator.Evaluate(o).FunctionalValue).ToArray();

        Assert.Equal(values[0], values[1], 12);
        Assert.Equal(values[1], values[2], 12);
    }
}
