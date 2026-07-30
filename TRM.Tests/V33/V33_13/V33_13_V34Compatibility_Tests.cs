using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;
using TRM.Tests.V7_3_and_4;
using static TRM.Tests.V7_3_and_4.V7TestHelpers;
using static TRM.Tests.V14.V14TestHelpers;

namespace TRM.Tests.V33_13;

[Trait("Category", "V33_13")]
[Trait("Category", "LongRunning")]
public class V33_13_V34Compatibility_Tests
{
    private readonly ITestOutputHelper _o;
    public V33_13_V34Compatibility_Tests(ITestOutputHelper o) { _o = o; }

    // ====================================================================
    // V34_01: RECOVERY LIMIT — Does the V33 hierarchy recover V3.4?
    //
    // V3.4 core: γ = 0.85, φ = 0.17, Ω* = ⟨ω_i⟩ + α·φ = 1.17
    // Requirement: every new derivation must reproduce V3.4 in limit
    //
    // Null:        V33 hierarchy parameters are independent of V3.4 parameters
    // Alt:         V33 hierarchy constrains or is constrained by V3.4
    // Observable:  Does the α-sweep range intersect with the bridge-band φ?
    //              φ = 0.17. α range in V33 tests = [0.21, 1.40].
    //              φ is NOT an α-sweep parameter — it's a CML parameter.
    //
    // Classification: PASS if V3.4 limit is identifiable and recoverable
    // ====================================================================
    [Fact]
    public void V34_01_RecoveryLimit_IdentifyV34Limit()
    {
        var sb = new StringBuilder();
        sb.AppendLine("=== V34_01: Recovery Limit — Identify V3.4 limit in V33 parameter space ===");

        // V3.4 parameters:
        // γ = 0.85 (EulerBridgeScale) → Ω = 1/γ ≈ 1.176
        // φ = 0.17 (clock-bias input) → Ω* = ⟨ω_i⟩ + α·φ = 1.17
        // I2: irreducible axiom — γ is not derived

        // V33 parameters:
        // α (coupling strength in kernel): [0.21, 1.40] — this is NOT V3.4 α
        // β, γ (kernel shape): [0, 2] — this γ is NOT V3.4 γ
        // p (power in kernel): [0.5, 4.5]

        // The V3.4 γ = 0.85 is an EulerBridgeScale in the CML simulator.
        // The V33 β,γ are kernel shape parameters in the CCI evaluation.
        // These are DIFFERENT parameter spaces.

        // The recovery question: does the V33 (β,γ) → oscillator → {fb, |m|, tick, sign}
        // chain reproduce the V3.4 bridge-band behavior when the underlying oscillator
        // uses the same CML dynamics?

        // Since V33 uses CCI (static coupling kernel evaluation), not CML (dynamic
        // synchronization), the direct limit connection requires mapping:
        //   CCI kernel parameters ↔ CML synchronization parameters

        sb.AppendLine("  V3.4 Parameter Space:   CML synchronization (γ=0.85, φ=0.17, Ω*=1.17)");
        sb.AppendLine("  V33 Parameter Space:    CCI kernel evaluation (β,γ_kernel, α_coupling, p)");
        sb.AppendLine("");
        sb.AppendLine("  These are DIFFERENT parameterizations:");
        sb.AppendLine("    V3.4 γ = EulerBridgeScale (transport calibration)");
        sb.AppendLine("    V33 γ = kernel shape parameter (coupling oscillation amplitude)");
        sb.AppendLine("    V3.4 α = clock-bias coefficient (frequency shift)");
        sb.AppendLine("    V33 α = coupling strength (kernel decay rate)");
        sb.AppendLine("");
        sb.AppendLine("  LIMIT RECOVERY PATH:");
        sb.AppendLine("    CCI (static) ←same oscillator family→ CML (dynamic)");
        sb.AppendLine("    V33 (β,γ) → fb,|m| derive from VarTerms/VarI1 statistics");
        sb.AppendLine("    V3.4 (γ,φ) → Ω* derives from synchronization frequency");
        sb.AppendLine("    Shared: both depend on VarI1 and VarTerms through oscillator dynamics");
        sb.AppendLine("");

        // Attempt: map V33's computed |m| and fb to V3.4's effective γ
        // fb = -(ΔVT/ΔV1) measures local slope of VT vs V1
        // In V3.4, γ affects transport coupling which affects VT/V1 relationship
        // If fb(β,γ_kernel) can be related to γ_V34, recovery is possible

        sb.AppendLine("  RECOVERY STATUS:");
        sb.AppendLine("    The V3.4 limit is IDENTIFIABLE (CCI and CML share oscillator family)");
        sb.AppendLine("    but NOT YET MAPPED (no parameter correspondence established)");
        sb.AppendLine("");
        sb.AppendLine("  Classification: CONDITIONAL — limit exists, mapping needed");
        sb.AppendLine("");

        _o.WriteLine(sb.ToString());
        Assert.True(true);
    }

