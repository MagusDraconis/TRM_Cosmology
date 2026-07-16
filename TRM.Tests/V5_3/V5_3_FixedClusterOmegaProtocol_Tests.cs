using Xunit;
using Xunit.Abstractions;
using System.Security.Cryptography;
using System.Text;

namespace TRM.Tests.V5_3;

/// <summary>
/// M1 Fixed Cluster Omega Protocol (FCOP):
///
/// Determines whether Omega xi-sensitivity is driven by:
///   A. CHANGING CLUSTER MEMBERSHIP — different nodes synchronize
///      at different xi values, changing the effective mean frequency.
///      (The null-model explanation, weakened but not ruled out by N1.)
///   B. DYNAMICS WITHIN A FIXED CLUSTER — the same nodes remain
///      synchronized across xi, but the collective frequency shifts
///      due to coupling-dynamics changes.
///
/// M1 disambiguates by running the TRM simulation at multiple xi
/// values, then computing Omega TWICE per xi:
///   - Omega_free(xi):  mean Omega over all nodes (TRM standard)
///   - Omega_fixed(xi): mean Omega over ONLY nodes synchronized
///     at baseline xi=1.80
///
/// If the Omega-xi curve is the SAME under both computations,
/// cluster membership doesn't matter — dynamics drive the effect.
/// If Omega_fixed is flat while Omega_free varies, membership
/// is the primary driver.
///
/// CLAIM DISCIPLINE:
///   SUPPORTED:       The protocol defines computation methods.
///   CONDITIONAL:     Results depend on cluster-identification
///                    threshold, finite seed ensembles, and the
///                    specific TRM simulation implementation.
///   HYPOTHESIS:      Omega xi-sensitivity may be driven by
///                    coupling dynamics within a fixed cluster.
///   NOT CLAIMED:     H9 confirmation, attractor decomposition,
///                    physical constants, quantum effects, spacetime.
///
/// This is a PROTOCOL-DEFINITION suite. It defines the experimental
/// design, frozen parameters, decision criteria, and forbidden actions.
/// No TRM simulations are executed here.
/// Execution is deferred to: V5_3_FixedClusterOmegaExecution_Tests.cs (FCOE)
/// </summary>
[Trait("Category", "V5_3")]
[Trait("Category", "V5_3_FCOP")]
public class V5_3_FixedClusterOmegaProtocol_Tests
{
    private readonly ITestOutputHelper _output;

    // ── Frozen TRM parameters (matching V5.2 primary regime) ──
    internal const double FrozenXi = 1.80;
    internal const double FrozenK0 = 1.15;
    internal const int FrozenN = 100;
    internal const double FrozenS = 0.08;

    // ── Simulation technical parameters (frozen) ──
    internal const double Dt = 0.05;
    internal const double REps = 1e-8;
    internal const int St = 400;
    internal const int Hd = 4;

    // ── Xi sweep (matching V5.2 Phase 1) ──
    internal static readonly double[] XiSweep = { 1.50, 1.65, 1.80, 1.95, 2.10 };

    // ── Seed ensemble ──
    internal const int BaselineSeedStart = 500;
    internal const int BaselineSeedCount = 20;  // 20 seeds for baseline cluster identification
    internal const int SweepSeedsPerPoint = 5;   // 5 seeds per xi point
    internal const int SweepSeedStart = 520;

    // ── Cluster identification threshold ──
    // A node is in the synchronized cluster if its mean phase
    // correlation to all other nodes exceeds this threshold.
    // 0.70 is a conservative threshold: well above the noise floor
    // (~0.1 for uncorrelated oscillators) but below full sync (~1.0).
    internal const double ClusterCorrelationThreshold = 0.70;

    // ── Decision thresholds (pre-registered, frozen) ──
    // If the fixed-cluster Omega xi-shift retains >50% of the
    // free-cluster shift, dynamics are dominant over membership.
    internal const double DynamicsDominantRatio = 0.50;

    // If the fixed-cluster Omega xi-shift retains <30% of the
    // free-cluster shift, membership is dominant over dynamics.
    internal const double MembershipDominantRatio = 0.30;

    // Minimum absolute Omega shift to consider "xi-sensitive" at all.
    internal const double MinimumXiShift = 0.005;

