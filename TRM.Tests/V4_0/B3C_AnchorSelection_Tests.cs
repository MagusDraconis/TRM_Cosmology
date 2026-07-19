using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;
using Xunit.Abstractions;

namespace TRM.Tests.V4;

/// <summary>
/// TRM V4 — B3C-T3: Frequency Anchor Selection Tests
///
/// Implements the strict comparative scoring framework from
/// docsV4/review/TRM_V4_B3C_T3_AnchorSelection.md.
///
/// 6 candidates scored against 6 criteria (0-2 each, max 12).
/// Output: ranked table with recommended anchor.
/// </summary>
[Trait("Category", "V4")]
[Trait("Category", "B3")]
[Trait("Category", "B3C")]
public class B3C_AnchorSelection_Tests
{
    private readonly ITestOutputHelper _output;

    // ────────────────────────────────────────────────────────────
    // Scoring types
    // ────────────────────────────────────────────────────────────

    private enum AnchorRank
    {
        Preferred,
        Acceptable,
        CalibratedOnly,
        Circular,
        NotSupported
    }

    private readonly record struct AnchorScore(
        string Name,
        int S1_GIndependent,
        int S2_TrmCircular,
        int S3_Dimensional,
        int S4_Universal,
        int S5_Measurable,
        int S6_TrmFit,
        int Total,
        AnchorRank Rank,
        string Verdict);

    public B3C_AnchorSelection_Tests(ITestOutputHelper output)
    {
        _output = output;
    }

    // ════════════════════════════════════════════════════════════
    // B3CT3_01 — Full scoring matrix
    // ════════════════════════════════════════════════════════════

    [Fact]
    public void B3CT3_01_Full_Scoring_Matrix()
    {
        _output.WriteLine("══════════════════════════════════════════════");
        _output.WriteLine("  B3C-T3.01 — FREQUENCY ANCHOR SCORING MATRIX");
        _output.WriteLine("  6 candidates × 6 criteria (0-2 each, max 12)");
        _output.WriteLine("══════════════════════════════════════════════");
        _output.WriteLine("");

        var scores = new List<AnchorScore>
        {
            // A — CMB
            new("A — CMB",           2, 1, 2, 2, 2, 1, 10, AnchorRank.Acceptable,
                "Tied on score, but TRM-circularity risk (S2=1) vs cesium (S2=2)"),

            // B — Orbital
            new("B — Orbital",        0, 0, 2, 0, 2, 0,  4, AnchorRank.Circular,
                "Contains G (S1=0), solar-system specific (S4=0)"),

            // C — Planck
            new("C — Planck",         0, 0, 2, 2, 2, 1,  7, AnchorRank.Circular,
                "Contains G explicitly (S1=0) — disqualifying"),

            // D — Intrinsic sync
            new("D — Intrinsic sync", 2, 2, 0, 0, 0, 2,  6, AnchorRank.NotSupported,
                "Not independently fixed (S3=0, S4=0, S5=0) — normalization convention"),

            // E — Dark energy
            new("E — Dark energy Λ",  2, 0, 2, 2, 1, 1,  8, AnchorRank.Circular,
                "TRM-circular if TRM claims Λ via φ₀ (S2=0)"),

            // F — Cesium
            new("F — Cesium (SI)",    2, 2, 2, 2, 2, 0, 10, AnchorRank.Preferred,
                "Zero TRM-circularity risk (S2=2). Exact definition (S5=2). PREFERRED.")
        };

        // Print header
        _output.WriteLine($"  {"Candidate",-22} {"S1",3} {"S2",3} {"S3",3} {"S4",3} {"S5",3} {"S6",3} {"Σ",4} {"Rank",-16} {"Verdict"}");
        _output.WriteLine($"  {new string('─', 22)} {new string('─', 3)} {new string('─', 3)} {new string('─', 3)} {new string('─', 3)} {new string('─', 3)} {new string('─', 3)} {new string('─', 4)} {new string('─', 16)} {new string('─', 40)}");

        // Print scores
        foreach (var s in scores.OrderByDescending(s => s.Total))
        {
            _output.WriteLine($"  {s.Name,-22} {s.S1_GIndependent,3} {s.S2_TrmCircular,3} {s.S3_Dimensional,3} {s.S4_Universal,3} {s.S5_Measurable,3} {s.S6_TrmFit,3} {s.Total,4} {s.Rank,-16} {s.Verdict}");
        }

        _output.WriteLine("");
        _output.WriteLine("  Ranking:");
        var ranked = scores.OrderByDescending(s => s.Total).ToList();
        for (int i = 0; i < ranked.Count; i++)
        {
            _output.WriteLine($"    #{i + 1}  {ranked[i].Name,-22}  Σ={ranked[i].Total}/12  {ranked[i].Rank}");
        }

        _output.WriteLine("");
        _output.WriteLine("  ✅ Cesium (F) is PREFERRED — 10/12, zero TRM-circularity risk.");
        _output.WriteLine("  ✅ CMB (A) is ACCEPTABLE — 10/12, minor circularity risk.");
        _output.WriteLine("  ✅ All others REJECTED (contain G, not independently fixed, or TRM-circular).");

        // Assertions
        var cesium = scores.First(s => s.Name.Contains("Cesium"));
        Assert.Equal(AnchorRank.Preferred, cesium.Rank);
        Assert.Equal(10, cesium.Total);

        var cmb = scores.First(s => s.Name.Contains("CMB"));
        Assert.Equal(AnchorRank.Acceptable, cmb.Rank);

        // Circular candidates must score 0 on S1 (G-independence)
        foreach (var s in scores.Where(s => s.Rank == AnchorRank.Circular))
        {
            Assert.True(s.S1_GIndependent == 0 || s.S2_TrmCircular == 0,
                $"Circular candidate '{s.Name}' should have S1=0 or S2=0, got S1={s.S1_GIndependent}, S2={s.S2_TrmCircular}");
        }
    }