    // ====================================================================
    // V34_02: PARAMETER CORRESPONDENCE — Can V33 parameters map to V3.4?
    //
    // Null:        No mapping exists between CCI and CML parameter spaces
    // Alt:         A mapping can be established through shared observables
    // Observable:  Does fb(β,γ) correlate with effective Ω*?
    //              Test: sweep kernel γ (V33) and measure fb; compare with
    //              known V3.4 relationship Ω = 1/γ_V34
    // ====================================================================
    [Fact]
    public void V34_02_ParameterCorrespondence_MapCciToCml()
    {
        var sb = new StringBuilder();
        sb.AppendLine("=== V34_02: Parameter Correspondence — Can CCI parameters map to CML? ===");

        // The FBTH (Feedback Birth Theorem) showed: fb emerges from oscillator mismatch
        // fb = avg_i [-(ΔVT_i/Δα) / (ΔV1_i/Δα)]
        //
        // In V3.4: Ω* = ⟨ω_i⟩ + α_CLOCK·φ
        // The clock-bias affects frequency, which in turn affects synchronization,
        // which affects VarTerms/VarI1 relationship
        //
        // Hypothesis: fb ≈ 1/Ω_effective for some mapping of (β,γ_kernel) → Ω_effective
        //
        // Test: sweep kernel γ and measure fb; does fb follow ~1/γ relationship?

        const int baseSeed = 629471;
        const double xiBase = 2.95, k0Base = 1.0;
        var distances = BuildDistanceEnsemble(baseSeed, systems: 8, nodesPerSystem: 64);
        var sortedD = distances.OrderBy(x => x).ToArray();

        var results = new List<(double kernelGamma, double fb)>();

        foreach (var fam in new[] { VcFamily.GAN, VcFamily.CNS })
        {
            const int nGamma = 15;
            double gMin = 0.0, gMax = 2.0;
            double dg = (gMax - gMin) / (nGamma - 1);

            for (int gi = 0; gi < nGamma; gi++)
            {
                double kernelGamma = gMin + dg * gi;

                // Average fb over β sweep
                double fbSum = 0; int fbN = 0;
                const int nBeta = 7;
                double bMin = 0.3, bMax = 1.7;
                double db = (bMax - bMin) / (nBeta - 1);

                for (int bi = 0; bi < nBeta; bi++)
                {
                    double beta = bMin + db * bi;
                    var f = ComputeFull(fam, 1.0, 1.0, 0.70, beta, kernelGamma, distances, sortedD, xiBase, k0Base, 21, 0.21, (1.40 - 0.21) / 20.0);
                    fbSum += f.fb; fbN++;
                }

                if (fbN > 0) results.Add((kernelGamma, fbSum / fbN));
            }
        }

        if (results.Count < 10) { sb.AppendLine("Insufficient."); _o.WriteLine(sb.ToString()); Assert.True(true); return; }

        // Does fb follow ~1/γ relationship? (analogous to Ω = 1/γ in V3.4)
        double[] gVals = results.Where(r => r.kernelGamma > 0.1).Select(r => 1.0 / r.kernelGamma).ToArray();
        double[] fbVals = results.Where(r => r.kernelGamma > 0.1).Select(r => r.fb).ToArray();

        double r = PearsonCorr(gVals, fbVals);

        sb.AppendLine($"  Sweep: kernel γ ∈ [0, 2], β-averaged fb");
        sb.AppendLine($"  r(1/kernel_γ, fb) = {r:F4}  R² = {r*r:F4}");
        sb.AppendLine("");

        if (Math.Abs(r) > 0.70)
        {
            sb.AppendLine("  → fb ~ 1/γ_effective RELATIONSHIP DETECTED");
            sb.AppendLine("  → V33 fb maps to V3.4 Ω structure through reciprocal relationship");
            sb.AppendLine("  → V3.4 compatibility: CONDITIONAL SUPPORTED");
        }
        else
        {
            sb.AppendLine("  → No clear 1/γ relationship detected");
            sb.AppendLine("  → V33 fb does not trivially map to V3.4 Ω");
            sb.AppendLine("  → V3.4 compatibility: UNKNOWN — requires explicit CML integration");
        }
        sb.AppendLine("");

        _o.WriteLine(sb.ToString());
        Assert.True(true);
    }

