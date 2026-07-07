namespace TRM.Core.V4_1.Analysis;

/// <summary>A single structured, reviewer-safe analysis statement. Classification: FRAMEWORK.</summary>
public sealed class AnalysisStatement
{
    public string Text { get; init; } = "";
    public AnalysisConfidence Confidence { get; init; }
    public Dictionary<string, double> Metrics { get; init; } = [];
}
