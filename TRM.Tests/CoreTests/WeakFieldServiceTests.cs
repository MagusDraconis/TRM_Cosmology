using TRM.Core.WeakField;

namespace TRM.Tests.CoreTests;

public class WeakFieldServiceTests
{
    [Fact]
    public void ComputePpn_Should_Mark_OptimizedTarget_As_Pass()
    {
        var service = new WeakFieldService();

        var result = service.ComputePpn(new PpnInput(0.3, 1.248));

        Assert.True(result.IsOptimizedZone);
        Assert.True(result.IsWithinBounds);
        Assert.Equal(1.0, result.BetaPpn, 8);
        Assert.Equal(1.0, result.GammaPpn, 8);
    }

    [Fact]
    public void ComputePpn_Should_Fail_When_Parameters_Are_Far_From_Target()
    {
        var service = new WeakFieldService();

        var result = service.ComputePpn(new PpnInput(0.8, 2.2));

        Assert.False(result.IsOptimizedZone);
        Assert.False(result.IsWithinBounds);
        Assert.True(result.BetaPpn > 1.0);
        Assert.True(result.GammaPpn > 1.0);
    }

    [Fact]
    public void ComputeRedshiftSeries_Should_Decrease_With_Radius()
    {
        var service = new WeakFieldService();

        var series = service.ComputeRedshiftSeries(WeakFieldBodyPreset.Earth, maxRadiusMultiplier: 10, samples: 30);

        Assert.True(series.Count >= 8);
        Assert.True(series[0].TrmDeltaNuOverNu > series[^1].TrmDeltaNuOverNu);
        Assert.True(series[0].GrDeltaNuOverNu > series[^1].GrDeltaNuOverNu);
    }

    [Fact]
    public void ComputeLightDeflection_Should_Match_Solar_Baseline_At_OneSolarRadius()
    {
        var service = new WeakFieldService();

        var result = service.ComputeLightDeflection(WeakFieldBodyPreset.Sun, impactParameterMultiplier: 1.0);

        Assert.InRange(result.GrDeflectionArcSeconds, 1.70, 1.80);
        Assert.InRange(result.TrmDeflectionArcSeconds, 1.70, 1.80);
    }

    [Fact]
    public void ComputePerihelion_Should_Produce_Mercury_Value_Near_Observed()
    {
        var service = new WeakFieldService();

        var result = service.ComputePerihelion(PerihelionPlanetPreset.Mercury);

        Assert.InRange(result.GrArcSecPerCentury, 42.0, 44.0);
        Assert.InRange(result.TrmArcSecPerCentury, 42.0, 44.0);
        Assert.True(result.IsWithinTolerance);
    }
}
