namespace TRM.Core.V4_1.Comparators;

// ── Comparator models ───────────────────────────────────────────

public sealed record ComparatorVariant(
    string Family,
    string Name,
    Dictionary<int, double> Scores,
    int PreferredDim,
    double D3Score,
    double DeviationFromBaseline);

public sealed record NullModelResult(
    string Name,
    bool IsTrivialWinner,
    bool ReproducesD3);

public sealed record ComparatorAuditResult(
    List<ComparatorVariant> Variants,
    List<NullModelResult> NullModels,
    double SpecificityScore,
    double D3SurvivalFraction,
    string Summary);

public sealed record ComparatorInterpretation(
    string Statement,
    string Classification,
    Dictionary<string, double> Metrics);
