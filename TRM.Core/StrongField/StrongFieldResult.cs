namespace TRM.Core.StrongField;

public sealed record StrongFieldResult(
    IReadOnlyList<StrongFieldPoint> Profile,
    double SchwarzschildRadiusMeters,
    double CoreRadiusMeters,
    double? InnerHorizonMeters,
    double? OuterHorizonMeters,
    double CenterG00,
    bool HasFiniteCore);
