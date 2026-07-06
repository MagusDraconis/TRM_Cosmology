namespace TRM.Core.StrongField;

public sealed record StrongFieldPoint(
    double RadiusMeters,
    double RadiusInSchwarzschildUnits,
    double RegularG00,
    double RegularGrr,
    double SchwarzschildG00,
    double SchwarzschildGrr);