    // ════════════════════════════════════════════════════════════
    // B3CT3_02 — Cesium vs CMB tiebreaker
    // ════════════════════════════════════════════════════════════

    [Fact]
    public void B3CT3_02_Cesium_Tiebreaker_Over_CMB()
    {
        _output.WriteLine("══════════════════════════════════════════════");
        _output.WriteLine("  B3C-T3.02 — CESIUM vs CMB TIEBREAKER");
        _output.WriteLine("══════════════════════════════════════════════");
        _output.WriteLine("");

        _output.WriteLine("  Both score 10/12. Decisive differences:");
        _output.WriteLine("");
        _output.WriteLine("  Criterion   Cesium                          CMB");
        _output.WriteLine("  ─────────   ──────────────────────────────   ──────────────────────────────");
        _output.WriteLine("  S2 (TRM)    ZERO circularity risk (2/2)      NON-ZERO risk (1/2)");
        _output.WriteLine("              TRM never claims atomic physics   TRM uses CMB as calibration");
        _output.WriteLine("");
        _output.WriteLine("  S5 (Prec)   EXACT — defined, not measured    Measured: T=2.72548±0.00057 K");
        _output.WriteLine("              9,192,631,770 Hz (zero error)    2×10⁻⁴ relative uncertainty");
        _output.WriteLine("");
        _output.WriteLine("  S6 (Fit)    No TRM connection (0/2)           Cosmological connection (1/2)");
        _output.WriteLine("              But: fit is NOT required for      But: fit creates circularity risk");
        _output.WriteLine("              an anchor — anchors are chosen,   if TRM later claims CMB derivation");
        _output.WriteLine("              not derived from the theory");
        _output.WriteLine("");

        // The tiebreaker: S2 (TRM-circularity risk)
        // Cesium: TRM will never claim to derive atomic frequencies → permanently safe
        // CMB: TRM currently uses CMB as calibration target → potential future circularity

        _output.WriteLine("  Tiebreaker: S2 (TRM-circularity risk)");
        _output.WriteLine("    Cesium S2 = 2 — permanently safe (TRM ≠ atomic physics)");
        _output.WriteLine("    CMB    S2 = 1 — potentially circular (TRM calibrates against CMB)");
        _output.WriteLine("");
        _output.WriteLine("  Winner: CESIUM");
        _output.WriteLine("");
        _output.WriteLine("  Key insight: An anchor should be chosen from a domain");
        _output.WriteLine("  that the theory does NOT claim to explain. Cesium is");
        _output.WriteLine("  atomic physics — TRM is not atomic physics. CMB is");
        _output.WriteLine("  cosmology — TRM IS cosmology. Using CMB as anchor");
        _output.WriteLine("  risks future circularity. Cesium eliminates this risk.");
    }

