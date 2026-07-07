namespace TRM.Core.V4_1.Sync;

/// <summary>
/// A single point in a parameter scan — immutable.
/// Classification: FRAMEWORK.
/// </summary>
public sealed class SyncParameterPoint
{
    public int Dimension { get; }
    public double CouplingK { get; }
    public double FrequencySpread { get; }
    public double Dt { get; }
    public int Steps { get; }
    public int Seed { get; }

    public SyncParameterPoint(int dimension, double couplingK, double frequencySpread,
        double dt, int steps, int seed)
    {
        Dimension = dimension;
        CouplingK = couplingK;
        FrequencySpread = frequencySpread;
        Dt = dt;
        Steps = steps;
        Seed = seed;
    }
}
