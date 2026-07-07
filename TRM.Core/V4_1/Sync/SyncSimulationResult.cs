namespace TRM.Core.V4_1.Sync;

/// <summary>
/// Immutable result of a Kuramoto synchronization simulation.
/// Classification: FRAMEWORK — holds computed values.
/// </summary>
public sealed class SyncSimulationResult
{
    /// <summary>Final order parameter magnitude (0..1).</summary>
    public double FinalOrderParameter { get; }

    /// <summary>Time series of order parameter R(t).</summary>
    public double[] OrderParameterHistory { get; }

    /// <summary>Estimated collective frequency Omega* (mean phase velocity).</summary>
    public double CollectiveFrequency { get; }

    /// <summary>Whether the system reached near-synchronisation.</summary>
    public bool IsSynchronized { get; }

    /// <summary>Number of time steps elapsed.</summary>
    public int Steps { get; }

    public SyncSimulationResult(
        double finalOrderParameter,
        double[] orderParameterHistory,
        double collectiveFrequency,
        bool isSynchronized,
        int steps)
    {
        FinalOrderParameter = finalOrderParameter;
        OrderParameterHistory = orderParameterHistory;
        CollectiveFrequency = collectiveFrequency;
        IsSynchronized = isSynchronized;
        Steps = steps;
    }
}
