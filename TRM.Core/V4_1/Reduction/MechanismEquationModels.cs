namespace TRM.Core.V4_1.Reduction;

// ── Mechanism-to-equation models ────────────────────────────────

public sealed record EquationTermMechanismMap(
    string EquationTerm,
    string Mechanism,
    double AttributionStrength,
    string Type);

public sealed record MechanismEquationResult(
    List<EquationTermMechanismMap> Mappings,
    List<string> PrimaryTerms,
    List<string> CorrectionTerms,
    List<string> MixedTerms,
    double UnexplainedResidual,
    double RedundancyScore);

public sealed record MechanismEquationInterpretation(
    string Statement,
    string Classification,
    Dictionary<string, double> Metrics);
