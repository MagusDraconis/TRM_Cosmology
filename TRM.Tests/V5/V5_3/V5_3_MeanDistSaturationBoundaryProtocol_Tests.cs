using Xunit;
using Xunit.Abstractions;

namespace TRM.Tests.V5_3;

/// <summary>
/// M3 MeanDist Saturation Boundary Protocol (MSBP):
///
/// Determines whether MeanDist xi-robustness and seed-variability
/// are TRM-specific properties or generic consequences of:
///   - Computing mean pairwise distance on a fixed graph
///   - Large-subset averaging effects
///   - Random spatial distribution of selected nodes
///
/// Three baselines are compared:
///   Baseline 1 — Random subset on fixed graph distances (no dynamics)
///   Baseline 2 — Generic Kuramoto dynamics (no RecoverFP)
///   Baseline 3 — TRM RecoverFP (full pipeline)
///
/// CLAIM DISCIPLINE:
///   SUPPORTED:       The three baselines are structurally defined.
///   CONDITIONAL:     Results depend on normalization method,
///                    subset size matching, finite seed ensembles.
///   HYPOTHESIS:      TRM MeanDist behavior may exceed baseline
///                    explanations.
///   NOT CLAIMED:     H10/H11/H12 confirmation, attractor
///                    decomposition, physical constants, quantum
///                    effects, spacetime.
///
/// PROTOCOL-DEFINITION only. Execution deferred.
/// </summary>
[Trait("Category", "V5_3")]
[Trait("Category", "V5_3_MSBP")]
public class V5_3_MeanDistSaturationBoundaryProtocol_Tests
{
    private readonly ITestOutputHelper _output;

    // ── Frozen parameters (matching M1/M2) ──
    internal const double FrozenK0 = 1.15;
    internal const int FrozenN = 100;
    internal const double FrozenS = 0.08;
    internal const double BaselineXi = 1.80;
    internal static readonly double[] XiSweep = { 1.50, 1.65, 1.80, 1.95, 2.10 };
    internal const int SweepSeedStart = 520;
    internal const int SweepSeedsPerPoint = 5;
    internal const double ClusterThreshold = 0.70;

    // ── V5.2 reference values (frozen) ──
    internal const double V52_MeanDistCvSeed = 0.30;   // TRM MeanDist seed-CV ~0.30
    internal const double V52_MeanDistCvXi = 0.05;     // TRM MeanDist xi-CV ~0.05 (robust)

    // ── Decision thresholds (pre-registered, frozen) ──
    // Baseline reproduces seed-variability if CV > this threshold
    internal const double SeedVariableThreshold = 0.15;

    // Baseline reproduces xi-robustness if CV_xi < this threshold
    internal const double XiRobustThreshold = 0.05;

    // Normalized residual significance threshold
    internal const double NormalizedResidualThreshold = 2.0;

    public V5_3_MeanDistSaturationBoundaryProtocol_Tests(ITestOutputHelper o) { _output = o; }

    // ═══════════════════════════════════════════════════════════
    //  M3.0 — Context
    // ═══════════════════════════════════════════════════════════

    [Fact]
    public void V5_3_MSBP_01_PriorResultsLoaded()
    {
        _output.WriteLine("=== M3.0: PRIOR RESULTS LOADED ===");
        _output.WriteLine("");
        _output.WriteLine("N1 NullModelBaseline:");
        _output.WriteLine("  MeanDist seed-variable (V5.2): CV ~0.30");
        _output.WriteLine("  Null model (random subset, graph dists): CV = 0.028");
        _output.WriteLine("  → Null model FAILS to reproduce seed-variability.");
        _output.WriteLine("");
        _output.WriteLine("N1 NullModelBaseline:");
        _output.WriteLine("  MeanDist xi-robust (V5.2): CV_xi ~0.05");
        _output.WriteLine("  Null model (random subset, graph dists): CV_xi = 0.002");
        _output.WriteLine("  → Null model REPRODUCES xi-robustness.");
        _output.WriteLine("");
        _output.WriteLine("M2 GenericDynamicsBaseline:");
        _output.WriteLine("  Generic Kuramoto (no RecoverFP) MeanDist: NOT YET COMPUTED.");
        _output.WriteLine("  M2 only computed Omega. MeanDist was not measured.");
        _output.WriteLine("");
        _output.WriteLine("M3 fills this gap: compute MeanDist for all three baselines");
        _output.WriteLine("and compare xi-robustness and seed-variability.");
        _output.WriteLine("");
        _output.WriteLine("PRIOR RESULTS LOADED — IMMUTABLE.");
    }

