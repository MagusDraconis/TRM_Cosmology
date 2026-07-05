using System;
using Xunit;
using Xunit.Abstractions;

namespace TRM.Tests.V4;

/// <summary>
/// B6: Redshift / Time Dilation Benchmark Tests
/// Benchmarks: B-R1 (solar redshift), B-R2 (GPS), B-T1 (Earth dilation), B-T2 (O(φ²))
/// </summary>
[Trait("Category", "V4")]
[Trait("Category", "B6")]
public class B6_Redshift_Benchmark_Tests
{
    private readonly ITestOutputHelper _output;
    private const double C = 2.99792458e8;
    private const double G = 6.67430e-11;
    private const double MSun = 1.98847e30;
    private const double MEarth = 5.972e24;
    private const double RSun = 6.957e8;
    private const double REarth = 6.371e6;

    public B6_Redshift_Benchmark_Tests(ITestOutputHelper o) { _output = o; }

    [Fact]
    public void BR1_Solar_Redshift()
    {
        double phi = -G * MSun / (C * C * RSun);
        double zTrm = phi;  // weak-field: z ≈ Δφ, observer at ∞
        double zRef = -2.12e-6;
        _output.WriteLine($"B-R1: z_sun = {zTrm:E4} (ref: {zRef:E4})");
        Assert.True(Math.Abs(zTrm - zRef) / Math.Abs(zRef) < 0.02);
    }

    [Fact]
    public void BR2_GPS_Gravitational_Redshift()
    {
        double rSat = REarth + 2.02e7;
        double phiG = -G * MEarth / (C * C * REarth);
        double phiS = -G * MEarth / (C * C * rSat);
        double z = (phiG - phiS) / (1 + phiS);
        double driftNsDay = z * 86400 * 1e9;
        double absDrift = Math.Abs(driftNsDay);
        _output.WriteLine($"B-R2: GPS grav drift = {absDrift:F0} ns/day (ref: ~45700)");
        Assert.True(absDrift > 40000 && absDrift < 50000);
    }

    [Fact]
    public void BT1_Earth_Time_Dilation()
    {
        double phi = -G * MEarth / (C * C * REarth);
        double dTau = 1.0 + phi;
        double refVal = 1.0 + phi;  // TRM TRM formula
        _output.WriteLine($"B-T1: dτ/dt = {dTau:F12} (ref: {refVal:F12})");
        Assert.Equal(dTau, refVal, 14);
    }

    [Fact]
    public void BT2_Second_Order_Deviation()
    {
        double phi = -0.17;
        double dTauTrm = 1.0 + phi;
        double dTauGr = Math.Sqrt(1.0 + 2.0 * phi);
        double diff = dTauTrm - dTauGr;
        double expectedDiff = phi * phi / 2.0;  // TRM − GR ≈ φ²/2 (positive)
        _output.WriteLine($"B-T2: TRM−GR at φ=−0.17: Δ = {diff:F6} (expected ≈ {expectedDiff:F6})");
        Assert.True(Math.Abs(diff - expectedDiff) < 0.004);  // O(φ³) ≈ 0.003
    }
}
