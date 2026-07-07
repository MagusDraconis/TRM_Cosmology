namespace TRM.Core.V4_1.Reduction;

public static class MechanismEquationInterpretationEngine
{
    public static List<MechanismEquationInterpretation> Interpret(MechanismEquationResult result)
    {
        var interps = new List<MechanismEquationInterpretation>();

        foreach (var t in result.PrimaryTerms)
            interps.Add(new MechanismEquationInterpretation(
                $"The {t} term maps cleanly to its mechanism and remains a primary driver.",
                "SUPPORTED",
                new() { ["primary_count"] = result.PrimaryTerms.Count }));

        foreach (var t in result.CorrectionTerms)
            interps.Add(new MechanismEquationInterpretation(
                $"The {t} term behaves as a leading geometric correction rather than a core driver.",
                "SUPPORTED",
                new() { ["correction_count"] = result.CorrectionTerms.Count }));

        foreach (var t in result.MixedTerms)
            interps.Add(new MechanismEquationInterpretation(
                $"The {t} term is partly composite, reflecting multiple mechanism channels.",
                "CONDITIONAL",
                new() { ["mixed_count"] = result.MixedTerms.Count }));

        interps.Add(new MechanismEquationInterpretation(
            result.UnexplainedResidual < 0.3
                ? "Mechanism-to-term mapping is largely complete with small unexplained residual."
                : "Substantial unexplained residual remains — some effective terms lack clear mechanism attribution.",
            result.UnexplainedResidual < 0.3 ? "SUPPORTED" : "CONDITIONAL",
            new() { ["unexplained"] = result.UnexplainedResidual }));

        interps.Add(new MechanismEquationInterpretation(
            result.RedundancyScore > 0.2
                ? $"Detectable redundancy (score: {result.RedundancyScore:F2}) — some mechanisms overlap in term-space."
                : "Low redundancy across term assignments.",
            result.RedundancyScore > 0.3 ? "CONDITIONAL" : "SUPPORTED",
            new() { ["redundancy"] = result.RedundancyScore }));

        return interps;
    }
}
