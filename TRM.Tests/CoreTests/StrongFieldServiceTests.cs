using TRM.Core.StrongField;

namespace TRM.Tests.CoreTests;

public class StrongFieldServiceTests
{
    [Fact]
    public void ComputeRegularSchwarzschild_Should_Return_FiniteCore_And_Profile()
    {
        var service = new StrongFieldService();

        var result = service.ComputeRegularSchwarzschild(new StrongFieldInput(
            MassSolarUnits: 5,
            CoreScaleFactor: 0.25,
            Samples: 180,
            MaxRadiusInSchwarzschildUnits: 12));

        Assert.True(result.HasFiniteCore);
        Assert.True(result.CenterG00 > 0);
        Assert.Equal(180, result.Profile.Count);
    }

    [Fact]
    public void ComputeRegularSchwarzschild_Should_Detect_At_Least_One_Horizon_For_Stellar_Mass()
    {
        var service = new StrongFieldService();

        var result = service.ComputeRegularSchwarzschild(new StrongFieldInput(
            MassSolarUnits: 10,
            CoreScaleFactor: 0.2,
            Samples: 220,
            MaxRadiusInSchwarzschildUnits: 15));

        Assert.True(result.InnerHorizonMeters.HasValue || result.OuterHorizonMeters.HasValue);
    }

    [Fact]
    public void ComputeRegularSchwarzschild_Should_Keep_RegularG00_Finite_In_Profile()
    {
        var service = new StrongFieldService();

        var result = service.ComputeRegularSchwarzschild(new StrongFieldInput(
            MassSolarUnits: 3,
            CoreScaleFactor: 0.3,
            Samples: 120,
            MaxRadiusInSchwarzschildUnits: 10));

        Assert.All(result.Profile, p =>
        {
            Assert.False(double.IsNaN(p.RegularG00));
            Assert.False(double.IsInfinity(p.RegularG00));
        });
    }
}
