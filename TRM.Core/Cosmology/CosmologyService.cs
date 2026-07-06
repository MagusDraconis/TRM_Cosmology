namespace TRM.Core.Cosmology;

public sealed class CosmologyService : ICosmologyService
{
    private const double G = 6.67430e-11;
    private const double SolarMassKg = 1.98847e30;
    private const double KpcToM = 3.08567758e19;
    private const double A0Ms2 = 1.2e-10;

    private readonly Lazy<IReadOnlyList<GalaxyData>> _galaxies;

    public CosmologyService()
    {
        _galaxies = new Lazy<IReadOnlyList<GalaxyData>>(LoadGalaxies);
    }

    public IReadOnlyList<CosmologyGalaxySummary> GetAvailableGalaxies(int maxCount = 100)
    {
        var count = Math.Clamp(maxCount, 1, 500);

        return _galaxies.Value
            .OrderBy(g => g.Name, StringComparer.OrdinalIgnoreCase)
            .Take(count)
            .Select(g => new CosmologyGalaxySummary(
                g.Name,
                g.Vflat,
                g.EVflat,
                g.MbarAbsolute,
                g.Q))
            .ToList();
    }

    public CosmologyRotationResult ComputeRotationCurves(string galaxyName, double lambda = 1.0, int samples = 40)
    {
        var galaxy = ResolveGalaxy(galaxyName);
        var safeLambda = Math.Max(0.2, lambda);
        var count = Math.Clamp(samples, 12, 120);

        var rd = EstimateDiskScaleKpc(galaxy);
        var maxRadius = Math.Max(12.0, 8.0 * rd);
        var dr = (maxRadius - 0.5) / (count - 1);

        var points = new List<CosmologyRotationPoint>(count);

        for (var i = 0; i < count; i++)
        {
            var radiusKpc = 0.5 + (i * dr);
            var radiusM = radiusKpc * KpcToM;

            var observed = galaxy.Vflat * (1.0 - Math.Exp(-radiusKpc / rd));
            var observedErr = Math.Max(1.0, galaxy.EVflat * (0.7 + 0.3 * Math.Exp(-radiusKpc / (2.0 * rd))));

            var enclosedMassSolar = galaxy.MbarAbsolute * (1.0 - Math.Exp(-radiusKpc / rd));
            var enclosedMassKg = Math.Max(1e3, enclosedMassSolar * SolarMassKg);

            var gNewton = G * enclosedMassKg / (radiusM * radiusM);
            var vNewton = Math.Sqrt(radiusM * gNewton) / 1000.0;

            var gTheory = gNewton + Math.Sqrt(gNewton * A0Ms2) / safeLambda;
            var vTheory = Math.Sqrt(radiusM * gTheory) / 1000.0;

            points.Add(new CosmologyRotationPoint(
                RadiusKpc: radiusKpc,
                ObservedKmS: observed,
                ObservedErrorKmS: observedErr,
                NewtonGrKmS: vNewton,
                TheoryKmS: vTheory));
        }

        return new CosmologyRotationResult(galaxy.Name, points, galaxy.Vflat, safeLambda);
    }

    private static IReadOnlyList<GalaxyData> LoadGalaxies()
    {
        var filePath = WorkspaceFileLocator.GetFilePath("SPARC_Lelli2016c.mrt");
        var data = SparcMrtParser.ParseFile(filePath);

        if (data.Count == 0)
        {
            throw new InvalidOperationException($"SPARC dataset parsed with zero galaxies from '{filePath}'.");
        }

        return data;
    }

    private GalaxyData ResolveGalaxy(string? galaxyName)
    {
        var galaxies = _galaxies.Value;

        if (!string.IsNullOrWhiteSpace(galaxyName))
        {
            var match = galaxies.FirstOrDefault(g => string.Equals(g.Name, galaxyName, StringComparison.OrdinalIgnoreCase));
            if (match is not null)
            {
                return match;
            }
        }

        return galaxies[0];
    }

    private static double EstimateDiskScaleKpc(GalaxyData galaxy)
    {
        // Keep within a practical range for UI curves.
        var proxy = 1.5 + 0.8 * Math.Log10(Math.Max(1.0, galaxy.MbarAbsolute));
        return Math.Clamp(proxy, 1.5, 12.0);
    }
}
