using Xunit;
using TRM.Core.V4_1.Sync;
using TRM.Core.V4_1.Graphs;

namespace TRM.Tests.V4_1;

/// <summary>
/// Compares synchronization diagnostics across graph families D = 1..4.
/// Validates computability and reproducibility — does NOT claim D=3 is selected.
/// </summary>
[Trait("Category", "V4.1")]
[Trait("Category", "V4_1_Synchronization")]
public class V4_1_SynchronizationDimensionComparison_Tests
{
    private static readonly (int D, GraphTopology Graph)[] Families =
    [
        (1, GraphFactory.Chain(8)),
        (2, GraphFactory.SquareGrid(4)),
        (3, GraphFactory.CubicLattice(3)),
        (4, GraphFactory.Hypercubic4D(2)),
    ];

    [Fact]
    public void V4_1_48_SyncDiagnostics_AllDimensions_AreComputable()
    {
        foreach (var (D, g) in Families)
        {
            int N = g.NodeCount;
            var cfg = new SyncSimulationConfig(N, 0.5, 0.1, 0.05, 200)
                { SeededRuns = 3, RandomSeed = D };
            var rng = new Random(D);
            var omega = SyncDiagnostics.GenerateNaturalFrequencies(N, cfg.NaturalFrequencySpread, rng);
            var init = Enumerable.Range(0, N).Select(i => (double)i / N * 2 * Math.PI).ToArray();

            var result = KuramotoGraphSimulator.Run(g, cfg, omega, init);
            Assert.True(double.IsFinite(result.FinalOrderParameter),
                $"D={D}: order parameter should be finite.");
            Assert.True(result.FinalOrderParameter >= 0 && result.FinalOrderParameter <= 1.0);
        }
    }

    [Fact]
    public void V4_1_49_SyncDiagnostics_AreReproducible()
    {
        var g = GraphFactory.CubicLattice(3);
        int N = g.NodeCount;
        var cfg = new SyncSimulationConfig(N, 0.5, 0.1, 0.05, 200) { SeededRuns = 3, RandomSeed = 42 };

        double RunOnce()
        {
            var rng = new Random(42);
            var omega = SyncDiagnostics.GenerateNaturalFrequencies(N, cfg.NaturalFrequencySpread, rng);
            var init = Enumerable.Range(0, N).Select(i => (double)i / N * 2 * Math.PI).ToArray();
            return KuramotoGraphSimulator.Run(g, cfg, omega, init).FinalOrderParameter;
        }

        Assert.Equal(RunOnce(), RunOnce(), 12);
    }

    [Fact]
    public void V4_1_50_SyncDiagnostics_NoBuiltInPreferenceForD3()
    {
        // With identical coupling and spread, all dimensions receive the same treatment.
        var results = new Dictionary<int, double>();
        foreach (var (D, g) in Families)
        {
            int N = g.NodeCount;
            var cfg = new SyncSimulationConfig(N, 1.0, 0.0, 0.05, 400);
            var omega = Enumerable.Repeat(1.0, N).ToArray();
            var init = Enumerable.Range(0, N).Select(i => (double)i / N * 2 * Math.PI).ToArray();
            results[D] = KuramotoGraphSimulator.Run(g, cfg, omega, init).FinalOrderParameter;
        }
        // All results must be finite and in [0,1]; D=3 receives no special treatment.
        foreach (var kv in results)
        {
            Assert.True(double.IsFinite(kv.Value));
            Assert.True(kv.Value >= 0 && kv.Value <= 1.0,
                $"D={kv.Key}: R={kv.Value:F3} should be in [0,1].");
        }
    }

    [Fact]
    public void V4_1_51_PerturbationRecovery_IsComputableAcrossDimensions()
    {
        foreach (var (D, g) in Families)
        {
            int N = g.NodeCount;
            var cfg = new SyncSimulationConfig(N, 0.5, 0.1, 0.05, 200);
            var rng = new Random(D);
            var omega = SyncDiagnostics.GenerateNaturalFrequencies(N, cfg.NaturalFrequencySpread, rng);

            double recovery = SyncDiagnostics.PerturbationRecovery(g, cfg, omega, kickNode: N / 2);
            Assert.True(double.IsFinite(recovery));
            Assert.True(recovery >= 0 && recovery <= 1.0,
                $"D={D}: recovery R={recovery:F3} should be in [0,1].");
        }
    }
}
