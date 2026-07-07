namespace TRM.Core.V4_1.Sync;

/// <summary>
/// Defines a parameter scan grid over graph families and Kuramoto parameters.
/// Classification: FRAMEWORK.
/// </summary>
public sealed class SyncParameterScanConfig
{
    public int[] Dimensions { get; init; } = [1, 2, 3, 4];
    public double[] CouplingStrengths { get; init; } = [0.1, 0.5, 1.0];
    public double[] FrequencySpreads { get; init; } = [0.0, 0.1, 0.5];
    public double Dt { get; init; } = 0.05;
    public int Steps { get; init; } = 200;
    public int SeedsPerPoint { get; init; } = 3;
    public int BaseSeed { get; init; } = 42;
    public int GraphSizePerDim { get; init; } = 4; // nodes per spatial dimension

    /// <summary>Total number of scan points (dimensions × K × spread).</summary>
    public int TotalPoints => Dimensions.Length * CouplingStrengths.Length * FrequencySpreads.Length;
}
