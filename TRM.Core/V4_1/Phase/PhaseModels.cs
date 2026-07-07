namespace TRM.Core.V4_1.Phase;

// ── Phase models ────────────────────────────────────────────────

public sealed record PhasePoint(
    double K,
    double Spread,
    double DefectStrength);

public sealed record DimensionWinnerSample(
    PhasePoint Point,
    int WinnerDimension,
    double WinnerScore,
    double RunnerUpScore,
    double Margin,
    string ConfidenceLabel);

public sealed record PhaseRegion(
    string Label,
    int WinnerDim,
    List<DimensionWinnerSample> Points,
    double FractionOfTotal);

public sealed record PhaseStructureResult(
    List<DimensionWinnerSample> Winners,
    List<PhaseRegion> Regions,
    double D3CoverageFraction,
    double MeanD3Margin);

public sealed record PhaseInterpretation(
    string Statement,
    string Classification,
    Dictionary<string, double> Metrics);
