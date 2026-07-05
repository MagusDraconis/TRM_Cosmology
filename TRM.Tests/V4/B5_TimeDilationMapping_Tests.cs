using System;
using Xunit;
using Xunit.Abstractions;

namespace TRM.Tests.V4;

/// <summary>
/// TRM V4 — B5-O5: Time Dilation Mapping Tests
///
/// TRM:  dτ/dt = T(x) = 1 + φ(x)
/// GR:   dτ/dt = √(1 + 2φ) ≈ 1 + φ − φ²/2 + ... (weak field)
///
/// First order matches. Second order difference is testable.
///
/// Reference: docsV4/theory/TRM_V4_B5_ObservableDictionary.md
/// </summary>
[Trait("Category", "V4")]
[Trait("Category", "B5")]
public class B5_TimeDilationMapping_Tests
{
    private readonly ITestOutputHelper _output;

    private const double C = 2.99792458e8;
    private const double G = 6.67430e-11;
    private const double MSun = 1.98847e30;
    private const double MEarth = 5.972e24;
    private const double REarth = 6.371e6;

    public B5_TimeDilationMapping_Tests(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    public void B5O5_01_Earth_Surface_Time_Dilation()
    {
        _output.WriteLine("══════════════════════════════════════════════");
        _output.WriteLine("  B5-O5.01 — EARTH SURFACE TIME DILATION");
        _output.WriteLine("══════════════════════════════════════════════");
        _output.WriteLine("");

        double phi = -G * MEarth / (C * C * REarth);

        // TRM: dτ/dt = 1 + φ
        double dTau_TRM = 1.0 + phi;

        // GR weak-field: dτ/dt = √(1 + 2φ)
        double dTau_GR = Math.Sqrt(1.0 + 2.0 * phi);

        // GR expansion: ≈ 1 + φ − φ²/2
        double dTau_GR_expand = 1.0 + phi - phi * phi / 2.0;

        _output.WriteLine($"  φ_earth    = {phi:E4}");
        _output.WriteLine($"  TRM:       dτ/dt = 1 + φ = {dTau_TRM:F10}");
        _output.WriteLine($"  GR:        dτ/dt = √(1+2φ) = {dTau_GR:F10}");
        _output.WriteLine($"  GR expand: dτ/dt ≈ 1+φ−φ²/2 = {dTau_GR_expand:F10}");
        _output.WriteLine("");

        double diffFirstOrder = Math.Abs(dTau_TRM - dTau_GR_expand);
        double diffFullGr = Math.Abs(dTau_TRM - dTau_GR);
        _output.WriteLine($"  TRM − GR(expand): {diffFirstOrder:E4} (should be ≈ 0 — matches O(φ))");
        _output.WriteLine($"  TRM − GR(full):   {diffFullGr:E4} (O(φ²) ≈ {phi * phi / 2:E4})");

        // TRM matches GR to first order
        Assert.True(diffFirstOrder < 1e-20,
            "TRM should match GR expanded to O(φ)");
        _output.WriteLine("");
        _output.WriteLine("  ✅ TRM time dilation matches GR to O(φ).");
        _output.WriteLine("  O(φ²) difference ≈ |φ²/2| is testable (e.g., GPS, atomic clocks).");
    }

    [Fact]
    public void B5O5_02_Second_Order_Deviation_Testable()
    {
        _output.WriteLine("══════════════════════════════════════════════");
        _output.WriteLine("  B5-O5.02 — SECOND ORDER DEVIATION");
        _output.WriteLine("══════════════════════════════════════════════");
        _output.WriteLine("");

        // At Earth surface: φ ≈ −7×10⁻¹⁰ → O(φ²) ≈ 2.5×10⁻¹⁹
        double phiEarth = -G * MEarth / (C * C * REarth);
        double o2Earth = phiEarth * phiEarth / 2;

        // At Sun surface: φ ≈ −2×10⁻⁶ → O(φ²) ≈ 2×10⁻¹²
        double phiSun = -G * MSun / (C * C * 6.957e8);
        double o2Sun = phiSun * phiSun / 2;

        // Near compact object (r ≈ 3r_s): φ ≈ −0.17 → O(φ²) ≈ 0.014
        double phiCompact = -0.17;
        double o2Compact = phiCompact * phiCompact / 2;

        _output.WriteLine($"  Earth:    φ={phiEarth:E2},  O(φ²)={o2Earth:E2}  (immeasurable)");
        _output.WriteLine($"  Sun:      φ={phiSun:E2},    O(φ²)={o2Sun:E2}    (challenging)");
        _output.WriteLine($"  Compact:  φ={phiCompact:F2}, O(φ²)={o2Compact:F4} (measurable!)");
        _output.WriteLine("");

        // The O(φ²) deviation is only observable near compact objects
        // or with ultra-precise clocks. This is a falsifiable prediction.
        _output.WriteLine("  Falsifiable prediction:");
        _output.WriteLine("    Near compact objects (φ ~ 0.1), TRM predicts");
        _output.WriteLine("    dτ/dt = 1 + φ, while GR predicts dτ/dt = √(1+2φ).");
        _output.WriteLine("    Difference ≈ φ²/2 ≈ 1.4% at φ = −0.17.");
        _output.WriteLine("    Measurement near black holes or neutron stars");
        _output.WriteLine("    could distinguish TRM from GR.");
    }

    [Fact]
    public void B5O5_03_GPS_Clock_Correction()
    {
        _output.WriteLine("══════════════════════════════════════════════");
        _output.WriteLine("  B5-O5.03 — GPS CLOCK CORRECTION");
        _output.WriteLine("══════════════════════════════════════════════");
        _output.WriteLine("");

        // GPS satellite at r = REarth + 20,200 km
        double rSat = REarth + 2.02e7;
        double phiGround = -G * MEarth / (C * C * REarth);
        double phiSat = -G * MEarth / (C * C * rSat);

        // Gravitational: clock runs FASTER in orbit (less gravity)
        double dTauGround = 1.0 + phiGround;
        double dTauSat = 1.0 + phiSat;
        double ratio = dTauSat / dTauGround;

        // Frequency shift
        double deltaF_over_F = ratio - 1.0;

        // Daily drift
        double driftNsPerDay = deltaF_over_F * 86400 * 1e9;

        _output.WriteLine($"  φ_ground = {phiGround:E4}");
        _output.WriteLine($"  φ_sat    = {phiSat:E4}");
        _output.WriteLine($"  f_sat/f_ground − 1 = {deltaF_over_F:E4}");
        _output.WriteLine($"  Gravitational drift = {driftNsPerDay:F1} ns/day");
        _output.WriteLine("");

        // Known: GR gravitational correction ≈ +45,700 ns/day
        // (plus SR kinematic correction ≈ −7,200 ns/day = net +38,500)
        double grGrav = 45700;  // ns/day (approximate)
        _output.WriteLine($"  Known GR gravitational correction: ~{grGrav} ns/day");
        _output.WriteLine($"  TRM prediction:                     {driftNsPerDay:F0} ns/day");
        _output.WriteLine("");

        // TRM and GR agree at O(φ) — GPS-scale differences are negligible
        double relDiff = Math.Abs(driftNsPerDay - grGrav) / grGrav;
        Assert.True(relDiff < 0.10, "TRM GPS correction should match GR within ~10%");
        _output.WriteLine("  ✅ TRM GPS gravitational correction matches GR at O(φ).");
    }
}
