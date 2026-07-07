namespace TRM.Core.V4_1.Universality;

public static class UniversalityInterpretationEngine
{
    public static List<UniversalityInterpretation> Interpret(UniversalityAuditResult result)
    {
        var interps = new List<UniversalityInterpretation>();

        interps.Add(new UniversalityInterpretation(
            $"D=3 remains favored in {result.RobustnessFraction * 100:F0}% of tested variants " +
            $"({result.Variants.Count(v => v.PreferredDimension == 3)}/{result.Variants.Count}).",
            result.RobustnessFraction > 0.8 ? "SUPPORTED" :
            result.RobustnessFraction > 0.6 ? "CONDITIONAL" : "INCONCLUSIVE",
            new() { ["robustness"] = result.RobustnessFraction }));

        int sensitiveCount = result.SensitiveFamilies.Count;
        interps.Add(new UniversalityInterpretation(
            $"{sensitiveCount} variant families show parameter sensitivity — " +
            (sensitiveCount > 2
                ? "the D=3 advantage is not universal across all tested variants."
                : "most variant families remain stable under controlled variations."),
            sensitiveCount <= 1 ? "SUPPORTED" : sensitiveCount <= 2 ? "CONDITIONAL" : "INCONCLUSIVE",
            new() { ["sensitive_families"] = sensitiveCount }));

        if (result.ConflictNotes.Count > 0)
            interps.Add(new UniversalityInterpretation(
                $"Conflicts: {string.Join("; ", result.ConflictNotes)}",
                "INCONCLUSIVE",
                new() { ["conflict_count"] = result.ConflictNotes.Count }));

        interps.Add(new UniversalityInterpretation(
            $"Universality score: {result.UniversalityScore:F2} (1.0 = all variants match baseline).",
            result.UniversalityScore > 0.8 ? "SUPPORTED" :
            result.UniversalityScore > 0.6 ? "CONDITIONAL" : "INCONCLUSIVE",
            new() { ["universality"] = result.UniversalityScore }));

        return interps;
    }
}