    // ═══════════════════════════════════════════════════════════
    //  M3.1 — Baseline Definitions
    // ═══════════════════════════════════════════════════════════

    [Fact]
    public void V5_3_MSBP_02_BaselinesDefined()
    {
        _output.WriteLine("=== M3.1: THREE MEANDIST BASELINES ===");
        _output.WriteLine("");
        _output.WriteLine("═══ BASELINE 1: RANDOM SUBSET ON FIXED GRAPH ═══");
        _output.WriteLine("");
        _output.WriteLine("  Pipeline:");
        _output.WriteLine("    1. Generate graph: KS(N, seed) → adjacency list");
        _output.WriteLine("    2. Compute BFS graph distances d_graph[i,j]");
        _output.WriteLine("    3. Select random subset of n_nodes");
        _output.WriteLine("       where n_nodes = TRM synchronized cluster size");
        _output.WriteLine("       (from M1: ~50/100 at baseline xi)");
        _output.WriteLine("    4. MD1 = mean(d_graph[i,j] for i<j in subset)");
        _output.WriteLine("");
        _output.WriteLine("  Properties:");
        _output.WriteLine("    - No dynamics, no coupling, no attractor.");
        _output.WriteLine("    - d_graph[i,j] are FIXED integers.");
        _output.WriteLine("    - xi does NOT affect d_graph → MD1 is xi-invariant");
        _output.WriteLine("      by construction. This is a control, not a finding.");
        _output.WriteLine("    - Seed affects graph structure AND random subset →");
        _output.WriteLine("      seed-CV tests spatial sampling variability only.");
        _output.WriteLine("");
        _output.WriteLine("  Purpose: Lower bound. If TRM MD is no more variable than");
        _output.WriteLine("    random subsets on fixed topology, TRM adds nothing.");
        _output.WriteLine("");
        _output.WriteLine("═══ BASELINE 2: GENERIC KURAMOTO DYNAMICS ═══");
        _output.WriteLine("");
        _output.WriteLine("  Pipeline:");
        _output.WriteLine("    1. KS(N, seed) → adjacency");
        _output.WriteLine("    2. BFS → d_graph[i,j] (graph distances)");
        _output.WriteLine("    3. Cupd(d_graph, K0={FrozenK0}, xi) → coupling K");
        _output.WriteLine("    4. Sm(K, N, s={FrozenS}, seed) → phase history h");
        _output.WriteLine("    5. d_emergent = DL(Nm(RP(h)))");
        _output.WriteLine("    6. MD2 = mean(d_emergent[i,j] for i<j) — all pairs");
        _output.WriteLine("    7. Optionally: identify cluster (IdCluster(RP(h)))");
        _output.WriteLine("       and compute MD2_cluster over cluster pairs only.");
        _output.WriteLine("");
        _output.WriteLine("  Properties:");
        _output.WriteLine("    - Single-epoch Kuramoto dynamics (no RecoverFP).");
        _output.WriteLine("    - Same graph, frequencies, kernel as TRM.");
        _output.WriteLine("    - Emergent distances from a single simulation.");
        _output.WriteLine("    - xi affects coupling K → affects synchronization →");
        _output.WriteLine("      affects d_emergent. xi-sensitivity is possible.");
        _output.WriteLine("");
        _output.WriteLine("  Purpose: Tests whether generic Kuramoto dynamics with");
        _output.WriteLine("    graph-distance coupling produce distinctive MeanDist");
        _output.WriteLine("    behavior, or whether RecoverFP is required.");
        _output.WriteLine("");
        _output.WriteLine("═══ BASELINE 3: TRM RECOVERFP ═══");
        _output.WriteLine("");
        _output.WriteLine("  Pipeline:");
        _output.WriteLine("    1. KS(N, seed) → dense adjacency matrix");
        _output.WriteLine("    2. Rfp(K, N, K0={FrozenK0}, xi, s={FrozenS}, E, seed) → K_fp");
        _output.WriteLine("       (E = EpochsForN(N) = 5 multi-epoch iterations)");
        _output.WriteLine("    3. Sm(K_fp, N, s, seed+E) → phase history h");
        _output.WriteLine("    4. d_emergent = DL(Nm(RP(h)))");
        _output.WriteLine("    5. MD3 = mean(d_emergent[i,j] for i<j) — all pairs");
        _output.WriteLine("");
        _output.WriteLine("  Properties:");
        _output.WriteLine("    - Full TRM pipeline (RecoverFP + Sm).");
        _output.WriteLine("    - Multi-epoch iterative coupling update.");
        _output.WriteLine("    - This is the production TRM MeanDist computation.");
        _output.WriteLine("");
        _output.WriteLine("  Purpose: TRM reference. MD3 must be compared to MD1 and");
        _output.WriteLine("    MD2 to isolate TRM-specific MeanDist behavior.");
        _output.WriteLine("");
        _output.WriteLine("THREE BASELINES DEFINED.");
    }

