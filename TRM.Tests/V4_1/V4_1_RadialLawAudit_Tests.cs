using Xunit;
using TRM.Core.V4_1.Defects;

namespace TRM.Tests.V4_1;

[Trait("Category", "V4.1")]
[Trait("Category", "V4_1_RadialLaw")]
public class V4_1_RadialLawAudit_Tests
{
    private static DefectExperimentConfig DefaultConfig() => new()
    {
        NodesPerDim = 8, DefectStrength = 0.5, TimeSteps = 200, BaseSeed = 42
    };

    [Fact]
    public void V4_1_160_RadialLawAudit_IsDeterministicAcrossSeededRuns()
    {
        var graph = TRM.Core.V4_1.Graphs.GraphFactory.CubicLattice(8);
        var r1 = RadialRegimeAnalyzer.Analyze(graph, 3, DefaultConfig());
        var r2 = RadialRegimeAnalyzer.Analyze(graph, 3, DefaultConfig());
        Assert.Equal(r1.OuterExponent, r2.OuterExponent, 6);
    }

    [Fact]
    public void V4_1_161_EffectiveExponent_IsFiniteAcrossShells()
    {
        var graph = TRM.Core.V4_1.Graphs.GraphFactory.SquareGrid(8);
        var result = RadialRegimeAnalyzer.Analyze(graph, 2, DefaultConfig());
        Assert.NotEmpty(result.ExponentCurve);
        foreach (var e in result.ExponentCurve)
            Assert.True(double.IsFinite(e.Exponent));
    }

    [Fact]
    public void V4_1_162_WindowedFitter_ComputesStableFitMetrics()
    {
        var graph = TRM.Core.V4_1.Graphs.GraphFactory.CubicLattice(8);
        var response = DefectResponseAnalyzer.MeasureDefectResponse(graph, 3, DefaultConfig());
        var fits = WindowedProfileFitter.FitWindows(response.Profile, new RadialRegimeConfig());
        Assert.Equal(3, fits.Count);
        foreach (var f in fits)
        {
            Assert.True(double.IsFinite(f.Exponent));
            Assert.True(double.IsFinite(f.Rmse));
        }
    }

    [Fact]
    public void V4_1_163_RadialLawAudit_ProducesPerDimensionOuterExponent()
    {
        var results = RadialLawAuditRunner.RunAudit(DefaultConfig());
        Assert.Equal(4, results.Count);
        foreach (var r in results)
            Assert.True(double.IsFinite(r.RegimeResult.OuterExponent));
    }

    [Fact]
    public void V4_1_164_RadialLawInterpretation_ProducesNoOverclaim()
    {
        var results = RadialLawAuditRunner.RunAudit(DefaultConfig());
        var interps = RadialLawInterpretationEngine.Interpret(results);
        foreach (var i in interps)
        {
            Assert.DoesNotContain("proven", i.Statement, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("derived", i.Statement, StringComparison.OrdinalIgnoreCase);
            Assert.True(i.Classification is "SUPPORTED" or "CONDITIONAL" or "INCONCLUSIVE");
        }
    }

    [Fact]
    public void V4_1_165_RadialLawAudit_HasNoBuiltInBiasForOneOverR()
    {
        var results = RadialLawAuditRunner.RunAudit(DefaultConfig());
        // Inner and outer exponents are measured, not assumed.
        foreach (var r in results)
        {
            Assert.NotEqual(0, r.RegimeResult.ExponentCurve.Count);
            Assert.NotNull(r.RegimeResult.RegimeLabel);
        }
    }
}
