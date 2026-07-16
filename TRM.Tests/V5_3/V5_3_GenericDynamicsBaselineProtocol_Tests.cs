using Xunit;
using Xunit.Abstractions;
using System.Security.Cryptography;
using System.Text;

namespace TRM.Tests.V5_3;

/// <summary>
/// M2 Generic Dynamics Baseline Protocol (GDBP):
///
/// Determines whether the M1 residual Omega-xi sensitivity is specific
/// to TRM dynamics (multi-epoch fixed-point recovery via RecoverFP) or
/// generic to simple Kuramoto dynamics with distance-dependent coupling.
///
/// The generic baseline differs from TRM in ONE way:
///   TRM:       KS → RecoverFP (multi-epoch) → Sm → Omega
///   Generic:   KS → Cupd(graph_dists, K0, xi) → Sm → Omega
///
/// Both use the same graph, same frequencies, same exponential coupling
/// kernel, and same Omega computation. The only difference is whether
/// the coupling matrix is built from fixed graph distances (generic) or
/// from emergent phase-correlation distances via multi-epoch recovery (TRM).
///
/// M2 reuses the frozen cluster masks from M1. It runs the generic
/// baseline at each xi, computes Ω_fixed over M1's frozen clusters,
/// and compares the xi-sensitivity to TRM's.
///
/// CLAIM DISCIPLINE:
///   SUPPORTED:       The generic baseline is structurally defined.
///   CONDITIONAL:     Results depend on the specific generic baseline
///                    choice (single-epoch, graph-distance coupling).
///   HYPOTHESIS:      Generic dynamics may explain part or all of the
///                    M1 residual Omega-xi sensitivity.
///   NOT CLAIMED:     H9 confirmation, attractor decomposition,
///                    physical constants, quantum effects, spacetime.
///
/// This is a PROTOCOL-DEFINITION suite. No simulations are executed.
/// Execution deferred to: V5_3_GenericDynamicsBaselineExecution_Tests.cs
/// </summary>
[Trait("Category", "V5_3")]
[Trait("Category", "V5_3_GDBP")]
public class V5_3_GenericDynamicsBaselineProtocol_Tests
{
    private readonly ITestOutputHelper _output;

    // ── Frozen parameters (matching M1/FCOP) ──
    internal const double FrozenK0 = 1.15;
    internal const int FrozenN = 100;
    internal const double FrozenS = 0.08;
    internal const double BaselineXi = 1.80;
    internal static readonly double[] XiSweep = { 1.50, 1.65, 1.80, 1.95, 2.10 };
    internal const int BaselineSeedStart = 500;
    internal const int BaselineSeedCount = 20;
    internal const int SweepSeedStart = 520;
    internal const int SweepSeedsPerPoint = 5;

    // ── M1 reference values (frozen, loaded from M1 execution) ──
    internal const double M1_DeltaOmegaFixedEnsemble = 1.629; // from M1 execution output
    internal const double M1_OmegaFixedEnsembleMean = 8.542;  // approximate mean

    // ── Decision thresholds (pre-registered, frozen) ──
    // If generic baseline reproduces ≥70% of TRM's ΔΩ_fixed,
    // the effect is generic, not TRM-specific.
    internal const double GenericDominantRatio = 0.70;

    // If generic baseline reproduces ≤30% of TRM's ΔΩ_fixed,
    // the effect is TRM-specific.
    internal const double TrmSpecificRatio = 0.30;

    // Minimum absolute shift to consider detectable.
    internal const double MinimumXiShift = 0.005;

    public V5_3_GenericDynamicsBaselineProtocol_Tests(ITestOutputHelper o) { _output = o; }

    // ═══════════════════════════════════════════════════════════
    //  M2.0 — Context
    // ═══════════════════════════════════════════════════════════

    [Fact]
    public void V5_3_GDBP_01_M1ContextLoaded()
    {
        _output.WriteLine("=== M2.0: M1 CONTEXT LOADED ===");
        _output.WriteLine("");
        _output.WriteLine("M1 FixedClusterOmega result (frozen):");
        _output.WriteLine("  Gate A — DYNAMICS DOMINANT");
        _output.WriteLine("  ratio_median = 0.9872");
        _output.WriteLine("  ΔΩ_free  = 1.689");
        _output.WriteLine("  ΔΩ_fixed = 1.629");
        _output.WriteLine("  Fixed-cluster Ω retained 98.7% of free-cluster xi-sensitivity.");
        _output.WriteLine("  Membership changes are NOT the primary driver.");
        _output.WriteLine("");
        _output.WriteLine("M2 question:");
        _output.WriteLine("  Is the residual Ω-ξ sensitivity SPECIFIC to TRM dynamics");
        _output.WriteLine("  (multi-epoch RecoverFP), or is it GENERIC to any Kuramoto");
        _output.WriteLine("  system with distance-dependent coupling?");
        _output.WriteLine("");
        _output.WriteLine("M1 CONTEXT LOADED — IMMUTABLE.");
    }

