namespace TRM.Core.Cosmology;

public sealed record CosmologyGalaxySummary(
    string Name,
    double VFlatKmS,
    double VFlatErrorKmS,
    double BaryonicMassSolar,
    int QualityFlag);
