namespace TRM.Core.V4_1.Cml;

/// <summary>Comparison statement between proxy and CML results. Classification: FRAMEWORK.</summary>
public sealed class ProxyCmlComparison
{
    public int Dimension { get; init; }
    public string Statement { get; init; } = "";
    public string Classification { get; init; } = "CONDITIONAL";
}
