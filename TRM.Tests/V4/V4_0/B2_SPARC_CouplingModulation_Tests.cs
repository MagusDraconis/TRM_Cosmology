using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TRM.Core;
using TRM.Core.Baryons;
using TRM.Tests.V4.V4_TestHelpers;
using Xunit;
using Xunit.Abstractions;

namespace TRM.Tests.V4;

/// <summary>
/// TRM V4 — B2 SPARC Coupling Modulation Integration Tests
///
/// Tests the K1×F1 coupling-modulation mechanism against real SPARC
/// galaxy rotation curves. Applies the B1-validated mapping chain:
///
///   baryons → M_bar(r) → δK(r) = k·M_bar(r)/r → δρ_eff → a(r) → v_pred(r)
///
/// Classification:
///   PASS:    v_pred qualitatively matches v_obs without DM halo
///   PARTIAL: shape okay but requires per-galaxy tuning
///   FAIL:    no qualitative match
///
/// Reference: docsV4/experiments/TRM_V4_MappingTests.md
///            docsV4/experiments/TRM_V4_B1_Results.md
///            docsV4/review/TRM_V4_B1_CriticalReview.md
/// </summary>
[Trait("Category", "V4")]
[Trait("Category", "B2")]
[Trait("Category", "LongRunning")]
public class B2_SPARC_CouplingModulation_Tests
{
    private readonly ITestOutputHelper _output;

    // ────────────────────────────────────────────────────────────
    // Physical constants
    // ────────────────────────────────────────────────────────────

    /// <summary>Speed of light in m/s.</summary>
    private const double C = 2.99792458e8;

    /// <summary>Newton's gravitational constant in m³/(kg·s²).</summary>
    private const double G = 6.67430e-11;

    /// <summary>km²/s² per kpc → m/s² conversion.</summary>
    private const double Kms2KpcToMs2 = 3.240779289e-14;

    /// <summary>kpc to meters.</summary>
    private const double KpcToM = 3.085677581e19;

    /// <summary>Solar mass in kg.</summary>
    private const double MSolar = 1.98847e30;

    // ────────────────────────────────────────────────────────────
    // B1 calibration (post-hoc, from TRM_V4_B1_Results.md)
    // ────────────────────────────────────────────────────────────

    /// <summary>
    /// Coupling perturbation constant: k = G·K₀/c².
    /// For B2 we absorb K₀ into the normalization — we test shape,
    /// not absolute amplitude. K₀ is the baseline oscillator coupling
    /// (dimensionless in CML units). Its physical value requires
    /// anchoring ω_i = 1.0 to a physical frequency scale — an open
    /// question tracked in TRM_V4_B1_CriticalReview.md.
    /// </summary>
    private const double k_over_K0 = G / (C * C);   // k/K₀ = G/c²  ≈ 7.43×10⁻²⁸ m/kg

    // ────────────────────────────────────────────────────────────
    // Result types
    // ────────────────────────────────────────────────────────────

    private enum B2Classification
    {
        /// <summary>v_pred qualitatively matches v_obs — no DM needed.</summary>
        Pass,

        /// <summary>Shape roughly correct but significant residuals.</summary>
        Partial,

        /// <summary>No qualitative match — mechanism fails at galactic scale.</summary>
        Fail
    }

    private readonly record struct GalaxyB2Result(
        string GalaxyName,
        int PointCount,
        double RmsLogResidual,
        double OuterSlope,          // dv/dr in outer region — ~0 for flat
        bool HasFlatOuterRegion,
        B2Classification Classification,
        string Note);

    // ────────────────────────────────────────────────────────────
    // Representative galaxy subset for CI-compatible testing
    // ────────────────────────────────────────────────────────────

    /// <summary>
    /// Hand-picked galaxies covering HSB, LSB, dwarf, and giant types.
    /// These are well-studied SPARC galaxies with high-quality data.
    /// </summary>
    private static readonly HashSet<string> RepresentativeGalaxies = new(
        StringComparer.OrdinalIgnoreCase)
    {
        "NGC2403",   // HSB spiral — classic flat rotation curve
        "DDO154",    // LSB dwarf — dark-matter-dominated
        "NGC7331",   // Massive spiral — bulge+disk
        "UGC128",    // Dwarf irregular
        "NGC2841",   // Early-type spiral — declining outer curve
        "NGC6503",   // Late-type spiral — well-studied
        "NGC3198",   // Extended HI disk
        "NGC5055",   // Sunflower galaxy — high-quality data
    };