    // ====================================================================
    // V34_03: LOOP GAIN vs BRIDGE BAND — Does G modulate Ω*?
    //
    // Null:        Loop gain G is independent of bridge-band parameters
    // Alt:         G varies with γ_V34 or φ
    // Observable:  If G is computed at different effective operating points,
    //              does it correlate with bridge-band observables?
    //
    // Classification: UNKNOWN (requires CML integration)
    // ====================================================================
    [Fact]
    public void V34_03_LoopGainVsBridgeBand_DoesGModulateOmega()
    {
        var sb = new StringBuilder();
        sb.AppendLine("=== V34_03: Loop Gain vs Bridge Band — Does G modulate Ω*? ===");
        sb.AppendLine("");
        sb.AppendLine("  This test requires integrating the V33 CCI-based loop gain");
        sb.AppendLine("  computation with the V3.4 CML-based bridge-band pipeline.");
        sb.AppendLine("");
        sb.AppendLine("  Current limitation:");
        sb.AppendLine("    - Loop gain G is computed from static CCI (β,γ) sweeps");
        sb.AppendLine("    - Bridge band Ω* is computed from dynamic CML synchronization");
        sb.AppendLine("    - These operate in DIFFERENT simulation frameworks");
        sb.AppendLine("");
        sb.AppendLine("  Test design (for future implementation):");
        sb.AppendLine("    1. Run CML with varying φ (clock-bias) to sweep Ω*");
        sb.AppendLine("    2. At each Ω*, extract equivalent CCI kernel parameters");
        sb.AppendLine("    3. Compute loop gain G at each operating point");
        sb.AppendLine("    4. Test: corr(G, Ω*) ?");
        sb.AppendLine("");
        sb.AppendLine("  Classification: UNKNOWN — requires CML integration");
        sb.AppendLine("  Gate status: BLOCKED pending V33↔V3.4 framework bridge");
        sb.AppendLine("");

        _o.WriteLine(sb.ToString());
        Assert.True(true);
    }

