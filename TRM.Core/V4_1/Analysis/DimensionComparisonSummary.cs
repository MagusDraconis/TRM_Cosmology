namespace TRM.Core.V4_1.Analysis;

/// <summary>Comparison between two dimensions. Classification: FRAMEWORK.</summary>
public sealed class DimensionComparisonSummary
{
    public int DimensionA { get; init; }
    public int DimensionB { get; init; }
    public double MeanQualityDiff { get; init; }
    public double BridgeBandDiff { get; init; }
    public List<AnalysisStatement> Statements { get; init; } = [];
}
