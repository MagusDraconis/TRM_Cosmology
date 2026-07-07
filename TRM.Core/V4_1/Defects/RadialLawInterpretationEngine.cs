namespace TRM.Core.V4_1.Defects;

public static class RadialLawInterpretationEngine
{
    public static List<RadialLawInterpretation> Interpret(List<RadialLawAuditResult> results)
    {
        var interps = new List<RadialLawInterpretation>();

        // Regime structure.
        int asymptoticCount = results.Count(r => r.RegimeResult.RegimeLabel == "asymptotic");
        interps.Add(new RadialLawInterpretation(
            asymptoticCount > 0
                ? $"The response shows a lattice-dominated inner core and a cleaner outer-shell scaling regime in {asymptoticCount}/4 dimensions."
                : "Outer-shell scaling remains close to lattice-dominated across tested dimensions.",
            asymptoticCount >= 3 ? "SUPPORTED" : asymptoticCount >= 1 ? "CONDITIONAL" : "INCONCLUSIVE",
            new() { ["asymptotic_count"] = asymptoticCount }));

        // Outer exponents.
        var outerExps = results.Select(r => r.RegimeResult.OuterExponent).ToList();
        double meanOuter = outerExps.Average();
        double outerSpread = outerExps.Max() - outerExps.Min();
        interps.Add(new RadialLawInterpretation(
            meanOuter > 0.5
                ? $"Outer-shell exponents tend toward p≈{meanOuter:F1}, consistent with power-law decay."
                : "Outer-shell exponents remain near zero or negative under tested parameters.",
            meanOuter > 0.5 && outerSpread < 2.0 ? "SUPPORTED" : "CONDITIONAL",
            new() { ["mean_outer_p"] = meanOuter, ["spread"] = outerSpread }));

        // Best outer-regime D.
        var best = results.MinBy(r => Math.Abs(r.RegimeResult.OuterExponent - 1.0))!;
        interps.Add(new RadialLawInterpretation(
            $"D={best.Dimension} outer exponent ({best.RegimeResult.OuterExponent:F2}) closest to 1.0.",
            "CONDITIONAL",
            new() { ["best_D"] = best.Dimension, ["outer_p"] = best.RegimeResult.OuterExponent }));

        return interps;
    }
}