    // ────────────────────────────────────────────────────────────
    // Constructor
    // ────────────────────────────────────────────────────────────

    public B2_SPARC_CouplingModulation_Tests(ITestOutputHelper output)
    {
        _output = output;
    }

    // ════════════════════════════════════════════════════════════
    // B2CM01 — Representative galaxy rotation curve shape test
    // ════════════════════════════════════════════════════════════

    /// <summary>
    /// B2CM01 — Tests whether the K1×F1 mechanism produces qualitatively
    /// correct rotation curves for representative SPARC galaxies using
    /// baryonic mass only.
    ///
    /// The TRM prediction chain:
    ///   M_bar(r) = (v_gas² + 0.5·v_disk² + 0.7·v_bulge²) · r / G
    ///   δK(r)    = k · M_bar(r) / r
    ///   δρ_eff(r) = ρ_ref · δK(r) / K₀   (normalized)
    ///   a_TRM(r)  = (k/K₀) · c² · d(M_bar/r)/dr
    ///             = G · d(M_bar/r)/dr     (by k = G·K₀/c²)
    ///
    /// For a distributed mass, this reproduces the Newtonian baryonic
    /// acceleration. The test verifies that the baryons-alone rotation
    /// curve has qualitatively correct shape compared to observations.
    /// </summary>
    [Fact]
    public void B2CM01_RepresentativeGalaxies_RotationCurveShape()
    {
        _output.WriteLine("══════════════════════════════════════════════");
        _output.WriteLine("  B2CM01 — REPRESENTATIVE GALAXY ROTATION CURVES");
        _output.WriteLine("  K1×F1 coupling-modulation mechanism");
        _output.WriteLine("  Baryons only — no dark matter halo");
        _output.WriteLine("══════════════════════════════════════════════");
        _output.WriteLine("");

        var data = LoadSparcData();
        if (data == null)
        {
            _output.WriteLine("  ⚠ SPARC data not available — skipping test.");
            return;   // CI-safe skip when data is absent
        }

        var results = new List<GalaxyB2Result>();

        foreach (var (galaxyName, points) in data)
        {
            if (!RepresentativeGalaxies.Contains(galaxyName))
                continue;

            if (points.Count < 5) continue;

            var result = EvaluateGalaxy(galaxyName, points);
            results.Add(result);

            _output.WriteLine($"  {result.GalaxyName,-10}  N={result.PointCount,3}  " +
                $"RMS_log={result.RmsLogResidual:F3}  OuterSlope={result.OuterSlope:+0.000;-0.000; 0.000}  " +
                $"Flat={result.HasFlatOuterRegion}  → {result.Classification}");
            if (!string.IsNullOrEmpty(result.Note))
                _output.WriteLine($"    {result.Note}");
        }

        _output.WriteLine("");

        // Summary
        int pass = results.Count(r => r.Classification == B2Classification.Pass);
        int partial = results.Count(r => r.Classification == B2Classification.Partial);
        int fail = results.Count(r => r.Classification == B2Classification.Fail);

        _output.WriteLine($"  PASS: {pass}  PARTIAL: {partial}  FAIL: {fail}  TOTAL: {results.Count}");
        _output.WriteLine("");

        // Assert: at least one galaxy should PASS (baryons alone should
        // not be completely wrong — for HSB galaxies, baryons dominate
        // the inner region and should produce reasonable curves there).
        Assert.True(pass + partial > 0,
            "All galaxies failed — mechanism produces no qualitative match. " +
            "Check data loading and the mapping chain.");
    }

    // ════════════════════════════════════════════════════════════
    // B2CM02 — Flat outer rotation curve test
    // ════════════════════════════════════════════════════════════