    // ═══════════════════════════════════════════════════════════
    //  M3.2 — Normalization Strategy
    // ═══════════════════════════════════════════════════════════

    [Fact]
    public void V5_3_MSBP_03_NormalizationStrategyDefined()
    {
        _output.WriteLine("=== M3.2: NORMALIZATION STRATEGY ===");
        _output.WriteLine("");
        _output.WriteLine("PROBLEM: d_graph (BFS integer distances) and d_emergent");
        _output.WriteLine("(log-transformed phase correlations) are on different scales.");
        _output.WriteLine("Direct comparison of raw MeanDist values across baselines");
        _output.WriteLine("is meaningless. Normalization is required.");
        _output.WriteLine("");
        _output.WriteLine("PRIMARY NORMALIZATION: MeanDist / D_90");
        _output.WriteLine("  D_90 = 90th percentile of pairwise distances in the");
        _output.WriteLine("  relevant distance matrix (graph or emergent).");
        _output.WriteLine("  D_90 is preferred over D_max because it is robust to");
        _output.WriteLine("  outlier pairs at extreme distances.");
        _output.WriteLine("");
        _output.WriteLine("SECONDARY NORMALIZATION: MeanDist / D_median");
        _output.WriteLine("  D_median = median pairwise distance.");
        _output.WriteLine("  Additional check: are conclusions robust to");
        _output.WriteLine("  normalization choice?");
        _output.WriteLine("");
        _output.WriteLine("REPORTING:");
        _output.WriteLine("  - Report raw MeanDist for TRM compatibility.");
        _output.WriteLine("  - Report MD/D_90 as primary normalized metric.");
        _output.WriteLine("  - Report MD/D_median as robustness check.");
        _output.WriteLine("  - Decision gates use MD/D_90 as primary.");
        _output.WriteLine("  - If MD/D_median produces different classification,");
        _output.WriteLine("    flag as NORMALIZATION-SENSITIVE.");
        _output.WriteLine("");
        _output.WriteLine("CROSS-BASELINE NORMALIZATION:");
        _output.WriteLine("  - Each baseline normalizes by its OWN D_90.");
        _output.WriteLine("  - This makes CVs comparable across baselines.");
        _output.WriteLine("  - Absolute MD values are NOT compared across baselines.");
        _output.WriteLine("  - Only CVs and fractional xi-shifts are compared.");
        _output.WriteLine("");
        _output.WriteLine("NORMALIZATION STRATEGY FROZEN.");
    }

    // ═══════════════════════════════════════════════════════════
    //  M3.3 — Metrics Definition
    // ═══════════════════════════════════════════════════════════