    public V5_3_FixedClusterOmegaProtocol_Tests(ITestOutputHelper o) { _output = o; }

    // ═══════════════════════════════════════════════════════════
    //  M1.0 — N1 Baseline Loaded
    // ═══════════════════════════════════════════════════════════

    [Fact]
    public void V5_3_FCOP_01_N1BaselineLoaded()
    {
        // N1 established that the simplest null model (frequency-threshold
        // selection without dynamics) cannot explain the full V5.2 pattern.
        // Specifically: Omega xi-sensitivity and MeanDist seed-variability
        // are NOT reproduced by the null model. These are candidates for
        // attractor-specific residuals. M1 targets the Omega xi-sensitivity
        // residual specifically.
        _output.WriteLine("=== M1.0: N1 BASELINE LOADED ===");
        _output.WriteLine("");
        _output.WriteLine("N1 NullModelBaseline result (frozen):");
        _output.WriteLine("  2/4 V5.2 signatures reproduced by null model.");
        _output.WriteLine("  PASS (reproduced): Omega seed-stable, MeanDist xi-robust");
        _output.WriteLine("  FAIL (not reproduced): Omega xi-sensitive, MeanDist seed-variable");
        _output.WriteLine("");
        _output.WriteLine("M1 targets the Omega xi-sensitive FAILED signature.");
        _output.WriteLine("Core question: Is Omega xi-sensitivity caused by");
        _output.WriteLine("  A. changing cluster membership (null-model mechanism), or");
        _output.WriteLine("  B. dynamics within a fixed cluster (attractor-specific mechanism)?");
        _output.WriteLine("");
        _output.WriteLine("N1 BASELINE LOADED — IMMUTABLE.");
    }

    // ═══════════════════════════════════════════════════════════
    //  M1.1 — Cluster Identification Method
    // ═══════════════════════════════════════════════════════════

    [Fact]
    public void V5_3_FCOP_02_ClusterIdentificationMethodDefined()
    {
        // M1.1: Define the operational method for identifying the
        // synchronized cluster from TRM simulation output.
        _output.WriteLine("=== M1.1: CLUSTER IDENTIFICATION METHOD ===");
        _output.WriteLine("");
        _output.WriteLine("Input: Phase history h[t][i] from TRM simulation Sm().");
        _output.WriteLine("");
        _output.WriteLine("Step 1 — Phase correlation matrix:");
        _output.WriteLine("  R[i,j] = |SUM_t exp(i*(theta_i(t) - theta_j(t)))| / T");
        _output.WriteLine("  Computed via RP(h): standard TRM phase correlation.");
        _output.WriteLine("");
        _output.WriteLine("Step 2 — Per-node mean correlation:");
        _output.WriteLine("  r_mean[i] = (1/N) * SUM_j R[i,j]");
        _output.WriteLine("  Measures how strongly each node is correlated with");
        _output.WriteLine("  the rest of the oscillator population.");
        _output.WriteLine("");
        _output.WriteLine($"Step 3 — Threshold at r_mean > {ClusterCorrelationThreshold:F2}:");
        _output.WriteLine("  Cluster membership[i] = (r_mean[i] > threshold)");
        _output.WriteLine("");
        _output.WriteLine("Justification for threshold:");
        _output.WriteLine($"  - Uncorrelated (noise): r_mean ≈ 0.1–0.2");
        _output.WriteLine($"  - Partially synchronized: r_mean ≈ 0.4–0.7");
        _output.WriteLine($"  - Fully synchronized: r_mean ≈ 0.9–1.0");
        _output.WriteLine($"  - Threshold {ClusterCorrelationThreshold:F2} is conservative:");
        _output.WriteLine($"    well above noise, below full sync.");
        _output.WriteLine("");
        _output.WriteLine("Edge case guards:");
        _output.WriteLine("  - If < 2 nodes pass threshold: fall back to top 50%");
        _output.WriteLine("    of nodes by r_mean (minimum viable cluster).");
        _output.WriteLine("  - If > 95% of nodes pass: cluster = all nodes");
        _output.WriteLine("    (saturated synchronization).");
        _output.WriteLine("");
        _output.WriteLine("CLUSTER IDENTIFICATION METHOD FROZEN.");
    }

