namespace TRM.Core.V4_1.Synthesis;

// ── Synthesis models ────────────────────────────────────────────

/// <summary>Evidence from one diagnostic layer.</summary>
public sealed record LayerEvidenceSample(
    string LayerName,
    int PreferredDimension,
    double Confidence,
    string Classification,
    string ParameterNote);

/// <summary>Per-dimension advantage profile.</summary>
public sealed record DimensionAdvantageProfile(
    int Dimension,
    int SupportCount,
    double WeightedScore,
    List<string> SupportingLayers);

/// <summary>Aggregate synthesis result.</summary>
public sealed record DimensionSynthesisResult(
    List<LayerEvidenceSample> LayerEvidence,
    List<DimensionAdvantageProfile> Profiles,
    List<string> ConflictFlags,
    double D3AdvantageScore);

public sealed record DimensionSynthesisInterpretation(
    string Statement,
    string Classification,
    Dictionary<string, double> Metrics);
