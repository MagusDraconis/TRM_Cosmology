namespace TRM.Core.StrongField;

public sealed record StrongFieldInput(
    double MassSolarUnits,
    double CoreScaleFactor,
    int Samples,
    double MaxRadiusInSchwarzschildUnits);
