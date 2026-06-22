namespace TRM.Core.Shared;

public record DistanceMappingResult(
    double Z,
    double TrmBaseDistance,
    double TrmAngularDiameterDistance,
    double TrmLuminosityDistance,
    double? InputGrDistance,
    double? ConversionFactor
);
