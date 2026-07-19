using Xunit;
using TRM.Core.V4_1.Sync;

namespace TRM.Tests.V4_1;

/// <summary>
/// Regression-envelope tests: baseline scan outputs must remain within tolerated ranges.
/// Conservative tolerances — detect accidental numerical breakage, not physics changes.
/// </summary>
[Trait("Category", "V4.1")]
[Trait("Category", "V4_1_SyncScans")]
public class V4_1_SyncRegressionEnvelope_Tests
{
    private static SyncParameterScanConfig BaselineConfig() => new()
    {
        Dimensions = [1, 2, 3],
        CouplingStrengths = [0.5, 1.0],
        FrequencySpreads = [0.0, 0.1],
        Dt = 0.05, Steps = 200, SeedsPerPoint = 2, GraphSizePerDim = 3
    };

    [Fact]
    public void V4_1_56_RegressionEnvelope_Rfinal_InExpectedRange()
    {
        var results = SyncParameterScanner.Scan(BaselineConfig());
        foreach (var r in results)
        {
            Assert.True(r.FinalOrderParameter >= 0.0 && r.FinalOrderParameter <= 1.0,
                $"R_final={r.FinalOrderParameter:F3} out of [0,1] at D={r.Point.Dimension}, K={r.Point.CouplingK}, spread={r.Point.FrequencySpread}.");
        }
    }

    [Fact]
    public void V4_1_57_RegressionEnvelope_RecoveryMetric_InExpectedRange()
    {
        var results = SyncParameterScanner.Scan(BaselineConfig());
        foreach (var r in results)
        {
            Assert.True(r.PerturbationRecovery >= 0.0 && r.PerturbationRecovery <= 1.0,
                $"Recovery={r.PerturbationRecovery:F3} out of [0,1] at D={r.Point.Dimension}.");
        }
    }

    [Fact]
    public void V4_1_58_RegressionEnvelope_BridgeBandSpread_IsNonNegative()
    {
        var results = SyncParameterScanner.Scan(BaselineConfig());
        foreach (var r in results)
            Assert.True(r.BridgeBandSpread >= 0,
                $"Bridge-band spread negative: {r.BridgeBandSpread:E3} at D={r.Point.Dimension}.");
    }

    [Fact]
    public void V4_1_59_RegressionEnvelope_QualityScore_InExpectedRange()
    {
        var results = SyncParameterScanner.Scan(BaselineConfig());
        foreach (var r in results)
            Assert.True(r.SyncQualityScore >= 0.0 && r.SyncQualityScore <= 1.0,
                $"Quality={r.SyncQualityScore:F3} out of [0,1].");
    }

    [Fact]
    public void V4_1_60_RegressionEnvelope_RegimeClassification_IsStable()
    {
        // Two independent scans must produce identical regime classifications.
        var r1 = SyncParameterScanner.Scan(BaselineConfig());
        var r2 = SyncParameterScanner.Scan(BaselineConfig());
        for (int i = 0; i < r1.Count; i++)
            Assert.Equal(r1[i].Regime, r2[i].Regime);
    }
}
