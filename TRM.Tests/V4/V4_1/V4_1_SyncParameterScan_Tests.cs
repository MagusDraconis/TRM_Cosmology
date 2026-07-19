using Xunit;
using TRM.Core.V4_1.Sync;

namespace TRM.Tests.V4_1;

/// <summary>
/// Validates parameter scan pipeline — determinism, computability, sensitivity.
/// Does NOT claim D=3 is selected.
/// </summary>
[Trait("Category", "V4.1")]
[Trait("Category", "V4_1_SyncScans")]
public class V4_1_SyncParameterScan_Tests
{
    private static SyncParameterScanConfig DefaultConfig() => new()
    {
        Dimensions = [1, 2, 3],
        CouplingStrengths = [0.5, 1.0],
        FrequencySpreads = [0.0, 0.1],
        Dt = 0.05,
        Steps = 200,
        SeedsPerPoint = 2,
        GraphSizePerDim = 3
    };

    [Fact]
    public void V4_1_50_ParameterScan_ProducesDeterministicGridCardinality()
    {
        var cfg = DefaultConfig();
        int expected = cfg.Dimensions.Length * cfg.CouplingStrengths.Length * cfg.FrequencySpreads.Length;
        var results = SyncParameterScanner.Scan(cfg);
        Assert.Equal(expected, results.Count);
    }

    [Fact]
    public void V4_1_51_ParameterScan_AllMetricsAreFinite()
    {
        var results = SyncParameterScanner.Scan(DefaultConfig());
        foreach (var r in results)
        {
            Assert.True(double.IsFinite(r.FinalOrderParameter));
            Assert.True(double.IsFinite(r.PerturbationRecovery));
            Assert.True(double.IsFinite(r.BridgeBandSpread));
            Assert.True(double.IsFinite(r.SyncQualityScore));
            Assert.True(r.FinalOrderParameter >= 0 && r.FinalOrderParameter <= 1.0);
            Assert.NotNull(r.Regime);
        }
    }

    [Fact]
    public void V4_1_52_ParameterScan_RepeatedExecution_IsIdentical()
    {
        var cfg = DefaultConfig();
        var r1 = SyncParameterScanner.Scan(cfg);
        var r2 = SyncParameterScanner.Scan(cfg);
        Assert.Equal(r1.Count, r2.Count);
        for (int i = 0; i < r1.Count; i++)
        {
            Assert.Equal(r1[i].FinalOrderParameter, r2[i].FinalOrderParameter, 12);
            Assert.Equal(r1[i].Regime, r2[i].Regime);
        }
    }

    [Fact]
    public void V4_1_53_ParameterScan_DifferentKValues_ChangeSyncMetrics()
    {
        var cfg = new SyncParameterScanConfig
        {
            Dimensions = [3],
            CouplingStrengths = [0.1, 1.0],
            FrequencySpreads = [0.1],
            Dt = 0.05, Steps = 300, SeedsPerPoint = 2, GraphSizePerDim = 3
        };
        var results = SyncParameterScanner.Scan(cfg);
        Assert.Equal(2, results.Count);
        // Higher K should give higher (or equal) order parameter.
        Assert.True(results[1].FinalOrderParameter >= results[0].FinalOrderParameter,
            $"K=1.0 R={results[1].FinalOrderParameter:F3} should be >= K=0.1 R={results[0].FinalOrderParameter:F3}.");
    }

    [Fact]
    public void V4_1_54_ParameterScan_DifferentSpreadValues_ChangeBridgeBandProxy()
    {
        var cfg = new SyncParameterScanConfig
        {
            Dimensions = [3],
            CouplingStrengths = [0.5],
            FrequencySpreads = [0.0, 0.5],
            Dt = 0.05, Steps = 300, SeedsPerPoint = 5, GraphSizePerDim = 4
        };
        var results = SyncParameterScanner.Scan(cfg);
        // spread=0 should have (near) zero bridge-band spread;
        // spread=0.5 should have larger spread due to natural frequency dispersion.
        Assert.True(results[1].BridgeBandSpread >= results[0].BridgeBandSpread,
            $"spread=0.5 spread={results[1].BridgeBandSpread:E3} should be >= spread=0 spread={results[0].BridgeBandSpread:E3}.");
    }

    [Fact]
    public void V4_1_55_ParameterScan_GraphFamilies_ProduceDistinctDiagnosticProfiles()
    {
        var cfg = new SyncParameterScanConfig
        {
            Dimensions = [1, 2, 3],
            CouplingStrengths = [0.5],
            FrequencySpreads = [0.1],
            Dt = 0.05, Steps = 300, SeedsPerPoint = 2, GraphSizePerDim = 3
        };
        var results = SyncParameterScanner.Scan(cfg);
        var byDim = results.GroupBy(r => r.Point.Dimension).ToDictionary(g => g.Key, g => g.First());
        // Different dimensions should produce distinguishable diagnostics.
        Assert.True(byDim[1].FinalOrderParameter != byDim[2].FinalOrderParameter ||
                    byDim[1].PerturbationRecovery != byDim[2].PerturbationRecovery,
            "D=1 and D=2 should differ on at least one metric.");
    }
}
