namespace TRM.Core.V4_1.Dispersion;

public sealed record DirectionalDispersionResult(
    int Dimension,
    string Direction,
    DispersionFitResult Fit,
    double AnisotropyIndex);