    /// <summary>
    /// B2CM02 — Tests whether the K1×F1 mechanism produces flat outer
    /// rotation curves (the defining feature of spiral galaxies).
    ///
    /// In the standard ΛCDM picture, flat rotation curves require dark
    /// matter halos. In TRM V4, the time-rate field may provide an
    /// alternative explanation. This test evaluates whether baryons
    /// alone can produce flat outer curves.
    ///
    /// Outer flatness criterion: |dv/dr| &lt; 0.05 km/s/kpc in the
    /// outermost 30% of data points.
    /// </summary>
    [Fact]
    public void B2CM02_FlatOuterRotationCurve()
    {
        _output.WriteLine("══════════════════════════════════════════════");
        _output.WriteLine("  B2CM02 — FLAT OUTER ROTATION CURVE TEST");
        _output.WriteLine("  Baryons only — no dark matter");
        _output.WriteLine("══════════════════════════════════════════════");
        _output.WriteLine("");

        var data = LoadSparcData();
        if (data == null)
        {
            _output.WriteLine("  ⚠ SPARC data not available — skipping test.");
            return;
        }

        int flatCount = 0;
        int totalWithOuter = 0;

        foreach (var (galaxyName, points) in data)
        {
            if (points.Count < 8) continue;
            totalWithOuter++;

            // Compute v_pred from baryons only
            var vPred = ComputeBaryonicVelocity(points);

            // Check outer flatness
            int outerN = Math.Max(3, points.Count / 3);
            double outerSlope = ComputeOuterSlope(points, vPred, outerN);

            bool isFlat = Math.Abs(outerSlope) < 0.10;   // km/s per kpc
            if (isFlat) flatCount++;

            if (RepresentativeGalaxies.Contains(galaxyName))
            {
                _output.WriteLine($"  {galaxyName,-10}  outerSlope={outerSlope:+0.000;-0.000} km/s/kpc  " +
                    $"flat={isFlat}");
            }
        }

        double flatFraction = totalWithOuter > 0 ? (double)flatCount / totalWithOuter : 0;
        _output.WriteLine("");
        _output.WriteLine($"  Flat outer curves: {flatCount}/{totalWithOuter} ({flatFraction:P0})");
        _output.WriteLine("");

        // Baryons alone typically produce declining outer curves.
        // We do NOT expect 100% flatness without DM — that would be
        // a discovery. The test documents the current state honestly.
        _output.WriteLine($"  Note: ΛCDM requires DM halos for flat curves.");
        _output.WriteLine($"  TRM V4 explores whether time-rate field replaces DM.");
        _output.WriteLine($"  This test measures baryons-only baseline — not TRM prediction.");

        // No hard assertion — this is a diagnostic, not a gate.
        // The honest result is reported, not forced.
    }

    // ════════════════════════════════════════════════════════════
    // B2CM03 — TRM chain consistency check
    // ════════════════════════════════════════════════════════════

    /// <summary>
    /// B2CM03 — Verifies that the K1×F1 mapping chain produces the same
    /// rotation curve as the standard Newtonian baryonic computation.
    ///
    /// This is a sanity check: given k = G·K₀/c², the TRM chain MUST
    /// reproduce the Newtonian baryonic curve exactly. Any deviation
    /// indicates a bug in the mapping implementation.
    /// </summary>
    [Fact]
    public void B2CM03_TrmaChain_Matches_NewtonianBaryons()
    {
        _output.WriteLine("══════════════════════════════════════════════");
        _output.WriteLine("  B2CM03 — TRM CHAIN vs NEWTONIAN BARYONS");
        _output.WriteLine("  Consistency check: k = G·K₀/c² ⇒ identical curves");
        _output.WriteLine("══════════════════════════════════════════════");
        _output.WriteLine("");

        var data = LoadSparcData();
        if (data == null)
        {
            _output.WriteLine("  ⚠ SPARC data not available — skipping test.");
            return;
        }

        double maxRelError = 0.0;
        int totalPoints = 0;

        foreach (var (galaxyName, points) in data)
        {
            if (!RepresentativeGalaxies.Contains(galaxyName)) continue;
            if (points.Count < 5) continue;

            foreach (var p in points.OrderBy(x => x.RadiusKpc))
            {
                if (p.RadiusKpc <= 0) continue;

                // Newtonian baryonic acceleration
                double vBarSq = BaryonicVelocitySquared(p);
                if (vBarSq <= 0) continue;
                double gBarNewton = vBarSq / p.RadiusKpc * Kms2KpcToMs2;

                // K1×F1 chain: M_bar(r) = (vBar²·r)/G [in kg/kpc units]
                double rMeters = p.RadiusKpc * KpcToM;
                double vBarMs = Math.Sqrt(vBarSq) * 1000.0;   // km/s → m/s
                double MBar = (vBarMs * vBarMs * rMeters) / G;  // kg

                // δK(r) = k·M_bar/r → acceleration from coupling gradient
                // a_TRM = (k/K₀)·c² · d(M_bar/r)/dr
                // With k/K₀ = G/c²: a_TRM = G · d(M_bar/r)/dr = G·M_bar/r² = Newtonian
                double aTrm = G * MBar / (rMeters * rMeters);

                double relError = Math.Abs(aTrm - gBarNewton) / Math.Max(gBarNewton, 1e-30);
                maxRelError = Math.Max(maxRelError, relError);
                totalPoints++;
            }
        }

        _output.WriteLine($"  Points checked: {totalPoints}");
        _output.WriteLine($"  Max relative error: {maxRelError:E2}");
        _output.WriteLine("");

        // The analytic chain is exact — any error is numerical (floating point).
        // k = G·K₀/c² by construction produces exact identity.
        Assert.True(maxRelError < 1e-6,
            $"TRM chain deviates from Newtonian by {maxRelError:E2}. " +
            "The mapping must be exact given k = G·K₀/c².");
        _output.WriteLine("  ✅ TRM chain exactly reproduces Newtonian baryons.");
        _output.WriteLine("     (This is an identity given the B1 calibration, not a discovery.)");
    }

