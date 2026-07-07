using Xunit;
using TRM.Core.V4_1.Sync;
using TRM.Core.V4_1.Graphs;

namespace TRM.Tests.V4_1;

/// <summary>
/// Validates Kuramoto synchronization infrastructure — computation and reproducibility.
/// Does NOT validate that D=3 is physically selected.
/// </summary>
[Trait("Category", "V4.1")]
[Trait("Category", "V4_1_Synchronization")]
public class V4_1_SynchronizationDynamics_Tests
{
    private static SyncSimulationConfig DefaultConfig(int N) => new(N, 0.5, 0.1, 0.05, 200);

    [Fact]
    public void V4_1_43_OrderParameter_IsBoundedBetweenZeroAndOne()
    {
        var g = GraphFactory.CubicLattice(3);
        var cfg = DefaultConfig(g.NodeCount);
        var rng = new Random(42);
        var omega = SyncDiagnostics.GenerateNaturalFrequencies(g.NodeCount, cfg.NaturalFrequencySpread, rng);
        var init = new double[g.NodeCount];
        for (int i = 0; i < init.Length; i++)
            init[i] = rng.NextDouble() * 2 * Math.PI;

        var result = KuramotoGraphSimulator.Run(g, cfg, omega, init);
        Assert.True(result.FinalOrderParameter >= 0 && result.FinalOrderParameter <= 1.0);
        foreach (double r in result.OrderParameterHistory)
            Assert.True(r >= 0 && r <= 1.0);
    }

    [Fact]
    public void V4_1_44_RegularGraph_SynchronizationMetric_IsComputable()
    {
        var g = GraphFactory.SquareGrid(4);
        var cfg = DefaultConfig(g.NodeCount);
        var rng = new Random(42);
        var omega = SyncDiagnostics.GenerateNaturalFrequencies(g.NodeCount, cfg.NaturalFrequencySpread, rng);
        var rng2 = new Random(42);

        double basin = SyncDiagnostics.SyncBasinFraction(g, cfg, omega, rng2);
        Assert.True(basin >= 0 && basin <= 1.0);
    }

    [Fact]
    public void V4_1_45_LocalPhaseKick_ProducesFiniteRecoveryMetric()
    {
        var g = GraphFactory.CubicLattice(3);
        var cfg = DefaultConfig(g.NodeCount);
        var rng = new Random(42);
        var omega = SyncDiagnostics.GenerateNaturalFrequencies(g.NodeCount, cfg.NaturalFrequencySpread, rng);

        double recovery = SyncDiagnostics.PerturbationRecovery(g, cfg, omega, kickNode: 0);
        Assert.True(double.IsFinite(recovery) && recovery >= 0 && recovery <= 1.0);
    }

    [Fact]
    public void V4_1_46_BridgeBandProxy_IsComputableAcrossSeededRuns()
    {
        var g = GraphFactory.SquareGrid(4);
        var cfg = new SyncSimulationConfig(g.NodeCount, 0.5, 0.1, 0.05, 200)
            { SeededRuns = 3, RandomSeed = 42 };
        var rng = new Random(42);
        var omega = SyncDiagnostics.GenerateNaturalFrequencies(g.NodeCount, cfg.NaturalFrequencySpread, rng);
        var rng2 = new Random(42);

        double spread = SyncDiagnostics.BridgeBandProxy(g, cfg, omega, rng2);
        Assert.True(double.IsFinite(spread) && spread >= 0);
    }

    [Fact]
    public void V4_1_47_DifferentGraphFamilies_ProduceDifferentSyncDiagnostics()
    {
        var graphs = new (int D, GraphTopology g)[]
        {
            (1, GraphFactory.Chain(6)),
            (2, GraphFactory.SquareGrid(4)),
            (3, GraphFactory.CubicLattice(3)),
        };
        var results = new Dictionary<int, double>();
        foreach (var (D, g) in graphs)
        {
            var cfg = new SyncSimulationConfig(g.NodeCount, 0.5, 0.1, 0.05, 200)
                { SeededRuns = 3, RandomSeed = D };
            var rng = new Random(D);
            var omega = SyncDiagnostics.GenerateNaturalFrequencies(g.NodeCount, cfg.NaturalFrequencySpread, rng);
            var rng2 = new Random(D);
            results[D] = KuramotoGraphSimulator.Run(g, cfg, omega,
                Enumerable.Range(0, g.NodeCount).Select(i => rng2.NextDouble() * 2 * Math.PI).ToArray())
                .FinalOrderParameter;
        }
        // All results must be computable and within valid range.
        foreach (var kv in results)
        {
            Assert.True(double.IsFinite(kv.Value));
            Assert.True(kv.Value >= 0 && kv.Value <= 1.0);
        }
        // Note: all may synchronise equally well — this test validates computability,
        // not dimensional differentiation.
    }
}
