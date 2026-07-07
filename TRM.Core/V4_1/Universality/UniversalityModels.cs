namespace TRM.Core.V4_1.Universality;

// ── Universality models ─────────────────────────────────────────

public sealed record VariantDefinition(
    string Family,
    string Variant,
    int PreferredDimension,
    double D3AdvantageScore,
    string ConfidenceLabel,
    bool IsSensitive);

public sealed record UniversalityAuditResult(
    List<VariantDefinition> Variants,
    double RobustnessFraction,
    double UniversalityScore,
    List<string> SensitiveFamilies,
    List<string> ConflictNotes);

public sealed record UniversalityInterpretation(
    string Statement,
    string Classification,
    Dictionary<string, double> Metrics);