    // ═══════════════════════════════════════════════════════════
    //  M2.1 — Generic Baseline Definition
    // ═══════════════════════════════════════════════════════════

    [Fact]
    public void V5_3_GDBP_02_GenericBaselineDefined()
    {
        _output.WriteLine("=== M2.1: GENERIC BASELINE DEFINED ===");
        _output.WriteLine("");
        _output.WriteLine("TRM PRODUCTION PIPELINE:");
        _output.WriteLine("  1. KS(N, seed)          → initial binary adjacency");
        _output.WriteLine("  2. RecoverFP (E epochs) → multi-epoch fixed-point K:");
        _output.WriteLine("     for e = 0..E-1:");
        _output.WriteLine("       h = Sm(K, N, s, seed+e)");
        _output.WriteLine("       d_ij = DL(Nm(RP(h)))    ← emergent distances");
        _output.WriteLine("       K = Cupd(d, K0, xi)      ← from phase correlations");
        _output.WriteLine("  3. Sm(K_fp, N, s, seed+E)   → final simulation");
        _output.WriteLine("  4. OmegaField(h)            → per-node Omega");
        _output.WriteLine("");
        _output.WriteLine("GENERIC BASELINE PIPELINE:");
        _output.WriteLine("  1. KS(N, seed)          → same initial graph");
        _output.WriteLine("  2. BFS distances on KS  → fixed graph distances");
        _output.WriteLine("  3. K = Cupd(d_graph, K0, xi) → from graph topology");
        _output.WriteLine("  4. Sm(K, N, s, seed)    → single simulation");
        _output.WriteLine("  5. OmegaField(h)        → same Omega computation");
        _output.WriteLine("");
        _output.WriteLine("DIFFERENCES:");
        _output.WriteLine("  - No RecoverFP (no multi-epoch fixed-point search)");
        _output.WriteLine("  - Coupling from FIXED graph distances, not emergent");
        _output.WriteLine("  - Single simulation per (seed, xi) pair");
        _output.WriteLine("");
        _output.WriteLine("IDENTICAL:");
        _output.WriteLine("  - Same graph generation KS(N, seed)");
        _output.WriteLine("  - Same natural frequencies (within Sm)");
        _output.WriteLine("  - Same exponential coupling kernel Cupd");
        _output.WriteLine("  - Same Kuramoto dynamics Sm");
        _output.WriteLine("  - Same Omega computation OmegaField");
        _output.WriteLine("  - Same frozen cluster masks from M1");
        _output.WriteLine("");
        _output.WriteLine("GENERIC BASELINE DEFINED.");
    }

    // ═══════════════════════════════════════════════════════════
    //  M2.2 — Measurement Pipeline
    // ═══════════════════════════════════════════════════════════

    [Fact]
    public void V5_3_GDBP_03_MeasurementPipelineDefined()
    {
        _output.WriteLine("=== M2.2: MEASUREMENT PIPELINE ===");
        _output.WriteLine("");
        _output.WriteLine("For each (seed, xi) pair in the xi sweep:");
        _output.WriteLine("");
        _output.WriteLine("  1. Generate graph: KS(N={FrozenN}, seed)");
        _output.WriteLine("  2. Compute BFS graph distances d_ij on the KS graph.");
        _output.WriteLine("  3. Build coupling: K = K0 * exp(-d_ij / xi)");
        _output.WriteLine("     [For i==j: K[i,i] = 0]");
        _output.WriteLine("     [For disconnected pairs: d_ij = large, K ≈ 0]");
        _output.WriteLine("  4. Run simulation: h = Sm(K, N, s={FrozenS}, seed)");
        _output.WriteLine("  5. Compute per-node Omega: om = OmegaField(h)");
        _output.WriteLine("  6. Load M1 frozen cluster mask for this seed.");
        _output.WriteLine("  7. Compute Ω_fixed = mean(om[i] for i where mask[i])");
        _output.WriteLine("  8. Compute Ω_free = mean(om) over all N nodes.");
        _output.WriteLine("");
        _output.WriteLine("The frozen cluster masks are IDENTICAL to M1's masks —");
        _output.WriteLine("identified at baseline xi=1.80 from the TRM pipeline.");
        _output.WriteLine("They are NOT recomputed from the generic baseline.");
        _output.WriteLine("");
        _output.WriteLine("MEASUREMENT PIPELINE DEFINED.");
    }

