namespace TRM.Core.V4_1.Dispersion;

public sealed record DispersionCurve(
    int Dimension,
    string Direction,
    List<DispersionSample> Samples,
    DispersionFitResult Fit);
