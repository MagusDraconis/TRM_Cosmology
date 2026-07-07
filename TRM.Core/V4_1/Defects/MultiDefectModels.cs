namespace TRM.Core.V4_1.Defects;

// ── Multi-defect models ─────────────────────────────────────────

public sealed record MultiDefectConfig(
    int[] DefectNodes,
    double[] DefectStrengths);

public sealed record MultiDefectResponseResult(
    int Dimension,
    int NodeCount,
    double[] CombinedResponse,
    double[] SuperpositionPrediction,
    double[] Residuals,
    double MeanResidual,
    double MaxResidual,
    double FarFieldResidual);

public sealed record MultiDefectExperimentConfig(
    int NodesPerDim = 6,
    double DefectStrength = 0.5,
    double DefectSeparation = 3,
    int TimeSteps = 300,
    int BaseSeed = 42);

public sealed record SuperpositionInterpretation(
    string Statement,
    string Classification,
    Dictionary<string, double> Metrics);
