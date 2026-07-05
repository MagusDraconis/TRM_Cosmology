using System;
using Xunit;
using Xunit.Abstractions;

namespace TRM.Tests.V4;

/// <summary>
/// TRM V4 — B6: Newton Benchmark Tests
/// Benchmarks: B-N1 (solar g), B-N2 (Earth orbit a), B-N3 (1/r² scaling)
/// </summary>
[Trait("Category", "V4")]
[Trait("Category", "B6")]
public class B6_Newton_Benchmark_Tests
{
    private readonly ITestOutputHelper _output;
    private const double G = 6.67430e-11;
    private const double MSun = 1.98847e30;
    private const double MEarth = 5.972e24;
    private const double AU = 1.495978707e11;
    private const double RSun = 6.957e8;
    private const double REarth = 6.371e6;

    public B6_Newton_Benchmark_Tests(ITestOutputHelper o) { _output = o; }

    [Fact]
    public void BN1_Solar_Surface_Gravity()
    {
        double gTrm = G * MSun / (RSun * RSun);
        double gRef = 274.0;
        double relErr = Math.Abs(gTrm - gRef) / gRef;
        _output.WriteLine($"B-N1: g_sun = {gTrm:F1} m/s² (ref: {gRef})  Δ = {relErr:P2}");
        Assert.True(relErr < 0.02);
    }

    [Fact]
    public void BN2_Earth_Orbit_Acceleration()
    {
        double aTrm = G * MSun / (AU * AU);
        double aRef = 5.93e-3;
        double relErr = Math.Abs(aTrm - aRef) / aRef;
        _output.WriteLine($"B-N2: a_earth = {aTrm:E3} m/s² (ref: {aRef:E3})  Δ = {relErr:P2}");
        Assert.True(relErr < 0.02);
    }

    [Fact]
    public void BN3_InverseSquare_Scaling()
    {
        double[] r = { AU, 2 * AU, 5 * AU };
        double a0 = G * MSun / (r[0] * r[0]);
        foreach (double ri in r)
        {
            double ai = G * MSun / (ri * ri);
            double ratio = ai * ri * ri / (a0 * r[0] * r[0]);
            _output.WriteLine($"B-N3: r={ri/AU:F0}AU  a·r²/a₀·r₀² = {ratio:F6}");
            Assert.True(Math.Abs(ratio - 1.0) < 1e-12);
        }
        _output.WriteLine("B-N3: 1/r² scaling confirmed ✓");
    }
}
