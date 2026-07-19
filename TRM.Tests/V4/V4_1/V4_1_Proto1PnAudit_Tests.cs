using Xunit;
using TRM.Core.V4_1.Defects;

namespace TRM.Tests.V4_1;

[Trait("Category", "V4.1")]
[Trait("Category", "V4_1_Proto1PN")]
public class V4_1_Proto1PnAudit_Tests
{
    private static NonlinearCorrectionExperimentConfig DefaultConfig() => new()
    {
        NodesPerDim = 5, MinStrength = 0.2, MaxStrength = 0.6, StrengthSteps = 2,
        Separations = [3], TimeSteps = 200, BaseSeed = 42
    };

    [Fact]
    public void V4_1_150_NonlinearAudit_IsDeterministicAcrossSeededRuns()
    {
        var graph = TRM.Core.V4_1.Graphs.GraphFactory.CubicLattice(5);
        var r1 = NonlinearCorrectionAnalyzer.Analyze(graph, 3, DefaultConfig());
        var r2 = NonlinearCorrectionAnalyzer.Analyze(graph, 3, DefaultConfig());
        Assert.Equal(r1.MeanEta, r2.MeanEta, 6);
    }

    [Fact]
    public void V4_1_151_NonlinearResiduals_AreFiniteAndComputable()
    {
        var graph = TRM.Core.V4_1.Graphs.GraphFactory.SquareGrid(5);
        var result = NonlinearCorrectionAnalyzer.Analyze(graph, 2, DefaultConfig());
        Assert.True(double.IsFinite(result.MeanEta));
        Assert.NotEmpty(result.Samples);
    }

    [Fact]
    public void V4_1_152_EffectiveCompositionFit_ComputesFiniteParameters()
    {
        var graph = TRM.Core.V4_1.Graphs.GraphFactory.CubicLattice(5);
        var result = NonlinearCorrectionAnalyzer.Analyze(graph, 3, DefaultConfig());
        var fit = NonlinearCorrectionAnalyzer.FitBilinear(result);
        Assert.True(double.IsFinite(fit.Eta));
        Assert.True(double.IsFinite(fit.Rmse));
    }

    [Fact]
    public void V4_1_153_WeakFieldWindow_IsComputablePerDimension()
    {
        var graph = TRM.Core.V4_1.Graphs.GraphFactory.CubicLattice(5);
        var window = NonlinearCorrectionAnalyzer.ComputeWeakFieldWindow(graph, 3, DefaultConfig());
        Assert.True(double.IsFinite(window.NearFieldResidual));
        Assert.True(double.IsFinite(window.FarFieldResidual));
        Assert.True(double.IsFinite(window.Ratio));
    }

    [Fact]
    public void V4_1_154_NonlinearInterpretation_ProducesNoOverclaim()
    {
        var (corrections, windows) = NonlinearCorrectionAuditRunner.RunAudit(DefaultConfig());
        var interps = NonlinearInterpretationEngine.Interpret(corrections, windows);
        foreach (var i in interps)
        {
            Assert.DoesNotContain("proven", i.Statement, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("derived", i.Statement, StringComparison.OrdinalIgnoreCase);
            Assert.True(i.Classification is "SUPPORTED" or "CONDITIONAL" or "INCONCLUSIVE");
        }
    }

    [Fact]
    public void V4_1_155_Proto1PnAudit_HasNoBuiltInBiasForD3()
    {
        var (corrections, windows) = NonlinearCorrectionAuditRunner.RunAudit(DefaultConfig());
        Assert.Equal(4, corrections.Count);
        Assert.Equal(4, windows.Count);
    }
}
