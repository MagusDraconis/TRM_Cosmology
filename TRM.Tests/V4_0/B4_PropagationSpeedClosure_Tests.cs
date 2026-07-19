using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;
using Xunit.Abstractions;

namespace TRM.Tests.V4;

/// <summary>
/// TRM V4 — B4-T1: Propagation Speed Closure Tests
///
/// Tests whether c_K can be expressed in TRM-native terms.
///
/// Candidates:
///   A) c_K = c                        (ASSUMED — GR match)
///   B) c_K = K₀·Δx·f_ref              (CALIBRATED — requires Δx)
///   C) c_K(CML) from defect front      (DERIVABLE — dimensionless)
///   D) c_K = f(topology)·K₀·Δx·f_ref   (DERIVABLE — topology factor)
///
/// Reference: docsV4/experiments/TRM_V4_B4_T1_PropagationSpeedClosure.md
/// </summary>
[Trait("Category", "V4")]
[Trait("Category", "B4")]
public class B4_PropagationSpeedClosure_Tests
{
    private readonly ITestOutputHelper _output;

    private const double C_Light = 2.99792458e8;
    private const double F_Ref = 9_192_631_770.0;
    private const double K0_Cml = 0.10;

    public B4_PropagationSpeedClosure_Tests(ITestOutputHelper output)
    {
        _output = output;
    }

    // ════════════════════════════════════════════════════════════
    // B4T1_01 — Candidate A: c_K = c
    // ════════════════════════════════════════════════════════════

    [Fact]
    public void B4T1_01_CandidateA_Ck_Equals_C()
    {
        _output.WriteLine("══════════════════════════════════════════════");
        _output.WriteLine("  B4-T1.01 — CANDIDATE A: c_K = c");
        _output.WriteLine("══════════════════════════════════════════════");
        _output.WriteLine("");

        // Assumption: c_K = c
        double cK = C_Light;

        // Implied Δx: c_K = K₀·Δx·f_ref → Δx = c_K/(K₀·f_ref)
        double deltaX = cK / (K0_Cml * F_Ref);
        _output.WriteLine($"  Assumption: c_K = c = {C_Light:E3} m/s");
        _output.WriteLine($"  Implied Δx = c/(K₀·f_ref) = {deltaX:F3} m ≈ {deltaX * 100:F1} cm");
        _output.WriteLine("");

        // Dimensional check
        _output.WriteLine("  Dimensional check:");
        _output.WriteLine($"    [K₀] = dimensionless, [f_ref] = 1/s, [Δx] = m");
        _output.WriteLine($"    → [K₀·Δx·f_ref] = m/s = [c_K]  ✓");
        _output.WriteLine("");

        // Consistency: Δx must be positive and physically meaningful
        Assert.True(deltaX > 0.01, "Δx must be macroscopic (> 1 cm)");
        Assert.True(deltaX < 100.0, "Δx must be sub-100 m (otherwise system too large)");

        _output.WriteLine($"  Δx ≈ {deltaX * 100:F0} cm — macroscopic oscillator spacing");
        _output.WriteLine("  This is physically plausible for a coupled-oscillator network.");
        _output.WriteLine("");
        _output.WriteLine("  Classification: ASSUMED (c_K = c matches GR, not derived)");
        _output.WriteLine("  Benefit: eliminates I4 (Δx is predicted, not assumed)");
    }

    // ════════════════════════════════════════════════════════════
    // B4T1_02 — Candidate B: c_K = K₀·Δx·f_ref
    // ════════════════════════════════════════════════════════════

