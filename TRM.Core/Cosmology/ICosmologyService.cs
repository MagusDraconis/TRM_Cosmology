namespace TRM.Core.Cosmology;

public interface ICosmologyService
{
    IReadOnlyList<CosmologyGalaxySummary> GetAvailableGalaxies(int maxCount = 100);
    CosmologyRotationResult ComputeRotationCurves(string galaxyName, double lambda = 1.0, int samples = 40);
}
