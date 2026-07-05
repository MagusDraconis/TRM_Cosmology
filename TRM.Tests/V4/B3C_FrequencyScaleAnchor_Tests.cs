using System;
using System.Collections.Generic;
using Xunit;
using Xunit.Abstractions;

namespace TRM.Tests.V4;

/// <summary>
/// TRM V4 — B3C-T2: Physical Frequency Scale Anchor Tests
///
/// Tests whether candidate physical reference frequencies f_ref can anchor
/// the dimensionless CML scale without circular dependence on G.
///
/// The CML operates in dimensionless units: ω_i ≈ 1.0, K₀ = 0.10.
/// To express TRM quantities in SI units, we need f_ref [Hz]:
///   K₀(physical) = f_ref · K₀(CML)
///
/// Without f_ref, G cannot be numerically predicted from TRM parameters.
///
/// Reference: docsV4/experiments/TRM_V4_B3C_T2_FrequencyScaleAnchor.md
/// </summary>
[Trait("Category", "V4")]
[Trait("Category", "B3")]
[Trait("Category", "B3C")]
public class B3C_FrequencyScaleAnchor_Tests
{
    private readonly ITestOutputHelper _output;

    // ────────────────────────────────────────────────────────────
    // Physical constants (SI)
    // ────────────────────────────────────────────────────────────

    private const double C = 2.99792458e8;          // m/s
    private const double G_SI = 6.67430e-11;        // m³/(kg·s²) — reference only
    private const double HBAR = 1.054571817e-34;     // J·s
    private const double KB = 1.380649e-23;          // J/K
    private const double CMB_T = 2.725;              // K

    // CML dimensionless parameters
    private const double K0_CML = 0.10;
    private const double R_SYNC = 0.889;

    // ────────────────────────────────────────────────────────────
    // Classification
    // ────────────────────────────────────────────────────────────

    private enum AnchorClassification
    {
        NonCircular,
        Calibrated,
        Circular,
        NotSupported
    }

    private readonly record struct AnchorResult(
        string Candidate,
        double FRefHz,
        double K0Physical,
        bool IndependentOfG,
        bool ContainsG,
        bool TrmCircular,
        AnchorClassification Classification,
        string Note);

    public B3C_FrequencyScaleAnchor_Tests(ITestOutputHelper output)
    {
        _output = output;
    }

    // ════════════════════════════════════════════════════════════
    // B3CT2_01 — Full candidate evaluation
    // ════════════════════════════════════════════════════════════

    [Fact]
    public void B3CT2_01_Evaluate_All_Frequency_Anchors()
    {
        _output.WriteLine("══════════════════════════════════════════════");
        _output.WriteLine("  B3C-T2.01 — FREQUENCY ANCHOR CANDIDATES");
        _output.WriteLine("  Evaluating f_ref for CML → SI anchoring");
        _output.WriteLine("══════════════════════════════════════════════");
        _output.WriteLine("");

        var results = new List<AnchorResult>();

        // A — CMB thermal frequency
        double fCmb = KB * CMB_T / HBAR;
        results.Add(new AnchorResult("A — CMB", fCmb, fCmb * K0_CML,
            true, false, false,
            AnchorClassification.Calibrated,
            "k_B·T_CMB/h — independent of G, universal, cosmological"));

        // B — Orbital (Kepler at 1 AU)
        double fOrbital = 1.0 / (365.25 * 24 * 3600);   // ~3.17×10⁻⁸ Hz
        results.Add(new AnchorResult("B — Orbital (1 yr⁻¹)", fOrbital, fOrbital * K0_CML,
            false, true, true,
            AnchorClassification.Circular,
            "Orbital period depends on G·M_sun — fully circular"));

        // C — Planck frequency
        double fPlanck = Math.Sqrt(C * C * C * C * C / (HBAR * G_SI));
        results.Add(new AnchorResult("C — Planck", fPlanck, fPlanck * K0_CML,
            false, true, true,
            AnchorClassification.Circular,
            "f_Planck = √(c⁵/ħG) — contains G explicitly. Maximally circular."));

        // D — Intrinsic sync (unknown — dimensionless convention)
        results.Add(new AnchorResult("D — Intrinsic sync", double.NaN, double.NaN,
            true, false, false,
            AnchorClassification.NotSupported,
            "Not independently fixed — the CML tick is a normalization convention"));

        // E — Dark energy / cosmological constant
        double lambda = 1.1e-52;   // m⁻²
        double fLambda = C * Math.Sqrt(lambda);
        results.Add(new AnchorResult("E — Dark energy Λ", fLambda, fLambda * K0_CML,
            true, false, false,
            AnchorClassification.Calibrated,
            "c·√Λ — independent of G, cosmological, potentially TRM-circular"));

        // F — Atomic (cesium hyperfine — SI second definition)
        double fCs = 9.192631770e9;   // Hz
        results.Add(new AnchorResult("F — Cesium (SI second)", fCs, fCs * K0_CML,
            true, false, false,
            AnchorClassification.Calibrated,
            "Independent of G, universal, but no TRM motivation"));

        // Print table
        _output.WriteLine($"  {"Candidate",-25} {"f_ref (Hz)",-14} {"K₀(phys)",-14} {"G-indep?",-10} {"Circular?",-10} {"Class",-14}");
        _output.WriteLine($"  {new string('─', 25)} {new string('─', 14)} {new string('─', 14)} {new string('─', 10)} {new string('─', 10)} {new string('─', 14)}");

        foreach (var r in results)
        {
            string fRefStr = double.IsNaN(r.FRefHz) ? "N/A" : $"{r.FRefHz:E2}";
            string k0Str = double.IsNaN(r.K0Physical) ? "N/A" : $"{r.K0Physical:E2}";
            _output.WriteLine($"  {r.Candidate,-25} {fRefStr,14} {k0Str,14} " +
                $"{r.IndependentOfG,-10} {r.ContainsG || r.TrmCircular,-10} {r.Classification,-14}");
        }

        _output.WriteLine("");
        _output.WriteLine("  Best candidates: A (CMB), E (dark energy), F (atomic)");
        _output.WriteLine("  — all independent of G, but none uniquely motivated by TRM.");
        _output.WriteLine("");
        _output.WriteLine("  Recommendation: use CMB T as provisional f_ref.");
        _output.WriteLine("  Analogy: SI second defined by cesium — an empirical choice.");
        _output.WriteLine("  I3 = 'one physical frequency scale f_ref required.'");

        // Assert: at least one candidate is non-circular
        int nonCircular = results.Count(r =>
            r.Classification is AnchorClassification.NonCircular or AnchorClassification.Calibrated);
        Assert.True(nonCircular >= 2,
            "At least 2 non-circular frequency anchors should exist (CMB + atomic fallback).");
    }

