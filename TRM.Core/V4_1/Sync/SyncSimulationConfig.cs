namespace TRM.Core.V4_1.Sync;

/// <summary>
/// Configuration for a deterministic Kuramoto-style synchronization simulation.
/// Classification: FRAMEWORK — computational infrastructure.
/// </summary>
public sealed class SyncSimulationConfig
{
    public int NodeCount { get; init; }
    public double CouplingStrength { get; init; }
    public double NaturalFrequencySpread { get; init; }
    public double Dt { get; init; }
    public int TotalSteps { get; init; }
    public int SeededRuns { get; init; } = 3;
    public int RandomSeed { get; init; } = 42;

    public SyncSimulationConfig(
        int nodeCount, double couplingStrength, double naturalFrequencySpread,
        double dt, int totalSteps)
    {
        if (nodeCount < 2) throw new ArgumentOutOfRangeException(nameof(nodeCount));
        if (couplingStrength < 0) throw new ArgumentOutOfRangeException(nameof(couplingStrength));
        if (naturalFrequencySpread < 0) throw new ArgumentOutOfRangeException(nameof(naturalFrequencySpread));
        if (dt <= 0) throw new ArgumentOutOfRangeException(nameof(dt));
        if (totalSteps < 1) throw new ArgumentOutOfRangeException(nameof(totalSteps));

        NodeCount = nodeCount;
        CouplingStrength = couplingStrength;
        NaturalFrequencySpread = naturalFrequencySpread;
        Dt = dt;
        TotalSteps = totalSteps;
    }
}
