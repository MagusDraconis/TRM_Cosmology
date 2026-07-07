namespace TRM.Core.V4_1.Dispersion;

public sealed record DispersionExperimentConfig(
    int ChainLength = 24,
    double Coupling = 0.5,
    int TimeSteps = 400,
    int BaseSeed = 42);