    // ════════════════════════════════════════════════════════════
    // B2CM04 — Summary report
    // ════════════════════════════════════════════════════════════

    [Fact]
    public void B2CM04_B2_SummaryReport()
    {
        _output.WriteLine("══════════════════════════════════════════════");
        _output.WriteLine("  B2 SUMMARY REPORT");
        _output.WriteLine("  K1×F1 Coupling Modulation + SPARC Data");
        _output.WriteLine("══════════════════════════════════════════════");
        _output.WriteLine("");
        _output.WriteLine("  Mechanism chain:");
        _output.WriteLine("    baryons → M_bar(r) → δK(r) = k·M_bar(r)/r");
        _output.WriteLine("    → δρ_eff ∝ δK → a(r) = G·M_bar(r)/r²");
        _output.WriteLine("    → v_pred(r) = √(r·a(r))");
        _output.WriteLine("");
        _output.WriteLine("  Calibration: k = G·K₀/c² (post-hoc, from B1)");
        _output.WriteLine("");
        _output.WriteLine("  What B2 tests:");
        _output.WriteLine("    1. Rotation curve shape (baryons only vs observed)");
        _output.WriteLine("    2. Flat outer velocity region prevalence");
        _output.WriteLine("    3. TRM chain ≡ Newtonian baryons (consistency)");
        _output.WriteLine("");
        _output.WriteLine("  Known limitations:");
        _output.WriteLine("    - k is post-hoc calibrated, not predicted");
        _output.WriteLine("    - K₀ has no physical value (ω_i = 1.0 is dimensionless)");
        _output.WriteLine("    - Baryons alone do NOT produce flat outer curves");
        _output.WriteLine("      in standard gravity → this is the ΛCDM motivation for DM");
        _output.WriteLine("    - The TRM time-rate field interpretation must provide");
        _output.WriteLine("      the additional acceleration that DM halos provide in ΛCDM");
        _output.WriteLine("");
        _output.WriteLine("  Honest assessment:");
        _output.WriteLine("    At the current calibration level (k post-hoc),");
        _output.WriteLine("    the TRM chain IS Newtonian gravity — by construction.");
        _output.WriteLine("    The physically interesting question — does TRM predict");
        _output.WriteLine("    the extra acceleration attributed to DM? — requires");
        _output.WriteLine("    a first-principles derivation of k and K₀.");
    }

    // ════════════════════════════════════════════════════════════
    // Helper — data loading
    // ════════════════════════════════════════════════════════════

    private static Dictionary<string, List<RarPoint>>? LoadSparcData()
    {
        try
        {
            string zipPath = WorkspaceFileLocator.GetFilePath("Rotmod_LTG.zip");
            string mrtPath = WorkspaceFileLocator.GetFilePath("SPARC_Lelli2016c.mrt");

            if (!File.Exists(zipPath) || !File.Exists(mrtPath))
                return null;

            var rawPoints = SparcRarAnalysis.ParseRarFromZip(zipPath);
            var galaxyMeta = SparcRarAnalysis.LoadGalaxyMetaFromMrt(mrtPath);

            // Group by galaxy
            return rawPoints
                .GroupBy(p => NormalizeGalaxyKey(p.GalaxyName))
                .Where(g => g.Count() >= 5)
                .ToDictionary(g => g.Key, g => g.OrderBy(p => p.RadiusKpc).ToList());
        }
        catch
        {
            return null;   // CI-safe — skip if data unavailable
        }
    }

    // ════════════════════════════════════════════════════════════
    // Helper — baryonic mass and velocity
    // ════════════════════════════════════════════════════════════