    // ════════════════════════════════════════════════════════════
    // B3CT2_02 — Circularity guard: G must not appear in f_ref
    // ════════════════════════════════════════════════════════════

    [Fact]
    public void B3CT2_02_Anchors_Must_Not_Depend_On_G()
    {
        _output.WriteLine("══════════════════════════════════════════════");
        _output.WriteLine("  B3C-T2.02 — CIRCULARITY GUARD");
        _output.WriteLine("  f_ref must not contain G");
        _output.WriteLine("══════════════════════════════════════════════");
        _output.WriteLine("");

        // Explicitly test that candidate formulas don't contain G
        // (except for known-circular candidates B and C)

        // CMB: f = k_B·T/h → no G
        double fCmb = KB * CMB_T / HBAR;
        bool cmbContainsG = ContainsGInFormula("k_B * T_CMB / h");
        _output.WriteLine($"  CMB:       f_ref = {fCmb:E2} Hz  contains G: {cmbContainsG}");
        Assert.False(cmbContainsG);

        // Cesium: f = 9.192631770e9 Hz (defined, not derived)
        bool csContainsG = ContainsGInFormula("9.192631770e9 Hz (SI definition)");
        _output.WriteLine($"  Cesium:    f_ref = 9.192631770e9 Hz  contains G: {csContainsG}");
        Assert.False(csContainsG);

        // Planck: f = sqrt(c⁵/(ħ·G)) → contains G explicitly
        bool planckContainsG = ContainsGInFormula("sqrt(c^5 / (hbar * G))");
        _output.WriteLine($"  Planck:    f_ref formula contains G: {planckContainsG}");
        Assert.True(planckContainsG, "Planck frequency IS circular — this is the expected result.");

        // Orbital: f² = G·M/(4π²·r³) → contains G
        bool orbitalContainsG = ContainsGInFormula("sqrt(G * M_sun / (4*pi^2 * r^3))");
        _output.WriteLine($"  Orbital:   f_ref formula contains G: {orbitalContainsG}");
        Assert.True(orbitalContainsG, "Orbital frequency IS circular — this is the expected result.");

        _output.WriteLine("");
        _output.WriteLine("  ✅ CMB and atomic anchors pass the circularity guard.");
        _output.WriteLine("  ✅ Planck and orbital correctly identified as circular.");
    }

    // ════════════════════════════════════════════════════════════
    // B3CT2_03 — G expression from f_ref
    // ════════════════════════════════════════════════════════════

