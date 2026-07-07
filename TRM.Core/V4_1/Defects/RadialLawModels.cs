namespace TRM.Core.V4_1.Defects;

// ── Radial-law models ───────────────────────────────────────────

public sealed record RadialRegimeConfig(
    double DefectStrength = 0.5,
    int InnerShells = 2,
    int OuterShellStart = 4);

public sealed record EffectiveExponentSample(
    int ShellDistance,
    double Exponent,
    double FitError);

public sealed record RadialRegimeResult(
    int Dimension,
    List<EffectiveExponentSample> ExponentCurve,
    double InnerExponent,
    double OuterExponent,
    double CrossoverDistance,
    string RegimeLabel);

public sealed record WindowedProfileFitResult(
    string WindowLabel,
    double Exponent,
    double Rmse,
    double RSquared,
    int PointCount);

public sealed record RadialLawAuditResult(
    int Dimension,
    RadialRegimeResult RegimeResult,
    List<WindowedProfileFitResult> WindowFits);

public sealed record RadialLawInterpretation(
    string Statement,
    string Classification,
    Dictionary<string, double> Metrics);
