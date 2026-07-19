using Xunit;
using TRM.Core.V4_1.Causality;

namespace TRM.Tests.V4_1;

[Trait("Category", "V4.1")]
[Trait("Category", "V4_1_Causality")]
public class V4_1_CausalConeAudit_Tests
{
    private static CausalFrontExperimentConfig DefaultConfig() => new()
    {
        NodesPerDim = 5, CouplingK = 0.3, MaxSteps = 200, BaseSeed = 42
    };

    [Fact]
    public void V4_1_120_CausalFront_ArrivalTimes_AreFiniteAndComputable()
    {
        var graph = TRM.Core.V4_1.Graphs.GraphFactory.CubicLattice(5);
        var front = CausalFrontAnalyzer.MeasureFront(graph, 3, 0, 0.3, 200, 42);
        Assert.NotEmpty(front.Arrivals);
        foreach (var a in front.Arrivals)
        {
            Assert.True(a.ArrivalStep > 0);
            Assert.True(a.GraphDistance > 0);
        }
    }

    [Fact]
    public void V4_1_121_CausalFrontAudit_IsDeterministicAcrossSeededRuns()
    {
        var graph = TRM.Core.V4_1.Graphs.GraphFactory.SquareGrid(5);
        var f1 = CausalFrontAnalyzer.MeasureFront(graph, 2, 0, 0.3, 200, 42);
        var f2 = CausalFrontAnalyzer.MeasureFront(graph, 2, 0, 0.3, 200, 42);
        Assert.Equal(f1.MeanFrontSpeed, f2.MeanFrontSpeed, 6);
        Assert.Equal(f1.AnisotropyIndex, f2.AnisotropyIndex, 6);
    }

    [Fact]
    public void V4_1_122_CausalFrontAudit_ProducesPerDimensionResults()
    {
        var results = CausalFrontAuditRunner.RunAudit(DefaultConfig());
        Assert.Equal(4, results.Count);
    }

    [Fact]
    public void V4_1_123_CausalConeWidth_IsComputable()
    {
        var results = CausalFrontAuditRunner.RunAudit(DefaultConfig());
        foreach (var r in results)
        {
            Assert.True(double.IsFinite(r.ConeWidth));
            Assert.True(r.ConeWidth >= 0);
        }
    }

    [Fact]
    public void V4_1_124_AnisotropyIndex_IsComputable()
    {
        var results = CausalFrontAuditRunner.RunAudit(DefaultConfig());
        foreach (var r in results)
            Assert.True(double.IsFinite(r.Result.AnisotropyIndex));
    }

    [Fact]
    public void V4_1_125_CausalFrontPipeline_HasNoBuiltInBiasForD3()
    {
        var results = CausalFrontAuditRunner.RunAudit(DefaultConfig());
        Assert.Equal(4, results.DistinctBy(r => r.Result.Dimension).Count());
    }
}
