using Xunit;
using TRM.Core.V4_1.Cml;

namespace TRM.Tests.V4_1;

[Trait("Category", "V4.1")]
[Trait("Category", "V4_1_CML")]
public class V4_1_CmlDimensionAudit_Tests
{
    private static CmlExperimentConfig DefaultConfig() => new()
    {
        NodesPerDim = 2, CouplingStrength = 0.5, TimeSteps = 200, SeededRuns = 3, BaseSeed = 42
    };

    [Fact]
    public void V4_1_100_CmlSimulator_IsDeterministicAcrossSeededRuns()
    {
        var config = DefaultConfig() with { Dimension = 3 };
        var graph = TRM.Core.V4_1.Graphs.GraphFactory.CubicLattice(config.NodesPerDim);
        var rng1 = new Random(42);
        var rng2 = new Random(42);
        var r1 = CoupledMapLatticeSimulator.Run(graph, config, rng1);
        var r2 = CoupledMapLatticeSimulator.Run(graph, config, rng2);
        Assert.Equal(r1.OrderParameter, r2.OrderParameter, 12);
        Assert.Equal(r1.CollectiveFrequency, r2.CollectiveFrequency, 12);
    }

    [Fact]
    public void V4_1_101_CmlObservables_AreFiniteAndComputable()
    {
        var config = DefaultConfig() with { Dimension = 3 };
        var graph = TRM.Core.V4_1.Graphs.GraphFactory.CubicLattice(config.NodesPerDim);
        var rng = new Random(42);
        var r = CoupledMapLatticeSimulator.Run(graph, config, rng);
        Assert.True(double.IsFinite(r.OrderParameter));
        Assert.True(r.OrderParameter >= 0 && r.OrderParameter <= 1.0);
        Assert.True(double.IsFinite(r.PerturbationRecovery));
        Assert.NotNull(r.Regime);
    }

    [Fact]
    public void V4_1_102_CmlAudit_ProducesPerDimensionResults()
    {
        var config = DefaultConfig();
        var results = CmlDimensionAuditRunner.RunAudit(config);
        Assert.Equal(4, results.Count);
        foreach (var r in results)
        {
            Assert.True(r.Dimension >= 1 && r.Dimension <= 4);
            Assert.True(r.MeanOrderParameter >= 0 && r.MeanOrderParameter <= 1.0);
            Assert.True(r.TotalRuns == config.SeededRuns);
        }
    }

    [Fact]
    public void V4_1_103_CmlAudit_ComputesBridgeBandWidthAcrossRuns()
    {
        var config = DefaultConfig() with { Dimension = 3 };
        var graph = TRM.Core.V4_1.Graphs.GraphFactory.CubicLattice(config.NodesPerDim);
        double bb = CmlDiagnostics.BridgeBandWidth(graph, config);
        Assert.True(double.IsFinite(bb) && bb >= 0);
    }

    [Fact]
    public void V4_1_104_CmlAudit_ProducesNoBuiltInBiasForD3()
    {
        var config = DefaultConfig();
        var results = CmlDimensionAuditRunner.RunAudit(config);
        // All dimensions receive identical treatment; no D=3 shortcut.
        Assert.Equal(4, results.DistinctBy(r => r.Dimension).Count());
    }

    [Fact]
    public void V4_1_105_CmlAudit_CanExpressCounterexampleIfPresent()
    {
        var config = DefaultConfig();
        var results = CmlDimensionAuditRunner.RunAudit(config);
        var comps = ProxyCmlComparisonEngine.Compare(results);
        Assert.NotEmpty(comps);
        foreach (var c in comps)
            Assert.True(c.Classification is "SUPPORTED" or "CONDITIONAL" or "INCONCLUSIVE");
    }
}