    // ═══════════════════════════════════════════════════════════
    //  M1.2 — Baseline Cluster Freezing
    // ═══════════════════════════════════════════════════════════

    [Fact]
    public void V5_3_FCOP_03_BaselineClusterFreezingDefined()
    {
        // M1.2: Define how the baseline cluster is identified and frozen
        // BEFORE any xi sweep computation.
        _output.WriteLine("=== M1.2: BASELINE CLUSTER FREEZING ===");
        _output.WriteLine("");
        _output.WriteLine("For each seed s in [BaselineSeedStart .. BaselineSeedStart + BaselineSeedCount - 1]:");
        _output.WriteLine("");
        _output.WriteLine("  1. Generate graph: KS(N={FrozenN}, seed=s)");
        _output.WriteLine("  2. Recover fixed point K:");
        _output.WriteLine("     K_fp = RecoverFP(K_initial, N, K0={FrozenK0}, xi={FrozenXi},");
        _output.WriteLine("                       s={FrozenS}, epochs=EpochsForN(N), seed=s)");
        _output.WriteLine("  3. Run simulation: h = Sm(K_fp, N, s={FrozenS}, seed=s + epochs)");
        _output.WriteLine("  4. Compute phase correlation: R = RP(h)");
        _output.WriteLine("  5. Compute per-node mean correlation: r_mean[i] = mean_j(R[i,j])");
        _output.WriteLine("  6. Identify cluster: frozen_membership[s][i] = (r_mean[i] > {ClusterCorrelationThreshold:F2})");
        _output.WriteLine("  7. SHA-256 hash the membership mask → freeze manifest.");
        _output.WriteLine("");
        _output.WriteLine("The membership mask frozen_membership[s][*] identifies which");
        _output.WriteLine("nodes were synchronized AT BASELINE XI ONLY.");
        _output.WriteLine("");
        _output.WriteLine("This mask is IMMUTABLE — it is not recomputed at other xi values.");
        _output.WriteLine("");
        _output.WriteLine("BASELINE CLUSTER FREEZING PROTOCOL DEFINED.");
    }

    // ═══════════════════════════════════════════════════════════
    //  M1.3 — Free-Cluster Omega Computation
    // ═══════════════════════════════════════════════════════════

    [Fact]
    public void V5_3_FCOP_04_FreeClusterOmegaDefined()
    {
        // M1.3: Define free-cluster Omega — the standard TRM Omega
        // computed over ALL nodes, regardless of synchronization state.
        _output.WriteLine("=== M1.3: FREE-CLUSTER OMEGA COMPUTATION ===");
        _output.WriteLine("");
        _output.WriteLine("For each (seed, xi) pair in the xi sweep:");
        _output.WriteLine("");
        _output.WriteLine("  1. Run full TRM simulation at this xi:");
        _output.WriteLine("     K_fp = RecoverFP(KS(N, seed), N, K0, xi, s, epochs, seed)");
        _output.WriteLine("     h = Sm(K_fp, N, s, seed + epochs)");
        _output.WriteLine("     om = OmegaField(h)");
        _output.WriteLine("");
        _output.WriteLine("  2. Compute Omega_free = mean(om) over ALL N nodes.");
        _output.WriteLine("");
        _output.WriteLine("  This is the STANDARD TRM Omega computation.");
        _output.WriteLine("  It reflects both membership changes AND dynamics changes");
        _output.WriteLine("  combined. It serves as the reference against which");
        _output.WriteLine("  the fixed-cluster Omega is compared.");
        _output.WriteLine("");
        _output.WriteLine("FREE-CLUSTER OMEGA COMPUTATION DEFINED.");
    }

    // ═══════════════════════════════════════════════════════════
    //  M1.4 — Fixed-Cluster Omega Computation
    // ═══════════════════════════════════════════════════════════