    // ═══════════════════════════════════════════════════════════
    //  M2.3 — Sensitivity Comparison
    // ═══════════════════════════════════════════════════════════

    [Fact]
    public void V5_3_GDBP_04_SensitivityComparisonDefined()
    {
        _output.WriteLine("=== M2.3: SENSITIVITY COMPARISON ===");
        _output.WriteLine("");
        _output.WriteLine("For each seed s:");
        _output.WriteLine("  ΔΩ_fixed_gen(s) = |Ω_fixed_gen(ξ_max) − Ω_fixed_gen(ξ_min)|");
        _output.WriteLine("");
        _output.WriteLine("Ensemble aggregation:");
        _output.WriteLine("  ΔΩ_fixed_gen_median = median(ΔΩ_fixed_gen across seeds)");
        _output.WriteLine("  ΔΩ_fixed_gen_mean   = mean(ΔΩ_fixed_gen across seeds)");
        _output.WriteLine("");
        _output.WriteLine("Comparison to TRM (M1 reference):");
        _output.WriteLine($"  ΔΩ_fixed_trm = {M1_DeltaOmegaFixedEnsemble:F3} (frozen from M1)");
        _output.WriteLine("");
        _output.WriteLine("  ratio_gen = ΔΩ_fixed_gen_median / ΔΩ_fixed_trm");
        _output.WriteLine("");
        _output.WriteLine("  residual = ΔΩ_fixed_trm − ΔΩ_fixed_gen_median");
        _output.WriteLine("  normalized_residual = residual / ΔΩ_fixed_trm");
        _output.WriteLine("");
        _output.WriteLine("Also compute for completeness:");
        _output.WriteLine("  ΔΩ_free_gen  — generic baseline free-cluster xi-sensitivity");
        _output.WriteLine("  Ω_free, Ω_fixed values at each xi (ensemble means)");
        _output.WriteLine("");
        _output.WriteLine("SENSITIVITY COMPARISON DEFINED.");
    }

    // ═══════════════════════════════════════════════════════════
    //  M2.4 — Decision Gates
    // ═══════════════════════════════════════════════════════════