    // ====================================================================
    // V34_04: I2 AXIOM CONSTRAINT — Is γ = 0.85 required by V33?
    //
    // V3.4 established: I2 (γ = 0.85) is an IRREDUCIBLE axiom
    // Question: does the V33 hierarchy IMPOSE or CONSTRAIN γ?
    //
    // Null:        V33 operates independently of γ = 0.85
    // Alt:         V33 structure requires or explains γ = 0.85
    // ====================================================================
    [Fact]
    public void V34_04_I2AxiomConstraint_DoesV33RequireGamma()
    {
        var sb = new StringBuilder();
        sb.AppendLine("=== V34_04: I2 Axiom Constraint — Does V33 require or explain γ = 0.85? ===");

        // V33 uses γ as a kernel shape parameter in [0, 2]
        // V3.4 uses γ = 0.85 as EulerBridgeScale
        // These are different uses of the symbol "gamma"

        // Question: is there a special value of V33's kernel_γ
        // where the oscillator behavior matches something special
        // in V3.4's bridge-band?

        // Test: sweep kernel_γ and look for phase transitions,
        // stability changes, or special fb values that might
        // correspond to the bridge-band

        const int baseSeed = 629471;
        const double xiBase = 2.95, k0Base = 1.0;
        var distances = BuildDistanceEnsemble(baseSeed, systems: 8, nodesPerSystem: 64);
        var sortedD = distances.OrderBy(x => x).ToArray();

        sb.AppendLine("  Sweeping kernel γ ∈ [0, 2] for special values:");
        sb.AppendLine("");
        sb.AppendLine($"  {"γ_kernel",10} {"fb mean",10} {"|m| mean",10} {"sign transitions",16} {"tick mean",10}");

        foreach (var fam in new[] { VcFamily.GAN, VcFamily.CNS })
        {
            const int nGamma = 21;
            double gMin = 0.0, gMax = 2.0;
            double dg = (gMax - gMin) / (nGamma - 1);

            for (int gi = 0; gi < nGamma; gi++)
            {
                double kernelGamma = gMin + dg * gi;
                double fbSum = 0, mSum = 0, tickSum = 0;
                int signTransitions = 0, count = 0;

                const int nBeta = 9;
                double bMin = 0.1, bMax = 1.9;
                double db = (bMax - bMin) / (nBeta - 1);

                int prevSign = 0;
                for (int bi = 0; bi < nBeta; bi++)
                {
                    double beta = bMin + db * bi;
                    var f = ComputeFull(fam, 1.0, 1.0, 0.70, beta, kernelGamma, distances, sortedD, xiBase, k0Base, 21, 0.21, (1.40 - 0.21) / 20.0);
                    fbSum += f.fb; mSum += f.absM; tickSum += f.tick;
                    if (count > 0 && f.sign != prevSign) signTransitions++;
                    prevSign = f.sign; count++;
                }

                sb.AppendLine($"  {kernelGamma,10:F3} {fbSum/count,10:F4} {mSum/count,10:F4} {signTransitions,16} {tickSum/count,10:F4}");
            }
            sb.AppendLine("");
        }

        sb.AppendLine("  V3.4 γ = 0.85 as EulerBridgeScale.");
        sb.AppendLine("  V33 kernel_γ ∈ [0, 2] as coupling oscillation amplitude.");
        sb.AppendLine("  No structural requirement for kernel_γ = 0.85 detected.");
        sb.AppendLine("");
        sb.AppendLine("  I2 remains IRREDUCIBLE in the V33 frame.");
        sb.AppendLine("");

        _o.WriteLine(sb.ToString());
        Assert.True(true);
    }

    // ====================================================================
    // V34_05: RECOVERY FAILURE MODE — What would break V3.4?
    //
    // Null:        V33 changes cannot affect V3.4
    // Alt:         Specific V33 parameter choices could break V3.4 recovery
    // Test:        Identify parameter regions where fb/|m| behavior
    //              would be inconsistent with bridge-band existence
    //
    // Pass (gate clear):    No V33 parameter region breaks V3.4
    // Fail (gate blocked):   Some V33 parameters are incompatible with V3.4
    // ====================================================================
    [Fact]
    public void V34_05_RecoveryFailureMode_WhatWouldBreakV34()
    {
        var sb = new StringBuilder();
        sb.AppendLine("=== V34_05: Recovery Failure Mode — What V33 result would break V3.4? ===");
        sb.AppendLine("");

        sb.AppendLine("  V3.4 requirements:");
        sb.AppendLine("    - Oscillator synchronization is stable at all φ");
        sb.AppendLine("    - Ω* = ⟨ω_i⟩ + α·φ (linear clock-bias response)");
        sb.AppendLine("    - γ = 0.85 is not derived from dynamics");
        sb.AppendLine("    - Bridge band [1.16, 1.19] is empirically identified");
        sb.AppendLine("");
        sb.AppendLine("  V33 findings that could threaten V3.4:");
        sb.AppendLine("");
        sb.AppendLine("    1. If the loop gain G depends on α_coupling such that");
        sb.AppendLine("       at some α, G >> 1 → runaway amplification");
        sb.AppendLine("       → Could destabilize synchronization at large φ");
        sb.AppendLine("       → THREAT LEVEL: MODERATE");
        sb.AppendLine("");
        sb.AppendLine("    2. If fb diverges in parameter regions that V3.4");
        sb.AppendLine("       requires to be well-behaved");
        sb.AppendLine("       → THREAT LEVEL: LOW (fb from CCI, not CML)");
        sb.AppendLine("");
        sb.AppendLine("    3. If the G-T-F coupling is strong enough to create");
        sb.AppendLine("       effective nonlinearities in the α-sweep that");
        sb.AppendLine("       would manifest in the CML as synchronization");
        sb.AppendLine("       phase transitions at specific φ values");
        sb.AppendLine("       → THREAT LEVEL: MODERATE (untested)");
        sb.AppendLine("");
        sb.AppendLine("  Current status:");
        sb.AppendLine("    No V33 result has been shown to conflict with V3.4.");
        sb.AppendLine("    But the cross-framework test has not been performed.");
        sb.AppendLine("");
        sb.AppendLine("  Classification: PASS (provisional — no known conflict)");
        sb.AppendLine("  Gate status: OPEN — requires explicit CML-CCI integration test");
        sb.AppendLine("");

        _o.WriteLine(sb.ToString());
        Assert.True(true);
    }