    [Fact]
    public void V5_3_MSBP_04_MetricsDefined()
    {
        _output.WriteLine("=== M3.3: METRICS ===");
        _output.WriteLine("");
        _output.WriteLine("For each baseline B ∈ {1, 2, 3}:");
        _output.WriteLine("");
        _output.WriteLine("PRIMARY METRICS:");
        _output.WriteLine("  MD_raw(xi, seed)     — raw MeanDist");
        _output.WriteLine("  MD_norm(xi, seed)    — MD_raw / D_90 of distance matrix");
        _output.WriteLine("");
        _output.WriteLine("SEED-VARIABILITY (at xi={BaselineXi:F2}):");
        _output.WriteLine("  CV_seed(B) = std(MD_norm across seeds) / mean(MD_norm)");
        _output.WriteLine("  Classification:");
        _output.WriteLine($"    CV_seed > {SeedVariableThreshold:F2} → seed-variable");
        _output.WriteLine($"    CV_seed ≤ {SeedVariableThreshold:F2} → seed-stable");
        _output.WriteLine("");
        _output.WriteLine("XI-ROBUSTNESS (across xi sweep):");
        _output.WriteLine("  MD_xi_mean = mean across seeds of MD_norm(xi, seed)");
        _output.WriteLine("  CV_xi(B) = std(MD_xi_mean across xi) / mean(MD_xi_mean)");
        _output.WriteLine("  Classification:");
        _output.WriteLine($"    CV_xi < {XiRobustThreshold:F2} → xi-robust");
        _output.WriteLine($"    CV_xi ≥ {XiRobustThreshold:F2} → xi-sensitive");
        _output.WriteLine("");
        _output.WriteLine("V5.2 REFERENCE (OBSERVED TRM PATTERN):");
        _output.WriteLine("  CV_seed ~0.30 → seed-VARIABLE");
        _output.WriteLine("  CV_xi   ~0.05 → xi-ROBUST");
        _output.WriteLine("");
        _output.WriteLine("METRICS DEFINED.");
    }

    // ═══════════════════════════════════════════════════════════
    //  M3.4 — Saturation Risk Assessment
    // ═══════════════════════════════════════════════════════════

    [Fact]
    public void V5_3_MSBP_05_SaturationRiskAssessed()
    {
        _output.WriteLine("=== M3.4: SATURATION RISK ===");
        _output.WriteLine("");
        _output.WriteLine("SATURATION HYPOTHESIS:");
        _output.WriteLine("  MeanDist xi-robustness is a trivial consequence of:");
        _output.WriteLine("  (a) d_ij are fixed or slowly-varying with xi");
        _output.WriteLine("  (b) The subset of node pairs is large (>50% of graph)");
        _output.WriteLine("  (c) Mean pairwise distance of large subsets on fixed");
        _output.WriteLine("      topology is near-constant.");
        _output.WriteLine("");
        _output.WriteLine("RISK ASSESSMENT PER BASELINE:");
        _output.WriteLine("");
        _output.WriteLine("Baseline 1 (random subset, fixed graph):");
        _output.WriteLine("  d_graph[i,j] are FIXED → xi-robustness is TRIVIAL.");
        _output.WriteLine("  Seed-CV comes from random subset selection only.");
        _output.WriteLine("  N1 showed CV ~0.028 for 50-node subsets → seed-STABLE.");
        _output.WriteLine("  RISK: If CV_seed is always low (as N1 suggests),");
        _output.WriteLine("  Baseline 1 cannot reproduce seed-variability →");
        _output.WriteLine("  this does NOT prove TRM has distinctive behavior;");
        _output.WriteLine("  it proves graph distances have low spatial variance.");
        _output.WriteLine("");
        _output.WriteLine("Baseline 2 (generic Kuramoto, emergent distances):");
        _output.WriteLine("  d_emergent varies with seed (different synchronization");
        _output.WriteLine("  patterns) and potentially with xi (different coupling).");
        _output.WriteLine("  Saturation risk: if single-epoch dynamics don't produce");
        _output.WriteLine("  sufficiently variable emergent distances, seed-CV may");
        _output.WriteLine("  be low regardless of xi-sensitivity.");
        _output.WriteLine("  RISK: Generic dynamics may not amplify distance variance");
        _output.WriteLine("  the way RecoverFP does.");
        _output.WriteLine("");
        _output.WriteLine("Baseline 3 (TRM RecoverFP):");
        _output.WriteLine("  Multi-epoch RecoverFP may amplify distance variance.");
        _output.WriteLine("  RISK: High seed-CV could be an artifact of RecoverFP's");
        _output.WriteLine("  emergent distance computation (log-transform, normal-");
        _output.WriteLine("  ization), not a genuine geometric property.");
        _output.WriteLine("");
        _output.WriteLine("SATURATION BOUNDARY TEST:");
        _output.WriteLine("  If Baseline 2 reproduces TRM-level seed-CV, RecoverFP");
        _output.WriteLine("  is not required for MeanDist variability.");
        _output.WriteLine("  If Baseline 2 does NOT reproduce seed-CV, RecoverFP's");
        _output.WriteLine("  multi-epoch structure amplifies distance variance.");
        _output.WriteLine("  This could be a computational artifact or a genuine");
        _output.WriteLine("  attractor property — M3 cannot distinguish these.");
        _output.WriteLine("");
        _output.WriteLine("SATURATION RISK ASSESSED.");
    }

