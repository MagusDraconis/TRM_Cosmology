namespace TRM.Core.Lattice;

public sealed record LatticeSnapshot(
    IReadOnlyList<double> NodeEnergies,
    IReadOnlyList<double> Distances,
    IReadOnlyList<double> Correlation,
    IReadOnlyList<double> PadeKernel);
