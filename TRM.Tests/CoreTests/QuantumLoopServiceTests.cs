using TRM.Core.QuantumLoops;

namespace TRM.Tests.CoreTests;

public class QuantumLoopServiceTests
{
    [Fact]
    public void ComputeUvLoopSeries_Should_Return_ValidFiniteSeries()
    {
        var service = new QuantumLoopService();
        var result = service.ComputeUvLoopSeries(new UvLoopInput(
            Kappa: 0.3,
            B: 1.248,
            LambdaSquared: 400,
            PMin: 0,
            PMax: 4000,
            Samples: 80));

        Assert.Equal(80, result.Count);
        Assert.All(result, point =>
        {
            Assert.False(double.IsNaN(point.Kernel));
            Assert.False(double.IsInfinity(point.Kernel));
            Assert.True(point.OneLoopIntegrand >= 0);
            Assert.True(point.TwoLoopIntegrand >= 0);
        });
    }

    [Fact]
    public void ComputeUvLoopSeries_Should_DecayAtHighMomentum()
    {
        var service = new QuantumLoopService();
        var result = service.ComputeUvLoopSeries(new UvLoopInput(
            Kappa: 0.3,
            B: 1.248,
            LambdaSquared: 400,
            PMin: 0,
            PMax: 4000,
            Samples: 80));

        var first = result.First().OneLoopIntegrand;
        var last = result.Last().OneLoopIntegrand;

        Assert.True(last < first, "Expected UV suppression at high momentum.");
    }
}
