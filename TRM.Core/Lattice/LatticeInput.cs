namespace TRM.Core.Lattice;

public sealed record LatticeInput(
    int NodeCount,
    double Coupling,
    double InjectedEnergy,
    int InjectionIndex);