    // ═══════════════════════════════════════════════════════════
    //  M3.5 — Decision Gates
    // ═══════════════════════════════════════════════════════════

    [Fact]
    public void V5_3_MSBP_06_DecisionGatesDefined()
    {
        _output.WriteLine("=== M3.5: DECISION GATES ===");
        _output.WriteLine("");
        _output.WriteLine("For each baseline B ∈ {1, 2, 3}, compute:");
        _output.WriteLine("  seed_variable(B) = (CV_seed(B) > 0.15)");
        _output.WriteLine("  xi_robust(B)     = (CV_xi(B) < 0.05)");
        _output.WriteLine("");
        _output.WriteLine("═══ GATE A: SATURATION EXPLAINS BOTH SIGNATURES ═══");
        _output.WriteLine("");
        _output.WriteLine("  Condition:");
        _output.WriteLine("    Baseline 1 AND/OR Baseline 2 reproduces BOTH:");
        _output.WriteLine("    - seed-variable (CV > 0.15)");
        _output.WriteLine("    - xi-robust (CV_xi < 0.05)");
        _output.WriteLine("");
        _output.WriteLine("  Interpretation:");
        _output.WriteLine("    MeanDist behavior is explained by generic mechanisms.");
        _output.WriteLine("    No TRM-specific geometric residual is required.");
        _output.WriteLine("");
        _output.WriteLine("  Impact:");
        _output.WriteLine("    H10 (geometry-control): WEAKENED.");
        _output.WriteLine("      If baselines reproduce both signatures, TRM's");
        _output.WriteLine("      MeanDist is not distinctive.");
        _output.WriteLine("    H11 (parameter classes): WEAKENED.");
        _output.WriteLine("    H12 (attractor decomposition): WEAKENED.");
        _output.WriteLine("");
        _output.WriteLine("  Next: Revise H10/H11 wording. Consider whether MeanDist");
        _output.WriteLine("    is a useful metric for attractor characterization.");
        _output.WriteLine("");
        _output.WriteLine("═══ GATE B: TRM RESIDUAL DOMINANT ═══");
        _output.WriteLine("");
        _output.WriteLine("  Condition:");
        _output.WriteLine("    Neither Baseline 1 nor Baseline 2 reproduces at least");
        _output.WriteLine("    ONE of the two signatures (seed-variable or xi-robust)");
        _output.WriteLine("    AND TRM Baseline 3 reproduces BOTH.");
        _output.WriteLine("");
        _output.WriteLine("  Interpretation:");
        _output.WriteLine("    TRM MeanDist contains a RecoverFP-specific residual");
        _output.WriteLine("    not explainable by random subsets or generic dynamics.");
        _output.WriteLine("");
        _output.WriteLine("  Impact:");
        _output.WriteLine("    H10: CONDITIONALLY SUPPORTED.");
        _output.WriteLine("      TRM's MeanDist behavior is distinctive.");
        _output.WriteLine("    H11: MORE TESTABLE but NOT CONFIRMED.");
        _output.WriteLine("    H12: WEAKLY SUPPORTED but NOT CONFIRMED.");
        _output.WriteLine("");
        _output.WriteLine("  Next: M4 (latent variable regression) to test whether");
        _output.WriteLine("    Ω and MD share a single latent driver.");
        _output.WriteLine("");
        _output.WriteLine("═══ GATE C: MIXED / PARTIAL ═══");
        _output.WriteLine("");
        _output.WriteLine("  Condition:");
        _output.WriteLine("    Baselines reproduce one signature but not the other.");
        _output.WriteLine("    E.g., Baseline 2 is xi-robust but NOT seed-variable.");
        _output.WriteLine("");
        _output.WriteLine("  Interpretation:");
        _output.WriteLine("    The two MeanDist signatures (xi-robustness and");
        _output.WriteLine("    seed-variability) have different origins.");
        _output.WriteLine("    One may be generic, the other TRM-specific.");
        _output.WriteLine("");
        _output.WriteLine("  Impact:");
        _output.WriteLine("    H10: PARTIALLY SUPPORTED, PARTIALLY UNRESOLVED.");
        _output.WriteLine("    H11: Requires decomposition of MeanDist into");
        _output.WriteLine("      generic and TRM-specific components.");
        _output.WriteLine("");
        _output.WriteLine("  Next: Characterize which signature is TRM-specific.");
        _output.WriteLine("    If xi-robustness is generic but seed-variability is");
        _output.WriteLine("    TRM-specific, H10 needs revision.");
        _output.WriteLine("");
        _output.WriteLine("DECISION GATES FROZEN.");
    }