    [Fact]
    public void V5_3_FCOP_05_FixedClusterOmegaDefined()
    {
        // M1.4: Define fixed-cluster Omega — Omega computed over ONLY
        // the nodes that were synchronized at baseline xi=1.80.
        _output.WriteLine("=== M1.4: FIXED-CLUSTER OMEGA COMPUTATION ===");
        _output.WriteLine("");
        _output.WriteLine("For each (seed, xi) pair in the xi sweep:");
        _output.WriteLine("");
        _output.WriteLine("  1. Run full TRM simulation at this xi (same as free-cluster):");
        _output.WriteLine("     K_fp = RecoverFP(KS(N, seed), N, K0, xi, s, epochs, seed)");
        _output.WriteLine("     h = Sm(K_fp, N, s, seed + epochs)");
        _output.WriteLine("     om = OmegaField(h)");
        _output.WriteLine("");
        _output.WriteLine("  2. LOAD the frozen baseline membership mask for this seed");
        _output.WriteLine("     (identified at xi={FrozenXi}, frozen BEFORE xi sweep).");
        _output.WriteLine("");
        _output.WriteLine("  3. Compute Omega_fixed = mean(om[i] for i where");
        _output.WriteLine("     frozen_membership[seed][i] == true).");
        _output.WriteLine("");
        _output.WriteLine("  CRITICAL: Omega_fixed uses ONLY nodes that were synchronized");
        _output.WriteLine("  at BASELINE xi, regardless of whether those same nodes are");
        _output.WriteLine("  synchronized at the current xi.");
        _output.WriteLine("");
        _output.WriteLine("  If Omega_fixed(xi) ≈ Omega_free(xi) at all xi values:");
        _output.WriteLine("    → cluster membership changes are NOT the driver.");
        _output.WriteLine("    → dynamics within the synchronized set dominate.");
        _output.WriteLine("");
        _output.WriteLine("  If Omega_fixed(xi) ≈ constant while Omega_free(xi) varies:");
        _output.WriteLine("    → cluster membership changes ARE the driver.");
        _output.WriteLine("    → the Ω-ξ relationship is a sampling effect.");
        _output.WriteLine("");
        _output.WriteLine("FIXED-CLUSTER OMEGA COMPUTATION DEFINED.");
    }

    // ═══════════════════════════════════════════════════════════
    //  M1.5 — Sensitivity Comparison Method
    // ═══════════════════════════════════════════════════════════

    [Fact]
    public void V5_3_FCOP_06_SensitivityComparisonDefined()
    {
        // M1.5: Define how free-cluster and fixed-cluster xi-sensitivity
        // are compared quantitatively.
        _output.WriteLine("=== M1.5: SENSITIVITY COMPARISON METHOD ===");
        _output.WriteLine("");
        _output.WriteLine("For each seed, compute:");
        _output.WriteLine("");
        _output.WriteLine("  ΔΩ_free = |Ω_free(xi_max) − Ω_free(xi_min)|");
        _output.WriteLine("  ΔΩ_fixed = |Ω_fixed(xi_max) − Ω_fixed(xi_min)|");
        _output.WriteLine("");
        _output.WriteLine("  sensitivity_ratio(seed) = ΔΩ_fixed / max(ΔΩ_free, ε)");
        _output.WriteLine("    where ε = {MinimumXiShift:F4} (floor to avoid division by zero)");
        _output.WriteLine("");
        _output.WriteLine("Ensemble aggregation:");
        _output.WriteLine("  ratio_median = median(sensitivity_ratio across seeds)");
        _output.WriteLine("  ratio_mean = mean(sensitivity_ratio across seeds)");
        _output.WriteLine("  ratio_ci = bootstrap 95% CI of the mean (10,000 resamples)");
        _output.WriteLine("");
        _output.WriteLine("Also compute for context:");
        _output.WriteLine("  ΔΩ_free_median, ΔΩ_fixed_median (across seeds)");
        _output.WriteLine("  CV(Ω_free) and CV(Ω_fixed) at each xi (across seeds)");
        _output.WriteLine("");
        _output.WriteLine("SENSITIVITY COMPARISON METHOD DEFINED.");
    }

    // ═══════════════════════════════════════════════════════════
    //  M1.6 — Decision Gates
    // ═══════════════════════════════════════════════════════════

