namespace TRM.Core.V4_1.Defects;

// ── Models ──────────────────────────────────────────────────────

public sealed record DefectInjectionConfig(
    int DefectNode,
    double CouplingReduction = 0.5);

public sealed record DefectResponseSample(
    int Node,
    int GraphDistance,
    double ResponseAmplitude);

public sealed record RadialProfileSample(
    int Distance,
    double MeanAmplitude,
    double StdDev,
    int NodeCount);

public sealed record DefectResponseResult(
    int Dimension,
    List<RadialProfileSample> Profile,
    double MeanResponse,
    double ResponseRange);

public sealed class ProfileFitResult
{
    public string Model { get; init; } = "";
    public double Rmse { get; init; }
    public double RSquared { get; init; }
    public double[] Parameters { get; init; } = [];
}

public sealed record DefectExperimentConfig(
    int NodesPerDim = 8,
    double DefectStrength = 0.5,
    int TimeSteps = 300,
    int BaseSeed = 42);
