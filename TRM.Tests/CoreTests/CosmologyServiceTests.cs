using TRM.Core.Cosmology;

namespace TRM.Tests.CoreTests;

public class CosmologyServiceTests
{
    [Fact]
    public void GetAvailableGalaxies_Should_Return_NonEmpty_List()
    {
        var service = new CosmologyService();

        var galaxies = service.GetAvailableGalaxies(20);

        Assert.NotEmpty(galaxies);
        Assert.All(galaxies, g => Assert.False(string.IsNullOrWhiteSpace(g.Name)));
    }

    [Fact]
    public void ComputeRotationCurves_Should_Return_Observed_Gr_Theory_Series()
    {
        var service = new CosmologyService();
        var galaxy = service.GetAvailableGalaxies(1).First().Name;

        var result = service.ComputeRotationCurves(galaxy, lambda: 1.0, samples: 40);

        Assert.Equal(galaxy, result.GalaxyName);
        Assert.Equal(40, result.Points.Count);
        Assert.All(result.Points, p =>
        {
            Assert.True(p.RadiusKpc > 0);
            Assert.True(p.ObservedKmS > 0);
            Assert.True(p.NewtonGrKmS > 0);
            Assert.True(p.TheoryKmS > 0);
        });
    }

    [Fact]
    public void ComputeRotationCurves_Should_Be_Sensitive_To_Lambda()
    {
        var service = new CosmologyService();
        var galaxy = service.GetAvailableGalaxies(1).First().Name;

        var lowLambda = service.ComputeRotationCurves(galaxy, lambda: 0.5, samples: 30);
        var highLambda = service.ComputeRotationCurves(galaxy, lambda: 2.0, samples: 30);

        var meanLow = lowLambda.Points.Average(p => p.TheoryKmS);
        var meanHigh = highLambda.Points.Average(p => p.TheoryKmS);

        Assert.True(meanLow > meanHigh);
    }
}
