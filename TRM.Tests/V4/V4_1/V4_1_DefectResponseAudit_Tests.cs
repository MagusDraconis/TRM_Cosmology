using Xunit;
using TRM.Core.V4_1.Defects;

namespace TRM.Tests.V4_1;

[Trait("Category", "V4.1")]
[Trait("Category", "V4_1_Defects")]
public class V4_1_DefectResponseAudit_Tests
{
    private static DefectExperimentConfig DefaultConfig() => new()
    {
        NodesPerDim = 6, DefectStrength = 0.5, TimeSteps = 200, BaseSeed = 42
    };

    [Fact]
    public void V4_1_130_DefectAudit_IsDeterministicAcrossSeededRuns()
    {
        var graph = TRM.Core.V4_1.Graphs.GraphFactory.CubicLattice(6);
        var r1 = DefectResponseAnalyzer.MeasureDefectResponse(graph, 3, DefaultConfig());
        var r2 = DefectResponseAnalyzer.MeasureDefectResponse(graph, 3, DefaultConfig());
        Assert.Equal(r1.Profile.Count, r2.Profile.Count);
        for (int i = 0; i < r1.Profile.Count; i++)
            Assert.Equal(r1.Profile[i].MeanAmplitude, r2.Profile[i].MeanAmplitude, 6);
    }

    [Fact]
    public void V4_1_131_DefectResponse_ProducesFiniteRadialProfiles()
    {
        var graph = TRM.Core.V4_1.Graphs.GraphFactory.SquareGrid(6);
        var result = DefectResponseAnalyzer.MeasureDefectResponse(graph, 2, DefaultConfig());
        Assert.NotEmpty(result.Profile);
        foreach (var p in result.Profile)
        {
            Assert.True(p.Distance > 0);
            Assert.True(double.IsFinite(p.MeanAmplitude));
            Assert.True(p.NodeCount > 0);
        }
    }

    [Fact]
    public void V4_1_132_RadialProfileFitter_ComputesFiniteFitScores()
    {
        var graph = TRM.Core.V4_1.Graphs.GraphFactory.CubicLattice(6);
        var result = DefectResponseAnalyzer.MeasureDefectResponse(graph, 3, DefaultConfig());
        var fit = RadialProfileFitter.FitPowerLaw(result.Profile, 1.0);
        Assert.True(double.IsFinite(fit.Rmse));
        Assert.True(double.IsFinite(fit.RSquared));
    }

    [Fact]
    public void V4_1_133_DefectAudit_ProducesPerDimensionResults()
    {
        var results = DefectAuditRunner.RunAudit(DefaultConfig());
        Assert.Equal(4, results.Count);
    }

    [Fact]
    public void V4_1_134_DefectInterpretation_ProducesNoOverclaim()
    {
        var audit = DefectAuditRunner.RunAudit(DefaultConfig());
        var interps = DefectInterpretationEngine.Interpret(audit);
        foreach (var i in interps)
        {
            Assert.DoesNotContain("proven", i.Statement, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("derived", i.Statement, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("proof", i.Statement, StringComparison.OrdinalIgnoreCase);
            Assert.True(i.Classification is "SUPPORTED" or "CONDITIONAL" or "INCONCLUSIVE");
        }
    }

    [Fact]
    public void V4_1_135_DefectAudit_HasNoBuiltInBiasForOneOverR()
    {
        var audit = DefectAuditRunner.RunAudit(DefaultConfig());
        foreach (var (_, fits) in audit)
        {
            // All three candidate models are tested; no single one is hard-coded as correct.
            Assert.Equal(3, fits.Count);
            Assert.Contains(fits, f => f.Model == "1/r^1");
            Assert.Contains(fits, f => f.Model == "1/r^2");
            Assert.Contains(fits, f => f.Model == "exp(-r/xi)/r");
        }
    }
}
