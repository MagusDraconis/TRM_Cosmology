namespace TRM.Core.Cosmology;

public sealed record CosmologyRotationResult(
    string GalaxyName,
    IReadOnlyList<CosmologyRotationPoint> Points,
    double VFlatKmS,
    double Lambda);