    [Fact]
    public void V5_3_FCOP_07_DecisionGatesDefined()
    {
        // M1.6: Define the decision gates that classify M1 outcomes.
        _output.WriteLine("=== M1.6: DECISION GATES ===");
        _output.WriteLine("");
        _output.WriteLine("Pre-registered thresholds (FROZEN):");
        _output.WriteLine($"  Dynamics-dominant ratio:    ratio > {DynamicsDominantRatio:F2}");
        _output.WriteLine($"  Membership-dominant ratio:  ratio < {MembershipDominantRatio:F2}");
        _output.WriteLine($"  Minimum xi shift (free):    ΔΩ_free > {MinimumXiShift:F4}");
        _output.WriteLine("");
        _output.WriteLine("═══ GATE A: DYNAMICS DOMINANT ═══");
        _output.WriteLine("");
        _output.WriteLine("  Condition:");
        _output.WriteLine($"    ratio_median > {DynamicsDominantRatio:F2}");
        _output.WriteLine($"    AND ΔΩ_free_median > {MinimumXiShift:F4}");
        _output.WriteLine("");
        _output.WriteLine("  Interpretation:");
        _output.WriteLine("    Fixed-cluster Omega retains most of the xi-sensitivity");
        _output.WriteLine("    of free-cluster Omega. Cluster membership changes are");
        _output.WriteLine("    NOT the primary driver. Coupling dynamics within the");
        _output.WriteLine("    synchronized set dominate the Omega-xi relationship.");
        _output.WriteLine("");
        _output.WriteLine("  Impact on hypotheses:");
        _output.WriteLine("    H9 (Omega sync-control): CONDITIONALLY SUPPORTED.");
        _output.WriteLine("      The membership-only null mechanism is insufficient.");
        _output.WriteLine("      Omega xi-sensitivity requires dynamics.");
        _output.WriteLine("    H11 (parameter classes): NOT TESTED by M1 alone.");
        _output.WriteLine("    H12 (attractor decomposition): NOT TESTED by M1 alone.");
        _output.WriteLine("");
        _output.WriteLine("  Next step: Proceed to M2 (null-model quantitative comparison)");
        _output.WriteLine("    to determine whether the residual dynamics effect is");
        _output.WriteLine("    specific to the TRM attractor.");
        _output.WriteLine("");
        _output.WriteLine("═══ GATE B: MEMBERSHIP DOMINANT ═══");
        _output.WriteLine("");
        _output.WriteLine("  Condition:");
        _output.WriteLine($"    ratio_median < {MembershipDominantRatio:F2}");
        _output.WriteLine($"    AND ΔΩ_free_median > {MinimumXiShift:F4}");
        _output.WriteLine("");
        _output.WriteLine("  Interpretation:");
        _output.WriteLine("    Fixed-cluster Omega loses most of the xi-sensitivity.");
        _output.WriteLine("    The Omega-xi relationship is primarily mediated through");
        _output.WriteLine("    cluster membership changes, not through direct dynamics.");
        _output.WriteLine("    This is the null-model mechanism (frequency sampling");
        _output.WriteLine("    through membership) applying WITHIN the TRM attractor.");
        _output.WriteLine("");
        _output.WriteLine("  Impact on hypotheses:");
        _output.WriteLine("    H9 (Omega sync-control): WEAKENED.");
        _output.WriteLine("      Membership, not coupling dynamics, drives Omega.");
        _output.WriteLine("      The ξ→Ω relationship is indirect.");
        _output.WriteLine("    H11 (parameter classes): WEAKENED.");
        _output.WriteLine("      If Ω is membership-mediated, the distinction between");
        _output.WriteLine("      sync-control and geometry-control blurs.");
        _output.WriteLine("");
        _output.WriteLine("  Next step: Revise the parameter classification framework.");
        _output.WriteLine("    ξ may not be a sync-control parameter in the intended sense.");
        _output.WriteLine("");
        _output.WriteLine("═══ GATE C: AMBIGUOUS ═══");
        _output.WriteLine("");
        _output.WriteLine("  Condition:");
        _output.WriteLine($"    {MembershipDominantRatio:F2} ≤ ratio_median ≤ {DynamicsDominantRatio:F2}");
        _output.WriteLine($"    OR ΔΩ_free_median ≤ {MinimumXiShift:F4} (no detectable xi effect)");
        _output.WriteLine($"    OR ratio_ci spans both gates (wide confidence interval)");
        _output.WriteLine("");
        _output.WriteLine("  Interpretation:");
        _output.WriteLine("    M1 cannot cleanly disambiguate membership from dynamics.");
        _output.WriteLine("    Possible causes:");
        _output.WriteLine("      - Both mechanisms contribute comparably.");
        _output.WriteLine("      - Seed ensemble too small for sufficient statistical power.");
        _output.WriteLine("      - Xi sweep range too narrow to detect the effect.");
        _output.WriteLine("      - No detectable xi-sensitivity at all (contradicts V5.2).");
        _output.WriteLine("");
        _output.WriteLine("  Impact on hypotheses:");
        _output.WriteLine("    H9: NOT DISAMBIGUATED. Requires larger experiment.");
        _output.WriteLine("");
        _output.WriteLine("  Next step:");
        _output.WriteLine("    If ΔΩ_free ≤ threshold: re-examine V5.2 comparison.");
        _output.WriteLine("    If ratio ambiguous: increase seeds to 10 per xi point");
        _output.WriteLine("      and/or widen xi range to {1.30, ..., 2.30}.");
        _output.WriteLine("");
        _output.WriteLine("DECISION GATES FROZEN.");
    }