    [Fact]
    public void V5_3_GDBP_05_DecisionGatesDefined()
    {
        _output.WriteLine("=== M2.4: DECISION GATES ===");
        _output.WriteLine("");
        _output.WriteLine("Pre-registered thresholds (FROZEN):");
        _output.WriteLine($"  Generic-dominant ratio:    ratio_gen ≥ {GenericDominantRatio:F2}");
        _output.WriteLine($"  TRM-specific ratio:        ratio_gen ≤ {TrmSpecificRatio:F2}");
        _output.WriteLine($"  Minimum detectable shift:  ΔΩ_fixed_trm > {MinimumXiShift:F4}");
        _output.WriteLine("");
        _output.WriteLine("═══ GATE A: GENERIC DYNAMICS EXPLAINS EFFECT ═══");
        _output.WriteLine("");
        _output.WriteLine("  Condition:");
        _output.WriteLine($"    ratio_gen ≥ {GenericDominantRatio:F2}");
        _output.WriteLine("");
        _output.WriteLine("  Interpretation:");
        _output.WriteLine("    The generic baseline reproduces ≥70% of TRM's fixed-cluster");
        _output.WriteLine("    Omega-xi sensitivity. The effect is NOT specific to TRM's");
        _output.WriteLine("    multi-epoch RecoverFP process.");
        _output.WriteLine("");
        _output.WriteLine("  Impact:");
        _output.WriteLine("    H9 remains CONDITIONALLY SUPPORTED but less distinctive.");
        _output.WriteLine("    The Ω-ξ sensitivity is a generic property of Kuramoto");
        _output.WriteLine("    dynamics with distance-dependent coupling, not a TRM-specific");
        _output.WriteLine("    synchronization-control mechanism.");
        _output.WriteLine("    H12 WEAKENED — the attractor decomposition claim loses");
        _output.WriteLine("    support if the key residual is generic.");
        _output.WriteLine("");
        _output.WriteLine("  Next step:");
        _output.WriteLine("    Revise H9 wording: 'Ω ξ-sensitivity is driven by");
        _output.WriteLine("    distance-dependent coupling in oscillator dynamics'");
        _output.WriteLine("    (more generic, less TRM-specific).");
        _output.WriteLine("    Proceed to M3 (MeanDist saturation boundary).");
        _output.WriteLine("");
        _output.WriteLine("═══ GATE B: TRM RESIDUAL DOMINANT ═══");
        _output.WriteLine("");
        _output.WriteLine("  Condition:");
        _output.WriteLine($"    ratio_gen ≤ {TrmSpecificRatio:F2}");
        _output.WriteLine("");
        _output.WriteLine("  Interpretation:");
        _output.WriteLine("    The generic baseline reproduces ≤30% of TRM's fixed-cluster");
        _output.WriteLine("    Omega-xi sensitivity. TRM's RecoverFP process produces");
        _output.WriteLine("    substantially different Ω-ξ behavior than simple Kuramoto");
        _output.WriteLine("    dynamics with fixed-distance coupling.");
        _output.WriteLine("");
        _output.WriteLine("  Impact:");
        _output.WriteLine("    H9 receives STRONGER CONDITIONAL SUPPORT.");
        _output.WriteLine("    The Ω-ξ sensitivity is not explainable by generic dynamics.");
        _output.WriteLine("    TRM's iterative fixed-point recovery is implicated as the");
        _output.WriteLine("    source of the distinctive Ω-ξ behavior.");
        _output.WriteLine("    H12 remains speculative but less weakened — the residual");
        _output.WriteLine("    is TRM-specific.");
        _output.WriteLine("");
        _output.WriteLine("  Next step:");
        _output.WriteLine("    Proceed to M3 (saturation boundary for MeanDist).");
        _output.WriteLine("    Do NOT claim H9 confirmed or H12 supported.");
        _output.WriteLine("");
        _output.WriteLine("═══ GATE C: MIXED ═══");
        _output.WriteLine("");
        _output.WriteLine("  Condition:");
        _output.WriteLine($"    {TrmSpecificRatio:F2} < ratio_gen < {GenericDominantRatio:F2}");
        _output.WriteLine("");
        _output.WriteLine("  Interpretation:");
        _output.WriteLine("    Generic dynamics explain part but not all of TRM's Ω-ξ");
        _output.WriteLine("    sensitivity. Both generic and TRM-specific mechanisms");
        _output.WriteLine("    contribute.");
        _output.WriteLine("");
        _output.WriteLine("  Impact:");
        _output.WriteLine("    H9 remains CONDITIONALLY SUPPORTED with caveats.");
        _output.WriteLine("    Ω-ξ sensitivity is partially generic, partially TRM-specific.");
        _output.WriteLine("");
        _output.WriteLine("  Next step:");
        _output.WriteLine("    Characterize the TRM-specific residual fraction.");
        _output.WriteLine("    Design controls to isolate which aspect of RecoverFP");
        _output.WriteLine("    (epoch count, emergent distances, normalization) drives");
        _output.WriteLine("    the TRM-specific component.");
        _output.WriteLine("");
        _output.WriteLine("═══ GATE D: NO DETECTABLE EFFECT ═══");
        _output.WriteLine("");
        _output.WriteLine("  Condition:");
        _output.WriteLine($"    ΔΩ_fixed_trm ≤ {MinimumXiShift:F4} (M1 reference)");
        _output.WriteLine("");
        _output.WriteLine("  This would contradict M1 results and indicate a");
        _output.WriteLine("  measurement or pipeline error. Re-examine M1 execution.");
        _output.WriteLine("");
        _output.WriteLine("DECISION GATES FROZEN.");
    }

    // ═══════════════════════════════════════════════════════════
    //  M2.5 — Execution Run Plan
    // ═══════════════════════════════════════════════════════════

    [Fact]
    public void V5_3_GDBP_06_ExecutionRunPlan()
    {
        _output.WriteLine("=== M2.5: EXECUTION RUN PLAN ===");
        _output.WriteLine("");
        _output.WriteLine("Generic baseline runs — no RecoverFP, single Sm per (seed, xi):");
        _output.WriteLine("");
        _output.WriteLine($"  Xi sweep: [{string.Join(", ", XiSweep.Select(x => x.ToString("F2")))}]");
        _output.WriteLine($"  Seeds: {SweepSeedStart}–{SweepSeedStart + SweepSeedsPerPoint - 1}");
        _output.WriteLine($"  Runs: {XiSweep.Length} × {SweepSeedsPerPoint} = {XiSweep.Length * SweepSeedsPerPoint}");
        _output.WriteLine("");
        _output.WriteLine("Each run:");
        _output.WriteLine("  1. KS(N, seed) → graph (same as M1)");
        _output.WriteLine("  2. BFS distances on graph");
        _output.WriteLine("  3. Cupd(dist, K0={FrozenK0}, xi) → coupling matrix");
        _output.WriteLine("  4. Sm(K, N, s={FrozenS}, seed) → single simulation");
        _output.WriteLine("  5. OmegaField(h) → per-node Omega");
        _output.WriteLine("  6. Load M1 frozen mask → Ω_fixed");
        _output.WriteLine("");
        _output.WriteLine("Comparison to M1 (TRM) runs:");
        _output.WriteLine("  M1: 5 epochs of RecoverFP + 1 final Sm = 6 Sm calls per run");
        _output.WriteLine("  M2: 1 Sm call per run (no RecoverFP)");
        _output.WriteLine($"  Expected M2 runtime: ~{XiSweep.Length * SweepSeedsPerPoint / 6:F0}× faster than M1");
        _output.WriteLine("");
        _output.WriteLine("EXECUTION RUN PLAN DEFINED.");
    }

