using TRM.Core.Lattice;

namespace TRM.Tests.CoreTests;

public class LatticeServiceTests
{
    [Fact]
    public void ComputeSnapshot_Should_Return_ExpectedSeriesLengths()
    {
        var service = new LatticeService();

        var result = service.ComputeSnapshot(new LatticeInput(
            NodeCount: 48,
            Coupling: 0.8,
            InjectedEnergy: 10,
            InjectionIndex: 12));

        Assert.Equal(48, result.NodeEnergies.Count);
        Assert.Equal(25, result.Distances.Count);
        Assert.Equal(result.Distances.Count, result.Correlation.Count);
        Assert.Equal(result.Distances.Count, result.PadeKernel.Count);
    }

    [Fact]
    public void ComputeSnapshot_Should_Normalize_ZeroDistance_Correlation_To_One()
    {
        var service = new LatticeService();

        var result = service.ComputeSnapshot(new LatticeInput(
            NodeCount: 40,
            Coupling: 0.6,
            InjectedEnergy: 8,
            InjectionIndex: 5));

        Assert.InRange(result.Correlation[0], 0.999999, 1.000001);
    }

    [Fact]
    public void ComputeSnapshot_Should_Increase_MaxEnergy_When_InjectedEnergy_Increases()
    {
        var service = new LatticeService();

        var low = service.ComputeSnapshot(new LatticeInput(48, 0.8, 5, 12));
        var high = service.ComputeSnapshot(new LatticeInput(48, 0.8, 12, 12));

        Assert.True(high.NodeEnergies.Max() > low.NodeEnergies.Max());
    }
}