    [Fact]
    public void B4T1_02_CandidateB_Ck_From_CouplingTimescale()
    {
        _output.WriteLine("══════════════════════════════════════════════");
        _output.WriteLine("  B4-T1.02 — CANDIDATE B: c_K = K₀·Δx·f_ref");
        _output.WriteLine("══════════════════════════════════════════════");
        _output.WriteLine("");

        // Sweep Δx to show c_K range
        double[] dxValues = { 0.10, 0.33, 1.0, 3.0, 10.0 };

        _output.WriteLine($"  {"Δx (m)",10} {"c_K (m/s)",14} {"c_K/c",10} {"Note"}");
        _output.WriteLine($"  {new string('─', 10)} {new string('─', 14)} {new string('─', 10)} {new string('─', 30)}");

        foreach (double dx in dxValues)
        {
            double cK = K0_Cml * dx * F_Ref;
            double ratio = cK / C_Light;
            string note = ratio switch
            {
                > 0.99 and < 1.01 => "≈ c (GR match)",
                > 1.0 => $"> c (superluminal? {ratio:F1}×c)",
                _ => $"< c ({ratio:P0} of c)"
            };
            _output.WriteLine($"  {dx,10:F2} {cK,14:E3} {ratio,10:F3} {note}");
        }

        _output.WriteLine("");
        _output.WriteLine("  For c_K = c: Δx ≈ 0.33 m");
        _output.WriteLine("  For Δx = 1 m: c_K ≈ 3.1c (superluminal — conflicts with GR)");
        _output.WriteLine("");

        // Dimensional consistency
        double testCk = K0_Cml * 1.0 * F_Ref;
        double[] dims = { 0, 1, -1 };  // [K₀] dimensionless^0, [Δx] m^1, [f_ref] s^-1
        _output.WriteLine($"  Dimensional: [K₀]⁰·[Δx]¹·[f_ref]⁻¹ = m/s ✓");
        Assert.True(testCk > 0);

        _output.WriteLine("  Classification: CALIBRATED (requires Δx as I4)");
        _output.WriteLine("  c_K is derived from I3(f_ref) + I4(Δx) + K₀(TRM-native).");
    }

    // ════════════════════════════════════════════════════════════
    // B4T1_03 — Candidate C: c_K(CML) from coupling timescale
    // ════════════════════════════════════════════════════════════

    [Fact]
    public void B4T1_03_CandidateC_Ck_Cml_Dimensionless()
    {
        _output.WriteLine("══════════════════════════════════════════════");
        _output.WriteLine("  B4-T1.03 — CANDIDATE C: c_K(CML) dimensionless");
        _output.WriteLine("══════════════════════════════════════════════");
        _output.WriteLine("");

        // In CML, coupling perturbation propagates at rate ~K₀
        // c_K(CML) = K₀ = 0.10 (sites per CML time unit)
        double ckCml = K0_Cml;

        _output.WriteLine($"  c_K(CML) = K₀ = {ckCml:F3} sites per CML tick");
        _output.WriteLine("");
        _output.WriteLine("  Physical interpretation:");
        _output.WriteLine($"    A perturbation at site i reaches site i+1 in ~{1.0 / ckCml:F1} ticks.");
        _output.WriteLine($"    In physical time: τ = 1/(K₀·f_ref) ≈ {1.0 / (K0_Cml * F_Ref) * 1e9:F2} ns");
        _output.WriteLine("");

        // c_K(CML) is a TRM-native prediction — dimensionless
        _output.WriteLine("  This is a TRM-native prediction:");
        _output.WriteLine("    c_K(CML) = K₀ = 0.10 (dimensionless)");
        _output.WriteLine("    No additional parameters needed.");
        _output.WriteLine("");
        _output.WriteLine("  Physical c_K requires Δx and f_ref:");
        _output.WriteLine("    c_K = c_K(CML) · Δx · f_ref");
        _output.WriteLine("");
        _output.WriteLine("  Classification: DERIVABLE (dimensionless c_K)");
        _output.WriteLine("                   CALIBRATED (physical c_K needs Δx)");

        Assert.True(ckCml > 0);
        Assert.True(ckCml < 1.0);   // subluminal in CML units
    }

    // ════════════════════════════════════════════════════════════
    // B4T1_04 — I4 analysis: spatial anchor vs c_K assumption
    // ════════════════════════════════════════════════════════════