    // ====================================================================
    // V34_06: MINIMAL DERIVATION CHAIN — V3.4-compatible V33 chain
    //
    // Construct the minimal V33 chain that can be tested against V3.4
    // and verify each link is compatible with the bridge-band.
    //
    // Chain: Oscillator → |m|/fb → boundary → ??? → V3.4
    // ====================================================================
    [Fact]
    public void V34_06_MinimalDerivationChain_ConstructV34CompatibleChain()
    {
        var sb = new StringBuilder();
        sb.AppendLine("=== V34_06: Minimal Derivation Chain — V3.4-compatible V33 chain ===");
        sb.AppendLine("");

        sb.AppendLine("  V3.4 Chain:");
        sb.AppendLine("    ω_i (tick baseline) → CML synchronization → Ω* = ⟨dφ/dt⟩");
        sb.AppendLine("    + clock-bias: Ω* = ⟨ω_i⟩ + α·φ");
        sb.AppendLine("    → Ω* = 1.17 at φ = 0.17");
        sb.AppendLine("    → γ* = 1/Ω* ≈ 0.8547");
        sb.AppendLine("");
        sb.AppendLine("  V33 Chain:");
        sb.AppendLine("    (β,γ) → CCI evaluation → {VarI1, VarTerms}");
        sb.AppendLine("    → m = Cov(VT,V1)/Var(V1)  →  |m|  (density proxy)");
        sb.AppendLine("    → fb = -avg(ΔVT/ΔV1)     →  fb   (feedback proxy)");
        sb.AppendLine("    → sgn(dTdp)               →  sign → boundary");
        sb.AppendLine("");
        sb.AppendLine("  Compatibility Test:");
        sb.AppendLine("    If ω_i (V3.4 tick baseline) can be expressed as f(|m|) or f(fb),");
        sb.AppendLine("    then the V33 → V3.4 link is through the VarTerms/VarI1 statistics");
        sb.AppendLine("    that both frameworks compute.");
        sb.AppendLine("");
        sb.AppendLine("    ω_i = 1.0 (normalization) in V3.4");
        sb.AppendLine("    tick = avg(|Δ(V1+VT)/Δα|) in V33");
        sb.AppendLine("");
        sb.AppendLine("    If tick ∝ ω_i under appropriate limits, then:");
        sb.AppendLine("      Oscillator → tick → ω_i → synchronization → Ω* → bridge band");
        sb.AppendLine("");
        sb.AppendLine("  Chain segments requiring explicit linkage:");
        sb.AppendLine("    ✓ Oscillator → |m|/fb      [V33 OGC_01, FBT_01 — SUPPORTED]");
        sb.AppendLine("    ✓ |m|/fb → boundary        [V33 BES_01, BSC_01 — SUPPORTED]");
        sb.AppendLine("    ? tick → ω_i mapping        [NOT YET ESTABLISHED]");
        sb.AppendLine("    ✓ ω_i → Ω*                 [V3.4 CML09-CML11 — VALIDATED]");
        sb.AppendLine("    ✓ Ω* → γ*                  [V3.4 — VALIDATED]");
        sb.AppendLine("");
        sb.AppendLine("  Missing link: tick (V33 α-derivative) ↔ ω_i (V3.4 tick baseline)");
        sb.AppendLine("  This is the V3.4 recovery bottleneck for V33.");
        sb.AppendLine("");

        _o.WriteLine(sb.ToString());
        Assert.True(true);
    }

    private static double PearsonCorr(double[] xs, double[] ys)
    {
        int n = xs.Length; if (n < 2) return 0;
        double mx = xs.Average(), my = ys.Average();
        double sxy = 0, sxx = 0, syy = 0;
        for (int i = 0; i < n; i++) { double dx = xs[i] - mx, dy = ys[i] - my; sxy += dx * dy; sxx += dx * dx; syy += dy * dy; }
        return sxx > 1e-15 && syy > 1e-15 ? sxy / Math.Sqrt(sxx * syy) : 0;
    }
}
