namespace TRM.Core.V4_1.Cml;

/// <summary>Immutable result of a single CML experiment run. Classification: FRAMEWORK.</summary>
public sealed class CmlExperimentResult
{
    public int Dimension { get; init; }
    public double OrderParameter { get; init; }
    public double CollectiveFrequency { get; init; }
    public double PerturbationRecovery { get; init; }
    public bool IsSynchronized { get; init; }
    public string Regime => IsSynchronized ? "synced" : OrderParameter > 0.5 ? "marginal" : "unsynced";
}
