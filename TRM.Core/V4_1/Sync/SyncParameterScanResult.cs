namespace TRM.Core.V4_1.Sync;

/// <summary>
/// Immutable diagnostic output for a single scan point.
/// Classification: FRAMEWORK.
/// </summary>
public sealed class SyncParameterScanResult
{
    public SyncParameterPoint Point { get; }
    public double FinalOrderParameter { get; }
    public double PerturbationRecovery { get; }
    public double BridgeBandSpread { get; }
    public double SyncQualityScore { get; }
    public bool IsSynced { get; }
    public string Regime { get; } // "synced", "marginal", "unsynced"

    public SyncParameterScanResult(
        SyncParameterPoint point,
        double finalOrderParameter,
        double perturbationRecovery,
        double bridgeBandSpread,
        double syncQualityScore,
        bool isSynced,
        string regime)
    {
        Point = point;
        FinalOrderParameter = finalOrderParameter;
        PerturbationRecovery = perturbationRecovery;
        BridgeBandSpread = bridgeBandSpread;
        SyncQualityScore = syncQualityScore;
        IsSynced = isSynced;
        Regime = regime;
    }
}
