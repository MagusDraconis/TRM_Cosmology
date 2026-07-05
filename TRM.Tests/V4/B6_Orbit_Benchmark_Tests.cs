using System;
using Xunit;
using Xunit.Abstractions;

namespace TRM.Tests.V4;

/// <summary>
/// B6: Orbit Benchmark Tests
/// Benchmarks: B-O2 (Kepler 3rd law)
/// </summary>
[Trait("Category", "V4")]
[Trait("Category", "B6")]
public class B6_Orbit_Benchmark_Tests
{
    private readonly ITestOutputHelper _output;
    private const double G = 6.67430e-11;
    private const double MSun = 1.98847e30;
    private const double Year = 365.25 * 86400;

    public B6_Orbit_Benchmark_Tests(ITestOutputHelper o) { _output = o; }

    [Fact]
    public void BO2_Kepler_Third_Law()
    {
        // T² ∝ a³ for planets — test with Earth, Mars, Jupiter
        var planets = new (string name, double a_AU, double T_yr)[]
        {
            ("Earth",  1.000, 1.000),
            ("Mars",   1.524, 1.881),
            ("Jupiter", 5.203, 11.862),
            ("Saturn",  9.537, 29.457),
        };

        _output.WriteLine("B-O2: Kepler T² ∝ a³");
        _output.WriteLine($"  {"Planet",-10} {"a (AU)",8} {"T (yr)",8} {"T²/a³",10}");
        _output.WriteLine($"  {new string('─', 10)} {new string('─', 8)} {new string('─', 8)} {new string('─', 10)}");

        foreach (var (name, a, T) in planets)
        {
            double ratio = T * T / (a * a * a);
            _output.WriteLine($"  {name,-10} {a,8:F3} {T,8:F3} {ratio,10:F4}");
            Assert.True(Math.Abs(ratio - 1.0) < 0.02, $"{name}: T²/a³ should ≈ 1");
        }

        _output.WriteLine("B-O2: Kepler 3rd law confirmed ✓");
    }

    [Fact]
    public void BO2_Kepler_From_Acceleration()
    {
        // Circular orbit: a_c = v²/r = GM/r² → v = √(GM/r) → T = 2πr/v = 2π√(r³/GM)
        double aEarth = 1.496e11;
        double vEarth = Math.Sqrt(G * MSun / aEarth);
        double TEarth = 2.0 * Math.PI * aEarth / vEarth;
        double TEarthYr = TEarth / Year;

        _output.WriteLine($"  Earth: T = 2π√(r³/GM) = {TEarthYr:F3} yr (expect 1.000)");
        Assert.True(Math.Abs(TEarthYr - 1.0) < 0.001);
    }

    [Fact]
    public void BO2_Galactic_Rotation_Keplerian()
    {
        // Sun's orbit in Milky Way: r ≈ 8 kpc, v ≈ 220 km/s
        double rKpc = 8.0;
        double rM = rKpc * 3.086e19;
        double vObs = 220e3;  // m/s

        // Keplerian: M_enc = v²r/G
        double mEnc = vObs * vObs * rM / G;
        double mEncSolar = mEnc / MSun;

        _output.WriteLine($"  M_enc(r=8kpc, v=220km/s) = {mEncSolar:E2} M_sun");
        _output.WriteLine($"  Expected (Milky Way): ~10¹¹ M_sun");
        _output.WriteLine("  Note: Keplerian mass ≈ visible mass → flat rotation");
        _output.WriteLine("  requires additional mass (DM) or modified gravity (TRM/MOND).");
    }
}