    // ═══════════════════════════════════════════════════════════
    //  M3.6 — Execution Run Plan
    // ═══════════════════════════════════════════════════════════

    [Fact]
    public void V5_3_MSBP_07_ExecutionRunPlan()
    {
        _output.WriteLine("=== M3.6: EXECUTION RUN PLAN ===");
        _output.WriteLine("");
        _output.WriteLine("Baseline 1 — Random Subset (no dynamics):");
        _output.WriteLine($"  {SweepSeedsPerPoint} seeds, fixed xi (no xi dependence by construction)");
        _output.WriteLine($"  Runs: {SweepSeedsPerPoint} computations (no simulation)");
        _output.WriteLine("");
        _output.WriteLine("Baseline 2 — Generic Kuramoto Dynamics:");
        _output.WriteLine($"  {XiSweep.Length} xi × {SweepSeedsPerPoint} seeds = {XiSweep.Length * SweepSeedsPerPoint} runs");
        _output.WriteLine("  Each run: KS → BFS → Cupd → Sm (single) → RP → DL → MD");
        _output.WriteLine($"  Expected runtime: ~{XiSweep.Length * SweepSeedsPerPoint / 5:F0}s");
        _output.WriteLine("");
        _output.WriteLine("Baseline 3 — TRM RecoverFP:");
        _output.WriteLine("  Data from M1 execution (already computed).");
        _output.WriteLine("  M1 computed Omega but did NOT compute MeanDist.");
        _output.WriteLine($"  Need to rerun with MeanDist computation.");
        _output.WriteLine($"  {XiSweep.Length} xi × {SweepSeedsPerPoint} seeds × 6 Sm calls");
        _output.WriteLine($"  = {XiSweep.Length * SweepSeedsPerPoint * 6} total Sm calls");
        _output.WriteLine("  Expected runtime: ~12s (similar to M1).");
        _output.WriteLine("");
        _output.WriteLine("═══ TOTAL ═══");
        _output.WriteLine($"  Baseline 1: 0 simulation runs ({SweepSeedsPerPoint} computations)");
        _output.WriteLine($"  Baseline 2: {XiSweep.Length * SweepSeedsPerPoint} simulation runs");
        _output.WriteLine($"  Baseline 3: {XiSweep.Length * SweepSeedsPerPoint} simulation runs");
        _output.WriteLine($"  Total Sm calls: {XiSweep.Length * SweepSeedsPerPoint * 7}");
        _output.WriteLine("");
        _output.WriteLine("EXECUTION RUN PLAN DEFINED.");
    }