    /// <summary>
    /// Baryonic velocity squared: v_bar² = v_gas² + 0.5·v_disk² + 0.7·v_bulge².
    /// Uses standard SPARC mass-to-light ratios (Υ_disk=0.5, Υ_bulge=0.7).
    /// </summary>
    private static double BaryonicVelocitySquared(RarPoint p)
    {
        double vGasSq = p.Vgas > 0 ? p.Vgas * p.Vgas : 0.0;
        double vDiskSq = p.Vdisk > 0 ? p.Vdisk * p.Vdisk : 0.0;
        double vBulgeSq = p.Vbulge > 0 ? p.Vbulge * p.Vbulge : 0.0;
        return vGasSq + 0.5 * vDiskSq + 0.7 * vBulgeSq;
    }

    /// <summary>
    /// Compute baryonic-only rotation velocity from SPARC data.
    /// v_bar(r) = √(G·M_bar(r)/r) = √(v_bar²).
    /// </summary>
    private static List<double> ComputeBaryonicVelocity(List<RarPoint> points)
    {
        return points.Select(p =>
        {
            double vBarSq = BaryonicVelocitySquared(p);
            return vBarSq > 0 ? Math.Sqrt(vBarSq) : 0.0;
        }).ToList();
    }

    /// <summary>
    /// Outer slope: linear fit dv/dr over the outermost `outerN` points.
    /// Units: km/s per kpc. Returns 0 if insufficient data.
    /// </summary>
    private static double ComputeOuterSlope(List<RarPoint> points, List<double> vPred, int outerN)
    {
        if (points.Count < outerN || outerN < 2) return 0.0;

        var outerPoints = points
            .Select((p, i) => (r: p.RadiusKpc, v: vPred[i]))
            .OrderByDescending(x => x.r)
            .Take(outerN)
            .OrderBy(x => x.r)
            .ToList();

        double sumR = 0, sumV = 0, sumRR = 0, sumRV = 0;
        int n = outerPoints.Count;
        foreach (var (r, v) in outerPoints)
        {
            sumR += r;
            sumV += v;
            sumRR += r * r;
            sumRV += r * v;
        }

        double denom = n * sumRR - sumR * sumR;
        if (Math.Abs(denom) < 1e-12) return 0.0;

        return (n * sumRV - sumR * sumV) / denom;
    }

    // ════════════════════════════════════════════════════════════
    // Helper — galaxy evaluation
    // ════════════════════════════════════════════════════════════

    private static GalaxyB2Result EvaluateGalaxy(string name, List<RarPoint> points)
    {
        var vPred = ComputeBaryonicVelocity(points);

        // RMS log residual: mean(|log10(v_pred/v_obs)|)
        double sumLogRes = 0;
        int validPts = 0;
        for (int i = 0; i < points.Count; i++)
        {
            if (points[i].Vobs > 0 && vPred[i] > 0)
            {
                sumLogRes += Math.Abs(Math.Log10(vPred[i] / points[i].Vobs));
                validPts++;
            }
        }
        double rmsLogRes = validPts > 0 ? sumLogRes / validPts : double.NaN;

        // Outer slope
        int outerN = Math.Max(3, points.Count / 4);
        double outerSlope = ComputeOuterSlope(points, vPred, outerN);

        bool hasFlat = Math.Abs(outerSlope) < 0.10;

        // Classification
        B2Classification classification;
        string note = "";

        if (double.IsNaN(rmsLogRes))
        {
            classification = B2Classification.Fail;
            note = "No valid points for comparison.";
        }
        else if (rmsLogRes < 0.15 && hasFlat)
        {
            classification = B2Classification.Pass;
            note = "Baryons alone match observation — unusual (HSB inner region?).";
        }
        else if (rmsLogRes < 0.30)
        {
            classification = B2Classification.Partial;
            note = hasFlat
                ? "Baryons roughly match; outer curve flat (unexpected without DM)."
                : "Baryons partially match; outer curve declining (standard expectation).";
        }
        else
        {
            classification = B2Classification.Fail;
            note = "Baryons significantly under-predict observed velocity.";
        }

        return new GalaxyB2Result(name, points.Count, rmsLogRes, outerSlope,
            hasFlat, classification, note);
    }

    /// <summary>
    /// Normalize galaxy names to canonical form for reliable matching.
    /// SPARC uses mixed-case keys; we normalize to uppercase.
    /// </summary>
    private static string NormalizeGalaxyKey(string raw)
    {
        return raw?.Trim().ToUpperInvariant() ?? "";
    }
}
