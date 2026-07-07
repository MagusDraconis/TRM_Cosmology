namespace TRM.Core.V4_1.Cml;

/// <summary>Configuration for a deterministic CML experiment. Classification: FRAMEWORK.</summary>
public sealed record CmlExperimentConfig
{
    public int Dimension { get; init; }
    public int NodesPerDim { get; init; } = 4;
    public double CouplingStrength { get; init; } = 0.5;
    public int TimeSteps { get; init; } = 200;
    public int SeededRuns { get; init; } = 3;
    public int BaseSeed { get; init; } = 42;
}
