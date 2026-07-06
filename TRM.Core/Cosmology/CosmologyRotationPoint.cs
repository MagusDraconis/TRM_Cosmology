namespace TRM.Core.Cosmology;

public sealed record CosmologyRotationPoint(
    double RadiusKpc,
    double ObservedKmS,
    double ObservedErrorKmS,
    double NewtonGrKmS,
    double TheoryKmS);
