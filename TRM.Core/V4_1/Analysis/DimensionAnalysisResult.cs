namespace TRM.Core.V4_1.Analysis;

/// <summary>Per-dimension aggregated diagnostics. Classification: FRAMEWORK.</summary>
public sealed class DimensionAnalysisResult
{
    public int Dimension { get; init; }
    public double MeanQuality { get; init; }
    public double QualityStdDev { get; init; }
    public double MeanBridgeBand { get; init; }
    public double MeanRecovery { get; init; }
    public int SyncedCount { get; init; }
    public int MarginalCount { get; init; }
    public int UnsyncedCount { get; init; }
    public int TotalPoints { get; init; }
    public double SyncFraction => TotalPoints > 0 ? (double)SyncedCount / TotalPoints : 0;
}