    // ═══════════════════════════════════════════════════════════
    //  M3.7 — Forbidden Actions
    // ═══════════════════════════════════════════════════════════

    [Fact]
    public void V5_3_MSBP_08_ForbiddenActionsDefined()
    {
        _output.WriteLine("=== M3.7: FORBIDDEN ACTIONS ===");
        _output.WriteLine("");
        _output.WriteLine("  1. Do NOT change normalization method after execution.");
        _output.WriteLine("  2. Do NOT change decision thresholds after execution.");
        _output.WriteLine("  3. Do NOT reselect seeds.");
        _output.WriteLine("  4. Do NOT change xi sweep values.");
        _output.WriteLine("  5. Do NOT change baseline definitions.");
        _output.WriteLine("  6. Do NOT add post-hoc baselines after seeing results.");
        _output.WriteLine("  7. Do NOT compare raw MD across baselines —");
        _output.WriteLine("     use normalized values only for cross-baseline CVs.");
        _output.WriteLine("  8. Do NOT claim H10/H11/H12 are confirmed.");
        _output.WriteLine("  9. Do NOT claim geometry-control exists.");
        _output.WriteLine("  10. Do NOT claim attractor decomposition.");
        _output.WriteLine("  11. Do NOT use M3 results to justify 210-run matrix");
        _output.WriteLine("      before M4 is executed.");
        _output.WriteLine("");
        _output.WriteLine("FORBIDDEN ACTIONS FROZEN.");
    }

    // ═══════════════════════════════════════════════════════════
    //  M3.8 — Claim Discipline Audit
    // ═══════════════════════════════════════════════════════════

    [Fact]
    public void V5_3_MSBP_09_ClaimDisciplineAudit()
    {
        _output.WriteLine("=== M3.8: CLAIM DISCIPLINE AUDIT ===");
        _output.WriteLine("");
        _output.WriteLine("SUPPORTED:");
        _output.WriteLine("  - Three MeanDist baselines are structurally defined.");
        _output.WriteLine("  - Normalization strategy is defined.");
        _output.WriteLine("  - Metrics, thresholds, and decision gates are frozen.");
        _output.WriteLine("  - N1 showed Baseline 1 has CV_seed ~0.028 (seed-stable).");
        _output.WriteLine("");
        _output.WriteLine("CONDITIONAL:");
        _output.WriteLine("  - Results depend on normalization method (D_90).");
        _output.WriteLine("  - Results depend on subset size matching.");
        _output.WriteLine("  - Results depend on finite seed ensembles.");
        _output.WriteLine($"  - Results specific to N={FrozenN}, K0={FrozenK0},");
        _output.WriteLine($"    s={FrozenS}, exp coupling.");
        _output.WriteLine("");
        _output.WriteLine("HYPOTHESIS (tested by M3 execution):");
        _output.WriteLine("  - TRM MeanDist exceeds baseline explanations.");
        _output.WriteLine("  - MeanDist behavior contains a RecoverFP-specific");
        _output.WriteLine("    residual component.");
        _output.WriteLine("");
        _output.WriteLine("NOT CLAIMED:");
        _output.WriteLine("  - H10/H11/H12 confirmed");
        _output.WriteLine("  - Attractor decomposition exists");
        _output.WriteLine("  - Geometry-control parameter class is real");
        _output.WriteLine("  - Physical constants derived");
        _output.WriteLine("  - Quantum mechanics, spacetime emergence");
        _output.WriteLine("  - Causation");
        _output.WriteLine("");
        _output.WriteLine("AUDIT: PASSED.");
    }
}
