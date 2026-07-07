namespace TRM.Core.V4_1.Dispersion;

public sealed record DispersionFitResult(
    double Slope,
    double Intercept,
    double FitError,
    int PointCount);