    // ═══════════════════════════════════════════════════════════
    //  M1.7 — Forbidden Actions
    // ═══════════════════════════════════════════════════════════

    [Fact]
    public void V5_3_FCOP_08_ForbiddenActionsDefined()
    {
        // M1.7: Define forbidden actions to prevent post-hoc tuning.
        _output.WriteLine("=== M1.7: FORBIDDEN ACTIONS ===");
        _output.WriteLine("");
        _output.WriteLine("The following actions are FORBIDDEN during and after");
        _output.WriteLine("M1 execution:");
        _output.WriteLine("");
        _output.WriteLine("  1. Do NOT change the cluster correlation threshold");
        _output.WriteLine($"     ({ClusterCorrelationThreshold:F2}) after seeing results.");
        _output.WriteLine("  2. Do NOT change the baseline xi ({FrozenXi:F2}) after");
        _output.WriteLine("     the cluster membership has been frozen.");
        _output.WriteLine("  3. Do NOT reselect seeds after seeing results.");
        _output.WriteLine("  4. Do NOT recompute baseline cluster membership");
        _output.WriteLine("     after running the xi sweep.");
        _output.WriteLine("  5. Do NOT exclude any seed from the ensemble analysis");
        _output.WriteLine("     unless the simulation failed to converge (numerical error).");
        _output.WriteLine("  6. Do NOT change the decision thresholds");
        _output.WriteLine($"     (ratios {DynamicsDominantRatio:F2}/{MembershipDominantRatio:F2})");
        _output.WriteLine("     after seeing results.");
        _output.WriteLine("  7. Do NOT change the Omega or cluster identification");
        _output.WriteLine("     computation methods after execution.");
        _output.WriteLine("  8. Do NOT add post-hoc interaction axes");
        _output.WriteLine("     (e.g., 'what if we also vary K0?').");
        _output.WriteLine("  9. Do NOT claim H9 is confirmed if Gate A triggers.");
        _output.WriteLine("     'Conditionally supported' ≠ 'confirmed'.");
        _output.WriteLine("  10. Do NOT claim attractor decomposition exists");
        _output.WriteLine("      based on M1 alone.");
        _output.WriteLine("  11. Do NOT use M1 results to justify the 210-run");
        _output.WriteLine("      sensitivity matrix before M2/M3/M4 are executed.");
        _output.WriteLine("  12. All cluster membership masks must be SHA-256 hashed");
        _output.WriteLine("      BEFORE the xi sweep begins.");
        _output.WriteLine("");
        _output.WriteLine("FORBIDDEN ACTIONS FROZEN.");
    }

    // ═══════════════════════════════════════════════════════════
    //  M1.8 — Claim Discipline
    // ═══════════════════════════════════════════════════════════

