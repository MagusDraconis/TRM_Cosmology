namespace TRM.Core.WeakField;

public sealed record LightDeflectionResult(
    double GrDeflectionRadians,
    double GrDeflectionArcSeconds,
    double TrmDeflectionRadians,
    double TrmDeflectionArcSeconds,
    double SolarBaselineArcSeconds,
    double TrmCorrectionFactor,
    double DeltaTrmMinusGrArcSeconds);
