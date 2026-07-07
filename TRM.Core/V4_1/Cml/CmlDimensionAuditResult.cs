namespace TRM.Core.V4_1.Cml;

/// <summary>Per-dimension CML audit result. Classification: FRAMEWORK.</summary>
public sealed class CmlDimensionAuditResult
{
    public int Dimension { get; init; }
    public double MeanOrderParameter { get; init; }
    public double MeanRecovery { get; init; }
    public double BridgeBandWidth { get; init; }
    public int SyncedCount { get; init; }
    public int TotalRuns { get; init; }
    public double SyncFraction => TotalRuns > 0 ? (double)SyncedCount / TotalRuns : 0;
}
