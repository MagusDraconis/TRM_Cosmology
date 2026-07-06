namespace TRM.Core.WeakField;

public sealed record LightDeflectionResult(
    double DeflectionRadians,
    double DeflectionArcSeconds,
    double SolarBaselineArcSeconds,
    double BaselineDeltaArcSeconds);
