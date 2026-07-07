namespace TRM.Core.V4_1.Reduction;

// ── Block-universality models ───────────────────────────────────

public sealed record CanonicalVariantSample(
    string Variant,
    string PreferredForm,
    double CoreWeight,
    double CorrectionWeight,
    double RSquared);

public sealed record BlockWeightStability(
    double CoreWeightVariance,
    double CorrectionWeightVariance,
    bool CorrectionRemainsSubleading);

public sealed record BlockUniversalityResult(
    List<CanonicalVariantSample> Variants,
    Dictionary<string, int> FormFrequency,
    string PreferredFormOverall,
    BlockWeightStability Stability,
    string RobustnessClass);

public sealed record BlockUniversalityInterpretation(
    string Statement,
    string Classification,
    Dictionary<string, double> Metrics);