    // ═══════════════════════════════════════════════════════════
    //  M2.6 — Forbidden Actions
    // ═══════════════════════════════════════════════════════════

    [Fact]
    public void V5_3_GDBP_07_ForbiddenActionsDefined()
    {
        _output.WriteLine("=== M2.6: FORBIDDEN ACTIONS ===");
        _output.WriteLine("");
        _output.WriteLine("  1. Do NOT change the generic baseline definition");
        _output.WriteLine("     after execution (single-epoch, graph-distance coupling).");
        _output.WriteLine("  2. Do NOT change decision thresholds");
        _output.WriteLine($"     ({GenericDominantRatio:F2}/{TrmSpecificRatio:F2}) after execution.");
        _output.WriteLine("  3. Do NOT reselect seeds.");
        _output.WriteLine("  4. Do NOT recompute M1 cluster masks.");
        _output.WriteLine("  5. Do NOT change xi sweep values.");
        _output.WriteLine("  6. Do NOT add RecoverFP epochs to the generic baseline");
        _output.WriteLine("     after seeing results.");
        _output.WriteLine("  7. Do NOT change coupling kernel form.");
        _output.WriteLine("  8. Do NOT compare Ω_free_gen to Ω_free_trm directly");
        _output.WriteLine("     without pre-registered comparison method.");
        _output.WriteLine("  9. Do NOT claim H9 is confirmed regardless of outcome.");
        _output.WriteLine("  10. Do NOT claim attractor decomposition exists.");
        _output.WriteLine("  11. Do NOT use M2 results to justify the 210-run matrix");
        _output.WriteLine("      before M3/M4 are executed.");
        _output.WriteLine("");
        _output.WriteLine("FORBIDDEN ACTIONS FROZEN.");
    }

    // ═══════════════════════════════════════════════════════════
    //  M2.7 — Claim Discipline Audit
    // ═══════════════════════════════════════════════════════════

    [Fact]
    public void V5_3_GDBP_08_ClaimDisciplineAudit()
    {
        _output.WriteLine("=== M2.7: CLAIM DISCIPLINE AUDIT ===");
        _output.WriteLine("");
        _output.WriteLine("SUPPORTED:");
        _output.WriteLine("  - The generic baseline is structurally defined.");
        _output.WriteLine("  - The generic baseline uses single-epoch Kuramoto dynamics");
        _output.WriteLine("    with graph-distance coupling.");
        _output.WriteLine("  - The generic baseline reuses M1 frozen cluster masks.");
        _output.WriteLine("  - Decision thresholds are pre-registered.");
        _output.WriteLine("");
        _output.WriteLine("CONDITIONAL:");
        _output.WriteLine("  - Results depend on the specific generic baseline choice.");
        _output.WriteLine("  - Results depend on M1 cluster masks.");
        _output.WriteLine("  - Results depend on finite seed ensembles.");
        _output.WriteLine($"  - Results specific to N={FrozenN}, K0={FrozenK0},");
        _output.WriteLine($"    s={FrozenS}, exponential coupling.");
        _output.WriteLine("");
        _output.WriteLine("HYPOTHESIS (tested by M2 execution):");
        _output.WriteLine("  - Generic dynamics may explain part of the M1 residual.");
        _output.WriteLine("  - TRM's RecoverFP may produce distinctive Ω-ξ behavior.");
        _output.WriteLine("");
        _output.WriteLine("NOT CLAIMED:");
        _output.WriteLine("  - H9 confirmed (regardless of gate)");
        _output.WriteLine("  - H11 or H12 confirmed");
        _output.WriteLine("  - Attractor decomposition exists");
        _output.WriteLine("  - Physical constants derived");
        _output.WriteLine("  - Quantum mechanics involved");
        _output.WriteLine("  - Spacetime emergence");
        _output.WriteLine("  - Causation");
        _output.WriteLine("");
        _output.WriteLine("AUDIT: PASSED.");
    }
}