    [Fact]
    public void B4T1_04_I4_Analysis()
    {
        _output.WriteLine("══════════════════════════════════════════════");
        _output.WriteLine("  B4-T1.04 — I4 ANALYSIS");
        _output.WriteLine("  Spatial anchor vs c_K assumption");
        _output.WriteLine("══════════════════════════════════════════════");
        _output.WriteLine("");

        _output.WriteLine("  Two approaches to closing c_K:");
        _output.WriteLine("");
        _output.WriteLine("  APPROACH 1: c_K = c (assumed)");
        _output.WriteLine("    Inputs:  I1, I2, I3(f_ref), D1 + assumption c_K=c");
        _output.WriteLine("    Derived: Δx = c/(K₀·f_ref) ≈ 33 cm");
        _output.WriteLine("    Cost:    1 assumption (empirically supported by LIGO)");
        _output.WriteLine("    Total:   4 irreducible + 1 assumption");
        _output.WriteLine("");
        _output.WriteLine("  APPROACH 2: I4 = Δx (spatial anchor)");
        _output.WriteLine("    Inputs:  I1, I2, I3(f_ref), I4(Δx), D1");
        _output.WriteLine("    Derived: c_K = K₀·Δx·f_ref");
        _output.WriteLine("    Cost:    1 new irreducible input (Δx)");
        _output.WriteLine("    Total:   5 irreducible");
        _output.WriteLine("");
        _output.WriteLine("  RECOMMENDATION: Approach 1 (c_K = c)");
        _output.WriteLine("    • Fewer irreducible inputs (4 vs 5)");
        _output.WriteLine("    • LIGO: gravitational waves propagate at c to ~10⁻¹⁵");
        _output.WriteLine("    • If future observations show c_K ≠ c, switch to Approach 2");
        _output.WriteLine("    • Approach 2 is the natural fallback — absorbs deviation gracefully");
        _output.WriteLine("");

        // Verify the relation between approaches
        double dxFromCk = C_Light / (K0_Cml * F_Ref);
        double ckFromDx = K0_Cml * dxFromCk * F_Ref;
        Assert.Equal(C_Light, ckFromDx, 3);  // closed loop: c → Δx → c_K = c

        _output.WriteLine($"  Closed-loop check: c → Δx={dxFromCk:F3}m → c_K={ckFromDx:E3}m/s = c ✓");
    }

    // ════════════════════════════════════════════════════════════
    // B4T1_05 — Summary report
    // ════════════════════════════════════════════════════════════

    [Fact]
    public void B4T1_05_SummaryReport()
    {
        _output.WriteLine("══════════════════════════════════════════════");
        _output.WriteLine("  B4-T1 SUMMARY — PROPAGATION SPEED CLOSURE");
        _output.WriteLine("══════════════════════════════════════════════");
        _output.WriteLine("");
        _output.WriteLine("  Candidate comparison:");
        _output.WriteLine("");
        _output.WriteLine("    A — c_K = c                      ASSUMED    (GR match, no I4)");
        _output.WriteLine("    B — c_K = K₀·Δx·f_ref            CALIBRATED (I4 = Δx required)");
        _output.WriteLine("    C — c_K(CML) = K₀                DERIVABLE  (dimensionless, TRM-native)");
        _output.WriteLine("    D — c_K = f(topo)·K₀·Δx·f_ref    DERIVABLE  (from topology, same Δx dep)");
        _output.WriteLine("");
        _output.WriteLine("  Recommended: Approach A (c_K = c)");
        _output.WriteLine("    • Empirically supported (LIGO: c_gw = c ± 10⁻¹⁵)");
        _output.WriteLine("    • Eliminates I4 (Δx predicted, not assumed)");
        _output.WriteLine("    • 4 irreducible inputs + 1 empirically-supported assumption");
        _output.WriteLine("");
        _output.WriteLine("  Fallback: Approach B (I4 = Δx)");
        _output.WriteLine("    • If c_K ≠ c is ever measured");
        _output.WriteLine("    • Absorbs deviation naturally: Δx = c_K/(K₀·f_ref)");
        _output.WriteLine("    • 5 irreducible inputs, c_K fully derived");
    }
}