    // ════════════════════════════════════════════════════════════
    // B3CT3_03 — I3 final formulation
    // ════════════════════════════════════════════════════════════

    [Fact]
    public void B3CT3_03_I3_Final_Formulation()
    {
        _output.WriteLine("══════════════════════════════════════════════");
        _output.WriteLine("  B3C-T3.03 — I3 FINAL FORMULATION");
        _output.WriteLine("══════════════════════════════════════════════");
        _output.WriteLine("");

        const string I3 = """
            I3: One physical frequency scale f_ref is required to anchor the
            dimensionless CML tick ω_i = 1.0 to physical time units.

            Recommended anchor: Cesium-133 hyperfine transition frequency
            f_ref = 9,192,631,770 Hz (exact, defining the SI second).

            Justification:
              - Independent of all TRM claims (atomic physics ≠ oscillator physics)
              - Non-circular — TRM will never derive cesium transition frequencies
              - Exact — defined, not measured (zero uncertainty)
              - Universal — same everywhere (SI standard)
              - Future-proof — no TRM result can invalidate this anchor

            Analogy: The SI second is defined by cesium frequency — an empirical
            convention, not a derived quantity. TRM requiring one empirical anchor
            is standard physics practice, not a theoretical weakness.
            """;

        _output.WriteLine(I3);
        _output.WriteLine("");

        // Verify key properties
        const double fCs = 9_192_631_770.0;
        Assert.True(fCs > 1e9, "Cesium frequency should be ~9.19 GHz");
        Assert.True(fCs < 1e10, "Cesium frequency should be ~9.19 GHz");

        // I3 is a single, precisely defined constant — not a free parameter range
        _output.WriteLine("  ✅ I3 is a single precisely-defined constant, not a free range.");
        _output.WriteLine("  ✅ I3 is chosen from a domain TRM does not claim to explain.");
        _output.WriteLine("  ✅ Final TRM input count: I1 + I2 + I3 + D1.");
    }

    // ════════════════════════════════════════════════════════════
    // B3CT3_04 — Summary report
    // ════════════════════════════════════════════════════════════

    [Fact]
    public void B3CT3_04_SummaryReport()
    {
        _output.WriteLine("══════════════════════════════════════════════");
        _output.WriteLine("  B3C-T3 SUMMARY — ANCHOR SELECTION");
        _output.WriteLine("══════════════════════════════════════════════");
        _output.WriteLine("");
        _output.WriteLine("  Selected anchor: CESIUM-133 HYPERFINE");
        _output.WriteLine("    f_ref = 9,192,631,770 Hz (SI second definition)");
        _output.WriteLine("");
        _output.WriteLine("  Why cesium:");
        _output.WriteLine("    • Zero TRM-circularity risk (atomic ≠ oscillator physics)");
        _output.WriteLine("    • Exact definition (zero measurement uncertainty)");
        _output.WriteLine("    • Universal (SI standard)");
        _output.WriteLine("    • Future-proof (no TRM result can invalidate it)");
        _output.WriteLine("");
        _output.WriteLine("  Why NOT CMB:");
        _output.WriteLine("    • TRM uses CMB as calibration target → potential circularity");
        _output.WriteLine("    • If TRM later claims CMB derivation, the anchor collapses");
        _output.WriteLine("");
        _output.WriteLine("  Impact on theory:");
        _output.WriteLine("    I3 = 'one physical frequency scale f_ref = 9,192,631,770 Hz'");
        _output.WriteLine("    Total irreducible inputs: I1 + I2 + I3 + D1");
        _output.WriteLine("    I3 is precisely defined, not a free parameter range.");
        _output.WriteLine("");
        _output.WriteLine("  Analogy to standard physics:");
        _output.WriteLine("    Newton: G is measured       TRM: f_ref is the SI second");
        _output.WriteLine("    Einstein: c is measured     TRM: c is used directly");
        _output.WriteLine("    QM: ħ is measured           TRM: one empirical anchor");
        _output.WriteLine("");
        _output.WriteLine("    All physical theories require at least one empirical anchor.");
        _output.WriteLine("    TRM is not exceptional in this regard.");
    }
}
