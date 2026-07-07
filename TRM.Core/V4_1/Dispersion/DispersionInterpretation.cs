namespace TRM.Core.V4_1.Dispersion;

public sealed record DispersionInterpretation(
    string Statement,
    string Classification,
    Dictionary<string, double> Metrics);
