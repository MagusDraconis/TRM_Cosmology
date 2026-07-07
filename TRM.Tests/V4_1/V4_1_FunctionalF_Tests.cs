using Xunit;
using TRM.Core.V4_1;

namespace TRM.Tests.V4_1;

/// <summary>
/// Validates the dimensional selection functional F(D).
/// Maps to: TRM_V4_1_Sync_Stability_Dimension_Selection.md §5.7.
/// Classification: FRAMEWORK — validates computation, not D = 3 claim.
/// </summary>
[Trait("Category", "V4.1")]
[Trait("Category", "V4_1_FunctionalF")]
public class V4_1_FunctionalF_Tests
{
    [Fact]
    public void V4_1_07_DimensionFunctional_ComputesExpectedTerms()
    {
        var obs = new DimensionObservables(
            dimension: 3,
            spectralBalance: 0.4,
            propagationIsotropy: 0.95,
            bridgeBandSharpness: 10.0);

        double F = DimensionFunctional.Compute(obs, a1: 1.0, a2: 1.0, a3: 1.0);
        Assert.Equal(0.4 + 0.95 + 10.0, F, 12);
    }

    [Fact]
    public void V4_1_08_DimensionFunctional_Decompose_MatchesCompute()
    {
        var obs = new DimensionObservables(3, 0.5, 0.8, 5.0);
        var (s, i, h) = DimensionFunctional.Decompose(obs, 2.0, 3.0, 1.0);
        double F = DimensionFunctional.Compute(obs, 2.0, 3.0, 1.0);
        Assert.Equal(s + i + h, F, 12);
    }

    [Fact]
    public void V4_1_09_DimensionFunctional_DefaultWeights_AreUnity()
    {
        var obs = new DimensionObservables(3, 0.3, 0.7, 8.0);
        double F = DimensionFunctional.Compute(obs);
        Assert.Equal(0.3 + 0.7 + 8.0, F, 12);
    }

    [Fact]
    public void V4_1_10_DimensionFunctional_MonotonicInEachTerm()
    {
        var baseObs = new DimensionObservables(3, 0.5, 0.8, 5.0);
        var betterObs = new DimensionObservables(3, 0.6, 0.9, 6.0);
        double FBase = DimensionFunctional.Compute(baseObs);
        double FBetter = DimensionFunctional.Compute(betterObs);
        Assert.True(FBetter > FBase,
            "F(D) should increase when all observables improve.");
    }

    [Fact]
    public void V4_1_11_DimensionFunctional_ZeroWeights_YieldZero()
    {
        var obs = new DimensionObservables(3, 0.5, 0.9, 100.0);
        double F = DimensionFunctional.Compute(obs, 0, 0, 0);
        Assert.Equal(0.0, F);
    }

    [Fact]
    public void V4_1_12_DimensionFunctional_RejectsNegativeWeights()
    {
        var obs = new DimensionObservables(3, 0.5, 0.5, 0.5);
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            DimensionFunctional.Compute(obs, -1, 0, 0));
    }

    // ── Observatory construction guards ─────────────────────────

    [Fact]
    public void V4_1_13_Observables_RejectsNonPositiveSpectralBalance()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new DimensionObservables(3, 0.0, 0.5, 0.5));
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new DimensionObservables(3, -0.1, 0.5, 0.5));
    }

    [Fact]
    public void V4_1_14_Observables_RejectsNonFiniteValues()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new DimensionObservables(3, double.NaN, 0.5, 0.5));
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new DimensionObservables(3, 0.5, double.PositiveInfinity, 0.5));
    }

    [Fact]
    public void V4_1_15_Observables_AcceptsValidValues()
    {
        var obs = new DimensionObservables(3, 0.4, 0.95, 10.0);
        Assert.Equal(3, obs.Dimension);
        Assert.Equal(0.4, obs.SpectralBalance);
        Assert.Equal(0.95, obs.PropagationIsotropy);
        Assert.Equal(10.0, obs.BridgeBandSharpness);
    }
}
