namespace TRM.Core.V4_1.Defects;

// ── Proto-1PN models ────────────────────────────────────────────

public sealed record NonlinearCorrectionConfig(
    double DefectStrength,
    int DefectSeparation);

public sealed record NonlinearCorrectionSample(
    double Strength,
    int Separation,
    double Eta);

public sealed record NonlinearCorrectionResult(
    int Dimension,
    List<NonlinearCorrectionSample> Samples,
    double MeanEta,
    double EtaStdDev,
    double WeakFieldScore);

public sealed record WeakFieldWindowResult(
    int Dimension,
    double NearFieldResidual,
    double FarFieldResidual,
    double Ratio);

public sealed record EffectiveCompositionFitResult(
    string Model,
    double Eta,
    double Rmse,
    double RSquared);

public sealed record NonlinearCorrectionExperimentConfig(
    int NodesPerDim = 6,
    double MinStrength = 0.1,
    double MaxStrength = 0.9,
    int StrengthSteps = 3,
    int[] Separations = null,
    int TimeSteps = 300,
    int BaseSeed = 42)
{
    public int[] SeparationsField = Separations ?? [2, 4, 6];
}

public sealed record NonlinearInterpretation(
    string Statement,
    string Classification,
    Dictionary<string, double> Metrics);
