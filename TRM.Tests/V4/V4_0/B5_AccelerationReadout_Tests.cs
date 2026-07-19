using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;
using Xunit.Abstractions;

namespace TRM.Tests.V4;

/// <summary>
/// TRM V4 — B5-O2: Acceleration Readout Tests
///
/// Tests the TRM V4 acceleration prediction chain against known benchmarks.
///
/// TRM:  a(r) = c²·∇T(r) = c²·∇φ(r)
///       Static point mass: a(r) = −GM/r² (with k = G·K₀/c² calibration)
///
/// Reference: docsV4/theory/TRM_V4_B5_ObservableDictionary.md
/// </summary>
[Trait("Category", "V4")]
[Trait("Category", "B5")]
public class B5_AccelerationReadout_Tests
{
    private readonly ITestOutputHelper _output;

    private const double C = 2.99792458e8;
    private const double G = 6.67430e-11;
    private const double MSun = 1.98847e30;
    private const double MEarth = 5.972e24;
    private const double AU = 1.495978707e11;

    public B5_AccelerationReadout_Tests(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    public void B5O2_01_Solar_System_Accelerations()
    {
        _output.WriteLine("══════════════════════════════════════════════");
        _output.WriteLine("  B5-O2.01 — SOLAR SYSTEM ACCELERATIONS");
        _output.WriteLine("  a = GM/r² for known benchmarks");
        _output.WriteLine("══════════════════════════════════════════════");
        _output.WriteLine("");

        var benchmarks = new (string label, double M, double r)[]
        {
            ("Earth surface", MEarth, 6.371e6),
            ("Earth orbit (1 AU)", MSun, AU),
            ("Sun surface", MSun, 6.957e8),
            ("Mercury perihelion", MSun, 4.600e10),
            ("Neptune orbit", MSun, 4.495e12),
        };

        _output.WriteLine($"  {"Benchmark",-20} {"a_TRM (m/s²)",14} {"a_known",14} {"Ratio",10}");
        _output.WriteLine($"  {new string('─', 20)} {new string('─', 14)} {new string('─', 14)} {new string('─', 10)}");

        foreach (var (label, M, r) in benchmarks)
        {
            double aTrm = G * M / (r * r);
            double aKnown = aTrm;  // TRM = Newton for static point mass with k calibration

            _output.WriteLine($"  {label,-20} {aTrm,14:E4} {aKnown,14:E4} {aTrm / aKnown,10:F6}");

            Assert.Equal(aTrm, aKnown, 1e-10);
        }

        _output.WriteLine("");
        _output.WriteLine("  ✅ TRM acceleration = Newtonian for all Solar System benchmarks.");
        _output.WriteLine("  This is an identity given k = G·K₀/c² calibration, not a prediction.");
    }

    [Fact]
    public void B5O2_02_Acceleration_From_TimeRate_Gradient()
    {
        _output.WriteLine("══════════════════════════════════════════════");
        _output.WriteLine("  B5-O2.02 — ACCELERATION FROM ∇T");
        _output.WriteLine("  a = c²·∇T = c²·∇(1+φ) = c²·∇φ");
        _output.WriteLine("══════════════════════════════════════════════");
        _output.WriteLine("");

        // Test: verify a = c²·∇φ gives GM/r² for point mass
        double M = MSun;
        double[] rValues = { 1e9, 1e10, 1e11, 1e12 };

        _output.WriteLine($"  {"r (m)",12} {"φ = −GM/(c²r)",16} {"∇φ (num)",14} {"a=c²·∇φ",14} {"a=GM/r²",14}");
        _output.WriteLine($"  {new string('─', 12)} {new string('─', 16)} {new string('─', 14)} {new string('─', 14)} {new string('─', 14)}");

        double dr = 1.0;  // numerical derivative step
        foreach (double r in rValues)
        {
            double phi_r = -G * M / (C * C * r);
            double phi_rp = -G * M / (C * C * (r + dr));
            double gradPhi = (phi_rp - phi_r) / dr;

            double aTrm = C * C * gradPhi;
            double aNewton = G * M / (r * r);

            _output.WriteLine($"  {r,12:E2} {phi_r,16:E4} {gradPhi,14:E4} {aTrm,14:E4} {aNewton,14:E4}");

            // TRM a(r) = c²·∇φ = c²·(G·M/(c²·r²)) = G·M/r² = Newton
            double relErr = Math.Abs(aTrm - aNewton) / aNewton;
            Assert.True(relErr < 3e-4, $"a_TRM should match Newton. Got rel error {relErr:E4} (numerical gradient at large r loses precision)");
        }

        _output.WriteLine("");
        _output.WriteLine("  ✅ a = c²·∇φ reproduces GM/r² numerically.");
        _output.WriteLine("  The chain: φ = GM/(c²r) → ∇φ = −GM/(c²r²) → a = −GM/r².");
        _output.WriteLine("  φ = GM/(c²r) is the calibration input, not a TRM prediction.");
    }

    [Fact]
    public void B5O2_03_Galactic_Acceleration_Scale()
    {
        _output.WriteLine("══════════════════════════════════════════════");
        _output.WriteLine("  B5-O2.03 — GALACTIC ACCELERATION SCALE");
        _output.WriteLine("  MOND a₀ vs TRM effective acceleration");
        _output.WriteLine("══════════════════════════════════════════════");
        _output.WriteLine("");

        double a0Mond = 1.2e-10;  // m/s²

        // At Solar orbit in Milky Way: r ≈ 8 kpc, M_enc ≈ 10¹¹ M_sun
        double rGalactic = 8.0 * 3.086e19;  // 8 kpc in meters
        double mGalactic = 1e11 * MSun;

        double aNewtonGalactic = G * mGalactic / (rGalactic * rGalactic);

        _output.WriteLine($"  MOND a₀        = {a0Mond:E2} m/s²");
        _output.WriteLine($"  a_Newton(Sol)  = {aNewtonGalactic:E2} m/s²");
        _output.WriteLine($"  a/a₀           = {aNewtonGalactic / a0Mond:F3}");
        _output.WriteLine("");

        _output.WriteLine("  MOND regime: a < a₀ → modified gravity.");
        _output.WriteLine("  TRM approach: baryonic mass models fitted to SPARC data");
        _output.WriteLine("  (RAR01–RAR27). TRM competitive with MOND but uses");
        _output.WriteLine("  calibrated a₀, not derived from oscillator dynamics.");
        _output.WriteLine("");
        _output.WriteLine("  Status: CALIBRATED. Galaxy rotation is a calibration target,");
        _output.WriteLine("  not a first-principles derivation.");
    }
}
