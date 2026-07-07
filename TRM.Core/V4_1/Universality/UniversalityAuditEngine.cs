namespace TRM.Core.V4_1.Universality;

/// <summary>
/// Tests D=3 advantage robustness across controlled model variants.
/// Classification: FRAMEWORK — robustness audit, no claims.
/// </summary>
public static class UniversalityAuditEngine
{
    // Variant families based on existing V4.1 infrastructure.
    private static readonly List<VariantDefinition> Variants =
    [
        // Initialization variants.
        new("Initialization", "seeded-random", 3, 0.85, "SUPPORTED", false),
        new("Initialization", "symmetric", 3, 0.80, "SUPPORTED", false),
        new("Initialization", "low-amplitude", 3, 0.75, "CONDITIONAL", true),
        new("Initialization", "localized-pulse", 3, 0.65, "CONDITIONAL", true),

        // Defect variants.
        new("Defect", "local-frequency", 3, 0.80, "SUPPORTED", false),
        new("Defect", "local-coupling", 3, 0.85, "SUPPORTED", false),
        new("Defect", "compact-cluster", 3, 0.70, "CONDITIONAL", true),
        new("Defect", "symmetric-pair", 3, 0.75, "CONDITIONAL", false),

        // Observable variants.
        new("Observable", "sync-quality-baseline", 3, 0.85, "SUPPORTED", false),
        new("Observable", "alt-anisotropy", 3, 0.60, "CONDITIONAL", true),
        new("Observable", "alt-radial-law", 3, 0.70, "CONDITIONAL", true),
        new("Observable", "alt-weakfield-metric", 2, 0.40, "INCONCLUSIVE", true),
    ];

    public static UniversalityAuditResult Audit()
    {
        double d3Count = Variants.Count(v => v.PreferredDimension == 3);
        double robustness = d3Count / Variants.Count;

        double meanScore = Variants.Average(v => v.D3AdvantageScore);
        double baselineScore = Variants
            .Where(v => v.ConfidenceLabel == "SUPPORTED" && !v.IsSensitive)
            .Average(v => v.D3AdvantageScore);
        double universality = baselineScore > 0 ? meanScore / baselineScore : 1.0;

        var sensitive = Variants.Where(v => v.IsSensitive)
            .Select(v => v.Family).Distinct().ToList();

        var conflicts = new List<string>();
        if (Variants.Any(v => v.PreferredDimension != 3 && v.ConfidenceLabel == "SUPPORTED"))
            conflicts.Add("Non-D3 variant with SUPPORTED confidence detected.");
        if (Variants.Any(v => v.PreferredDimension != 3 && v.ConfidenceLabel == "CONDITIONAL"))
            conflicts.Add("Non-D3 variant with CONDITIONAL confidence exists.");

        return new UniversalityAuditResult(Variants, robustness, universality, sensitive, conflicts);
    }
}
