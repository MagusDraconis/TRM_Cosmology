namespace TRM.Core.V4_1.Causality;

public sealed record FrontInjectionConfig(
    int SourceNode,
    double PerturbationAmplitude = 1.0);

public sealed record FrontArrivalSample(
    int Node,
    int GraphDistance,
    int ArrivalStep);

public sealed record FrontPropagationResult(
    int Dimension,
    List<FrontArrivalSample> Arrivals,
    double MeanFrontSpeed,
    double SpeedStdDev,
    double AnisotropyIndex);

public sealed record CausalConeResult(
    FrontPropagationResult Result,
    double ConeWidth,
    double ShortestPathResidual);