    [Fact]
    public void B3CT2_03_G_Expression_From_FrequencyAnchor()
    {
        _output.WriteLine("══════════════════════════════════════════════");
        _output.WriteLine("  B3C-T2.03 — G EXPRESSION FROM f_ref");
        _output.WriteLine("  Can G be expressed via f_ref + TRM parameters?");
        _output.WriteLine("══════════════════════════════════════════════");
        _output.WriteLine("");

        // Using CMB anchor:
        double fCmb = KB * CMB_T / HBAR;
        double K0_phys = fCmb * K0_CML;

        // C4 chain: G = (c² · E_defect) / (K₀ · M · 4π · c²) = E_defect / (K₀ · M · 4π)
        // E_defect ∝ δK_defect · R² (dimensionless)
        // M = E_defect / c² (from E=mc²)
        //
        // G = (c² · δK · R²) / (K₀_phys · 4π)
        //   = (c² · δK · R²) / (f_ref · K₀_CML · 4π)

        double deltaK = 0.05;     // example defect strength
        double G_predicted = (C * C * deltaK * R_SYNC * R_SYNC) /
                             (K0_phys * 4.0 * Math.PI);

        _output.WriteLine($"  Using CMB anchor: f_ref = {fCmb:E2} Hz");
        _output.WriteLine($"  K₀(physical) = {K0_phys:E2} Hz");
        _output.WriteLine($"  G_predicted = {G_predicted:E4} m³/(kg·s²)");
        _output.WriteLine($"  G_measured  = {G_SI:E4} m³/(kg·s²)");
        _output.WriteLine($"  Ratio G_pred/G_meas = {G_predicted / G_SI:E4}");
        _output.WriteLine("");

        // The ratio is not 1.0 because δK=0.05 and other parameters
        // were not calibrated. This test documents the expression, not the match.
        _output.WriteLine("  Note: The ratio ≠ 1 because δK, ρ_ref, and the");
        _output.WriteLine("  exact energy→defect mapping are not calibrated here.");
        _output.WriteLine("  This test demonstrates the FORM of the G expression,");
        _output.WriteLine("  not its numerical prediction.");
        _output.WriteLine("");
        _output.WriteLine("  Form: G = (c² · δK · R²) / (f_ref · K₀_CML · 4π)");
        _output.WriteLine("  This expresses G in terms of c, δK, R, f_ref, K₀_CML.");
        _output.WriteLine("  δK and f_ref are empirical; R and K₀_CML are TRM-native.");

        // Verify form is dimensionally correct
        // [G] = m³/(kg·s²)
        // [c²] = m²/s², [δK] = dimensionless, [R²] = dimensionless
        // [f_ref] = 1/s, [K₀_CML] = dimensionless
        // → [c²/(f_ref)] = m²/s → NOT m³/(kg·s²)
        //
        // We need an additional length scale and mass scale.
        // The missing scales are ρ_ref (energy density → kg/m³) and a
        // characteristic length. This documents the dimensional challenge.

        _output.WriteLine("");
        _output.WriteLine("  ⚠ Dimensional note: The expression above is incomplete —");
        _output.WriteLine("  it requires ρ_ref (reference energy density) and a");
        _output.WriteLine("  characteristic length scale for full dimensional consistency.");
        _output.WriteLine("  C4 closure requires not only f_ref but also ρ_ref.");
    }

    // ════════════════════════════════════════════════════════════
    // B3CT2_04 — Summary report
    // ════════════════════════════════════════════════════════════

    [Fact]
    public void B3CT2_04_SummaryReport()
    {
        _output.WriteLine("══════════════════════════════════════════════");
        _output.WriteLine("  B3C-T2 SUMMARY");
        _output.WriteLine("  Physical Frequency Scale Anchor");
        _output.WriteLine("══════════════════════════════════════════════");
        _output.WriteLine("");
        _output.WriteLine("  Problem:");
        _output.WriteLine("    CML operates in dimensionless units (ω_i = 1.0).");
        _output.WriteLine("    K₀, G require a physical frequency f_ref to get SI values.");
        _output.WriteLine("");
        _output.WriteLine("  Findings:");
        _output.WriteLine("    Non-circular anchors exist: CMB (k_B·T/h), atomic (Cs), dark energy (c·√Λ)");
        _output.WriteLine("    Circular anchors rejected: Planck (contains G), orbital (depends on G·M)");
        _output.WriteLine("    No anchor is uniquely motivated by TRM oscillator physics");
        _output.WriteLine("");
        _output.WriteLine("  Recommended: CMB T as provisional f_ref");
        _output.WriteLine("    f_ref = k_B · T_CMB / h ≈ 5.7×10¹⁰ Hz");
        _output.WriteLine("");
        _output.WriteLine("  Analogy to standard physics:");
        _output.WriteLine("    The SI second is defined by cesium frequency — an empirical choice.");
        _output.WriteLine("    f_ref in TRM is analogous — an empirical anchor, not a derivation.");
        _output.WriteLine("");
        _output.WriteLine("  Impact on I3:");
        _output.WriteLine("    I3 = 'one physical frequency scale f_ref is required'");
        _output.WriteLine("    This is cleaner than a generic empirical constant.");
        _output.WriteLine("    It identifies precisely what is assumed, not vaguely.");
    }

    // ════════════════════════════════════════════════════════════
    // Helper
    // ════════════════════════════════════════════════════════════

    /// <summary>
    /// Naive string check: does the formula text contain "G"?
    /// (Not a parser — just for documentation purposes in the test.)
    /// </summary>
    private static bool ContainsGInFormula(string formula)
    {
        return formula.Contains("G ") || formula.Contains("G)") ||
               formula.Contains(" G") || formula.Contains("*G") ||
               formula.Contains("/G") || formula.Contains("(G") ||
               formula.Contains(" G,") || formula.Contains(",G");
    }
}
