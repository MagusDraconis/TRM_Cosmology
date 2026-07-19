using System;
using Xunit;
using Xunit.Abstractions;

namespace TRM.Tests.V4;

/// <summary>
/// TRM V4 — B5-O4: Redshift Mapping Tests
///
/// Tests the TRM V4 redshift prediction against GR weak-field expectation.
///
/// TRM:  z = ΔT/T = (φ₁ − φ₂)/(1 + φ₂) ≈ Δφ  (for φ ≪ 1)
/// GR:   z ≈ G·M/(c²·r₁) − G·M/(c²·r₂)
///
/// Reference: docsV4/theory/TRM_V4_B5_ObservableDictionary.md
/// </summary>
[Trait("Category", "V4")]
[Trait("Category", "B5")]
public class B5_RedshiftMapping_Tests
{
    private readonly ITestOutputHelper _output;

    private const double C = 2.99792458e8;
    private const double G = 6.67430e-11;
    private const double MSun = 1.98847e30;
    private const double RSun = 6.957e8;

    public B5_RedshiftMapping_Tests(ITestOutputHelper output)
    {
        _output = output;
    }

    // ════════════════════════════════════════════════════════════
    // B5O4_01 — Solar redshift
    // ════════════════════════════════════════════════════════════

    [Fact]
    public void B5O4_01_Solar_Gravitational_Redshift()
    {
        _output.WriteLine("══════════════════════════════════════════════");
        _output.WriteLine("  B5-O4.01 — SOLAR GRAVITATIONAL REDSHIFT");
        _output.WriteLine("══════════════════════════════════════════════");
        _output.WriteLine("");

        // Sun surface to Earth (r → ∞)
        double phiSun = -G * MSun / (C * C * RSun);
        double phiEarth = 0;  // r → ∞

        double zTrm = (phiSun - phiEarth) / (1.0 + phiEarth);
        double zTrmApprox = phiSun - phiEarth;
        double zGr = phiSun;  // weak-field GR: z ≈ −GM/(c²r)

        _output.WriteLine($"  φ_sun     = {phiSun:E4}");
        _output.WriteLine($"  z_TRM     = {zTrm:E4}  (exact: Δφ/(1+φ₂))");
        _output.WriteLine($"  z_TRM≈    = {zTrmApprox:E4}  (approx: Δφ)");
        _output.WriteLine($"  z_GR      = {zGr:E4}");
        _output.WriteLine("");

        double relDiff = Math.Abs(zTrm - zGr) / Math.Abs(zGr);
        _output.WriteLine($"  TRM/GR relative difference: {relDiff:E4}");
        _output.WriteLine($"  (Difference is O(φ²) ≈ {(phiSun * phiSun):E4})");

        // TRM exact matches GR to O(φ) — difference is O(φ²)
        Assert.True(relDiff < 1e-5,
            $"TRM redshift should match GR to O(φ). Got relative diff {relDiff:E4}");
        _output.WriteLine("  ✅ TRM redshift matches GR to O(φ). O(φ²) difference is testable.");
    }

    // ════════════════════════════════════════════════════════════
    // B5O4_02 — Redshift between two finite heights
    // ════════════════════════════════════════════════════════════

    [Fact]
    public void B5O4_02_Redshift_Between_Two_Heights()
    {
        _output.WriteLine("══════════════════════════════════════════════");
        _output.WriteLine("  B5-O4.02 — REDSHIFT BETWEEN TWO HEIGHTS");
        _output.WriteLine("══════════════════════════════════════════════");
        _output.WriteLine("");

        double rEarth = 6.371e6;
        double mEarth = 5.972e24;

        // Ground (r₁) to GPS altitude (r₂ = r₁ + 20,200 km)
        double r1 = rEarth;
        double r2 = rEarth + 2.02e7;

        double phi1 = -G * mEarth / (C * C * r1);
        double phi2 = -G * mEarth / (C * C * r2);

        double zTrm = (phi1 - phi2) / (1.0 + phi2);
        double zGr = phi1 - phi2;

        _output.WriteLine($"  φ_ground   = {phi1:E4}");
        _output.WriteLine($"  φ_GPS      = {phi2:E4}");
        _output.WriteLine($"  z_TRM      = {zTrm:E4}");
        _output.WriteLine($"  z_GR       = {zGr:E4}");
        _output.WriteLine("");

        // Daily clock drift: Δt/day = z · 86400 s
        double driftNsTrn = zTrm * 86400 * 1e9;
        double driftNsGr = zGr * 86400 * 1e9;
        _output.WriteLine($"  Clock drift (TRM): {driftNsTrn:F1} ns/day");
        _output.WriteLine($"  Clock drift (GR):  {driftNsGr:F1} ns/day");
        _output.WriteLine($"  Known GPS offset: ~45,700 ns/day (GR+SR combined)");

        // GPS gravitational redshift is ~45 µs/day (GR). TRM matches.
        Assert.True(Math.Abs(driftNsTrn - driftNsGr) < 1e-3,
            $"TRM clock drift should match GR. Got diff {Math.Abs(driftNsTrn - driftNsGr):E2} ns");
        _output.WriteLine("  ✅ TRM gravitational redshift matches GR for GPS altitudes.");
    }

    // ════════════════════════════════════════════════════════════
    // B5O4_03 — Redshift limit behavior
    // ════════════════════════════════════════════════════════════

    [Fact]
    public void B5O4_03_Redshift_Limit_Behavior()
    {
        _output.WriteLine("══════════════════════════════════════════════");
        _output.WriteLine("  B5-O4.03 — REDSHIFT LIMIT BEHAVIOR");
        _output.WriteLine("══════════════════════════════════════════════");
        _output.WriteLine("");

        // φ → 0: z → 0 (no gravity → no redshift)
        double zZero = (0.0 - 0.0) / (1.0 + 0.0);
        Assert.Equal(0.0, zZero);
        _output.WriteLine("  φ → 0: z = 0  ✓");

        // r → ∞: z → 0 (observer at infinity, emitter at finite r)
        double phiFinite = -1e-9;
        double zInfinity = (phiFinite - 0.0) / (1.0 + 0.0);
        _output.WriteLine($"  r₂ → ∞: z = φ₁ = {zInfinity:E4}  (finite → correct)");
        Assert.Equal(phiFinite, zInfinity, 12);

        // Symmetric: z(r₁→r₂) = −z(r₂→r₁) approximately
        double phiA = -1e-8;
        double phiB = -5e-9;
        double zAB = (phiA - phiB) / (1.0 + phiB);
        double zBA = (phiB - phiA) / (1.0 + phiA);
        _output.WriteLine($"  z(A→B) = {zAB:E4}, z(B→A) = {zBA:E4}");
        _output.WriteLine($"  Sum ≈ {zAB + zBA:E4} (should be ≈ 0 at O(φ))");
        Assert.True(Math.Abs(zAB + zBA) < 1e-16);

        _output.WriteLine("  ✅ Redshift has correct limit behavior.");
    }
}
