namespace TRM.Core.V4_1.Reduction;

public static class ReductionInterpretationEngine
{
    public static List<ReductionInterpretation> Interpret(MinimalCoreResult result)
    {
        var interps = new List<ReductionInterpretation>();

        if (result.MinimalCores.Count > 0)
        {
            var mc = result.MinimalCores[0];
            interps.Add(new ReductionInterpretation(
                $"D=3 advantage persists with minimal subset: {string.Join(", ", mc.Mechanisms)} " +
                $"({mc.Mechanisms.Count} mechanisms).",
                "SUPPORTED",
                new() { ["minimal_size"] = mc.Mechanisms.Count }));
        }

        interps.Add(new ReductionInterpretation(
            $"Critical mechanisms (removal collapses D=3): {string.Join(", ", result.CriticalMechanisms)}.",
            "CONDITIONAL",
            new() { ["critical_count"] = result.CriticalMechanisms.Count }));

        interps.Add(new ReductionInterpretation(
            result.RedundantMechanisms.Count > 0
                ? $"Redundant mechanisms (removal preserves D=3): {string.Join(", ", result.RedundantMechanisms)}."
                : "All mechanisms contribute to D=3 advantage — none are fully redundant.",
            result.RedundantMechanisms.Count > 2 ? "SUPPORTED" : "CONDITIONAL",
            new() { ["redundant_count"] = result.RedundantMechanisms.Count }));

        int preserved = result.AllEvaluations.Count(e => e.Status == "PRESERVED");
        int weakened = result.AllEvaluations.Count(e => e.Status == "WEAKENED");
        int lost = result.AllEvaluations.Count(e => e.Status == "LOST");
        interps.Add(new ReductionInterpretation(
            $"Subset evaluation: {preserved} preserved, {weakened} weakened, {lost} lost.",
            preserved > lost ? "SUPPORTED" : "CONDITIONAL",
            new() { ["preserved"] = preserved, ["weakened"] = weakened, ["lost"] = lost }));

        return interps;
    }
}
