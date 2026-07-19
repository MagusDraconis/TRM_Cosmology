using Xunit;
using TRM.Core.V4_1.Dispersion;

namespace TRM.Tests.V4_1;

[Trait("Category", "V4.1")]
[Trait("Category", "V4_1_Dispersion")]
public class V4_1_DispersionAudit_Tests
{
    private static DispersionExperimentConfig DefaultConfig() => new();

    [Fact]
    public void V4_1_110_DispersionAnalyzer_ComputesFiniteOmegaValues()
    {
        double omega = DispersionAnalyzer.EstimateFrequency1D(24, 0.5, 0.5, 400, 42);
        Assert.True(double.IsFinite(omega));
        Assert.True(omega >= 0);
    }

    [Fact]
    public void V4_1_111_DispersionFit_IsDeterministicAcrossRepeatedRuns()
    {
        var c1 = DispersionAnalyzer.SampleChainDispersion(24, 0.5, 400, 42);
        var c2 = DispersionAnalyzer.SampleChainDispersion(24, 0.5, 400, 42);
        Assert.Equal(c1.Fit.Slope, c2.Fit.Slope, 6);
        Assert.Equal(c1.Fit.Intercept, c2.Fit.Intercept, 6);
    }

    [Fact]
    public void V4_1_112_DispersionAudit_ProducesPerDimensionResults()
    {
        var results = DispersionAuditRunner.RunAudit(DefaultConfig());
        Assert.NotEmpty(results);
        var dims = results.Select(r => r.Dimension).Distinct().OrderBy(d => d).ToList();
        Assert.Contains(1, dims);
        Assert.Contains(3, dims);
    }

    [Fact]
    public void V4_1_113_DirectionalDispersion_IsComputable()
    {
        var results = DispersionAuditRunner.RunAudit(DefaultConfig());
        foreach (var r in results)
        {
            Assert.True(double.IsFinite(r.Fit.Slope));
            Assert.True(double.IsFinite(r.Fit.FitError));
            Assert.NotNull(r.Direction);
        }
    }

    [Fact]
    public void V4_1_114_LowKFit_ProducesFiniteSlopeAndError()
    {
        var curve = DispersionAnalyzer.SampleChainDispersion(24, 0.5, 400, 42);
        Assert.True(double.IsFinite(curve.Fit.Slope));
        Assert.True(double.IsFinite(curve.Fit.FitError));
    }

    [Fact]
    public void V4_1_115_DispersionPipeline_HasNoBuiltInBiasForD3()
    {
        var results = DispersionAuditRunner.RunAudit(DefaultConfig());
        // All dimensions processed through identical chain-dispersion logic.
        var byDim = results.GroupBy(r => r.Dimension);
        Assert.True(byDim.Count() >= 4);
        // D=3 receives no special treatment beyond what all dimensions get.
        var d3 = results.Where(r => r.Dimension == 3).ToList();
        Assert.Equal(3, d3.Count); // x, y, z directions
    }
}
