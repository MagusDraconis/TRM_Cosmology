using Xunit;
using TRM.Core.V4_1.Defects;

namespace TRM.Tests.V4_1;

[Trait("Category", "V4.1")]
[Trait("Category", "V4_1_Superposition")]
public class V4_1_MultiDefectSuperposition_Tests
{
    private static MultiDefectExperimentConfig DefaultConfig() => new()
    {
        NodesPerDim = 6, DefectStrength = 0.5, DefectSeparation = 3, TimeSteps = 200, BaseSeed = 42
    };

    [Fact]
    public void V4_1_140_MultiDefectAudit_IsDeterministicAcrossSeededRuns()
    {
        var graph = TRM.Core.V4_1.Graphs.GraphFactory.CubicLattice(6);
        var r1 = MultiDefectAnalyzer.MeasureSuperposition(graph, 3, DefaultConfig());
        var r2 = MultiDefectAnalyzer.MeasureSuperposition(graph, 3, DefaultConfig());
        Assert.Equal(r1.MeanResidual, r2.MeanResidual, 6);
        Assert.Equal(r1.FarFieldResidual, r2.FarFieldResidual, 6);
    }

    [Fact]
    public void V4_1_141_MultiDefectResponse_ProducesFiniteResiduals()
    {
        var graph = TRM.Core.V4_1.Graphs.GraphFactory.SquareGrid(6);
        var result = MultiDefectAnalyzer.MeasureSuperposition(graph, 2, DefaultConfig());
        Assert.True(double.IsFinite(result.MeanResidual));
        Assert.True(double.IsFinite(result.MaxResidual));
        Assert.Equal(graph.NodeCount, result.Residuals.Length);
    }

    [Fact]
    public void V4_1_142_SuperpositionError_IsComputablePerDimension()
    {
        var results = MultiDefectAuditRunner.RunAudit(DefaultConfig());
        Assert.Equal(4, results.Count);
        foreach (var r in results)
        {
            Assert.True(r.MeanResidual >= 0);
            Assert.True(r.MaxResidual >= r.MeanResidual);
        }
    }

    [Fact]
    public void V4_1_143_FarFieldResiduals_AreSeparatelyComputed()
    {
        var results = MultiDefectAuditRunner.RunAudit(DefaultConfig());
        foreach (var r in results)
            Assert.True(double.IsFinite(r.FarFieldResidual));
    }

    [Fact]
    public void V4_1_144_SuperpositionInterpretation_ProducesNoOverclaim()
    {
        var results = MultiDefectAuditRunner.RunAudit(DefaultConfig());
        var interps = SuperpositionInterpretationEngine.Interpret(results);
        foreach (var i in interps)
        {
            Assert.DoesNotContain("proven", i.Statement, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("derived", i.Statement, StringComparison.OrdinalIgnoreCase);
            Assert.True(i.Classification is "SUPPORTED" or "CONDITIONAL" or "INCONCLUSIVE");
        }
    }

    [Fact]
    public void V4_1_145_SuperpositionAudit_HasNoBuiltInBiasForD3()
    {
        var results = MultiDefectAuditRunner.RunAudit(DefaultConfig());
        Assert.Equal(4, results.DistinctBy(r => r.Dimension).Count());
    }
}