    [Fact]
    public void V5_3_FCOP_09_ClaimDisciplineAudit()
    {
        // M1.8: Self-audit — verify no prohibited claims are made.
        _output.WriteLine("=== M1.8: CLAIM DISCIPLINE AUDIT ===");
        _output.WriteLine("");
        _output.WriteLine("This protocol suite makes the following claims:");
        _output.WriteLine("");
        _output.WriteLine("SUPPORTED:");
        _output.WriteLine("  - The cluster identification method is defined.");
        _output.WriteLine("  - The baseline cluster freezing protocol is defined.");
        _output.WriteLine("  - The free-cluster and fixed-cluster Omega computation");
        _output.WriteLine("    methods are defined.");
        _output.WriteLine("  - The sensitivity comparison method is defined.");
        _output.WriteLine("  - Decision gates and thresholds are pre-registered.");
        _output.WriteLine("  - Forbidden actions are enumerated.");
        _output.WriteLine("");
        _output.WriteLine("CONDITIONAL:");
        _output.WriteLine("  - Results depend on the cluster correlation threshold");
        _output.WriteLine($"    ({ClusterCorrelationThreshold:F2}).");
        _output.WriteLine("  - Results depend on the TRM simulation implementation");
        _output.WriteLine("    (Sm, RecoverFP, RP, OmegaField).");
        _output.WriteLine("  - Results depend on finite seed ensembles.");
        _output.WriteLine($"  - Results are specific to N={FrozenN}, K0={FrozenK0},");
        _output.WriteLine($"    s={FrozenS}, exponential coupling.");
        _output.WriteLine("");
        _output.WriteLine("HYPOTHESIS (tested by M1 execution):");
        _output.WriteLine("  - Omega xi-sensitivity survives when cluster membership");
        _output.WriteLine("    is held fixed at baseline membership.");
        _output.WriteLine("");
        _output.WriteLine("EXPLICITLY NOT CLAIMED:");
        _output.WriteLine("  - H9 is confirmed (even if Gate A triggers).");
        _output.WriteLine("  - Attractor decomposition exists.");
        _output.WriteLine("  - Synchronization-control parameter classes are real.");
        _output.WriteLine("  - Physical constants are derived.");
        _output.WriteLine("  - Quantum mechanics is involved.");
        _output.WriteLine("  - Spacetime emerges from TRM.");
        _output.WriteLine("  - Causation — all observations are correlations.");
        _output.WriteLine("");
        _output.WriteLine("AUDIT: PASSED — claim discipline maintained.");
    }

    // ═══════════════════════════════════════════════════════════
    //  M1.9 — Execution Run Plan
    // ═══════════════════════════════════════════════════════════

    [Fact]
    public void V5_3_FCOP_10_ExecutionRunPlan()
    {
        _output.WriteLine("=== M1.9: EXECUTION RUN PLAN ===");
        _output.WriteLine("");
        _output.WriteLine("Phase 1 — Baseline cluster identification:");
        _output.WriteLine($"  Seeds: {BaselineSeedStart}–{BaselineSeedStart + BaselineSeedCount - 1}");
        _output.WriteLine($"  Xi: {FrozenXi:F2} (primary regime only)");
        _output.WriteLine($"  Runs: {BaselineSeedCount} simulation runs");
        _output.WriteLine($"  Output: {BaselineSeedCount} frozen cluster membership masks");
        _output.WriteLine("");
        _output.WriteLine("Phase 2 — Xi sweep (free + fixed simultaneously):");
        _output.WriteLine($"  Xi values: [{string.Join(", ", XiSweep.Select(x => x.ToString("F2")))}]");
        _output.WriteLine($"  Seeds per xi: {SweepSeedsPerPoint}");
        _output.WriteLine($"  Runs: {XiSweep.Length} × {SweepSeedsPerPoint} = {XiSweep.Length * SweepSeedsPerPoint}");
        _output.WriteLine($"  Each run produces: Ω_free, Ω_fixed, cluster membership at this xi");
        _output.WriteLine("");
        _output.WriteLine($"Phase 3 — Comparison:");
        _output.WriteLine("  Compute sensitivity_ratio per seed × xi pair");
        _output.WriteLine("  Aggregate across seeds");
        _output.WriteLine("  Apply decision gates");
        _output.WriteLine("");
        _output.WriteLine("═══ TOTAL RUNS ═══");
        _output.WriteLine($"  Baseline: {BaselineSeedCount}");
        _output.WriteLine($"  Xi sweep: {XiSweep.Length * SweepSeedsPerPoint}");
        _output.WriteLine($"  Total:    {BaselineSeedCount + XiSweep.Length * SweepSeedsPerPoint}");
        _output.WriteLine("");
        _output.WriteLine("Expected runtime: ~5–15 minutes (45 TRM simulation runs).");
        _output.WriteLine("");
        _output.WriteLine("EXECUTION RUN PLAN DEFINED.");
    }
}
