using Xunit;
using Xunit.Abstractions;

namespace TRM.Tests.V5_4;

/// <summary>
/// V5.4 State-Space Geometry Protocol (SSGP):
///
/// Defines the experimental design for studying the intrinsic geometry
/// of the RecoverFP branch state space. V5.3 established that two Omega
/// outcome families exist connected by a narrow bridge. V5.4 determines
/// the intrinsic dimensionality and geometric structure of the underlying
/// coupling/distance state space.
///
/// Primary objects: full K and d matrices (not scalar summaries).
/// M14 signature treated as comparison control only.
///
/// CLAIM DISCIPLINE:
///   SUPPORTED:       Protocol definitions and methods.
///   CONDITIONAL:     Results depend on N, seeds, embedding parameters.
///   HYPOTHESIS:      Branch geometry may be low-dimensional.
///   NOT CLAIMED:     Physical interpretation, attractor decomposition,
///                    time, space, length, c, relativity, quantum.
///
/// PROTOCOL-DEFINITION only. No execution.
/// Execution deferred to: V5_4_StateSpaceGeometryExecution_Tests.cs
/// </summary>
[Trait("Category", "V5_4")]
[Trait("Category", "V5_4_SSGP")]
public class V5_4_StateSpaceGeometryProtocol_Tests
{
    private readonly ITestOutputHelper _output;

    // ── Frozen regime (V4.1 baseline, same as V5.3) ──
    internal const double FrozenXi = 1.75;
    internal const double FrozenK0 = 1.2;
    internal const double FrozenS = 0.10;
    internal const int FrozenSt = 300;
    internal const double FrozenREps = 1e-6;

    // ── N values for cross-N validation ──
    internal static readonly int[] NValues = { 67, 69, 72 };

    // ── Seed ensemble ──
    internal const int SeedsPerN = 100;
    internal const int SeedStart = 0;

    // ── State space dimensions (flattened upper triangle) ──
    // N=67: 67×66/2 = 2211 per matrix, 4422 combined

    // ── Dimensionality thresholds (pre-registered) ──
    internal const int LowDimThreshold = 5;
    internal const int HighDimThreshold = 20;

    // ── V5.3 reference ──
    internal const double V53_BranchThreshold = 1.783; // from M7/M8
    internal const double V53_SignatureAccuracy = 0.86; // M14 minimum accuracy

    // ── Embedding parameters ──
    internal static readonly int[] KnnValues = { 3, 5, 10, 30 };
    internal const double UmapMinDist = 0.1;
    internal const int UmapNeighbors = 15;

    public V5_4_StateSpaceGeometryProtocol_Tests(ITestOutputHelper o) { _output = o; }

    // ═══════════════════════════════════════════════════════════
    //  SSGP.0 — V5.3 Context
    // ═══════════════════════════════════════════════════════════

    [Fact]
    public void V5_4_SSGP_01_V53ContextLoaded()
    {
        _output.WriteLine("=== SSGP.0: V5.3 CONTEXT LOADED ===");
        _output.WriteLine("");
        _output.WriteLine("V5.3 established (frozen):");
        _output.WriteLine("  1. Two Omega outcome families: low≈1.1, high≈2.0-2.7");
        _output.WriteLine("  2. Finite-N threshold at N≈66");
        _output.WriteLine("  3. Coupling/distance separates before Omega");
        _output.WriteLine("  4. M14 LDA signature: 86-100% branch classification");
        _output.WriteLine("  5. Signature disruption → 9-42× branch flip probability");
        _output.WriteLine("  6. Weak bridge: silhouette 0.32-0.47");
        _output.WriteLine("");
        _output.WriteLine("V5.4 question:");
        _output.WriteLine("  Is the M14 signature the actual branch geometry,");
        _output.WriteLine("  or a compressed projection of a deeper state space?");
        _output.WriteLine("");
        _output.WriteLine("V5.3 CONTEXT LOADED — IMMUTABLE.");
    }

    // ═══════════════════════════════════════════════════════════
    //  SSGP.1 — State Representation
    // ═══════════════════════════════════════════════════════════

    [Fact]
    public void V5_4_SSGP_02_StateRepresentationDefined()
    {
        _output.WriteLine("=== SSGP.1: STATE REPRESENTATION ===");
        _output.WriteLine("");
        _output.WriteLine("Three state representations defined:");
        _output.WriteLine("");
        _output.WriteLine("S1 — Flattened K (coupling matrix):");
        _output.WriteLine("  For N nodes, extract upper triangle of K[i,j] for i<j.");
        _output.WriteLine($"  N=67: {67 * 66 / 2} dimensions.");
        _output.WriteLine($"  N=69: {69 * 68 / 2} dimensions.");
        _output.WriteLine($"  N=72: {72 * 71 / 2} dimensions.");
        _output.WriteLine("  K is the Epoch 5 coupling matrix after full RecoverFP.");
        _output.WriteLine("");
        _output.WriteLine("S2 — Flattened d (distance matrix):");
        _output.WriteLine("  For N nodes, extract upper triangle of d[i,j] for i<j.");
        _output.WriteLine("  Same dimensionality as S1.");
        _output.WriteLine("  d is the Epoch 5 emergent distance matrix.");
        _output.WriteLine("");
        _output.WriteLine("S3 — Combined [K | d] (concatenated):");
        _output.WriteLine("  Concatenate S1 and S2 into a single state vector.");
        _output.WriteLine($"  N=67: {67 * 66} dimensions total.");
        _output.WriteLine("");
        _output.WriteLine("All representations exclude diagonal entries (K[i,i]=0, d[i,i]=0).");
        _output.WriteLine("");
        _output.WriteLine("STATE REPRESENTATION DEFINED.");
    }

    // ═══════════════════════════════════════════════════════════
    //  SSGP.2 — Dimensionality Metrics
    // ═══════════════════════════════════════════════════════════

    [Fact]
    public void V5_4_SSGP_03_DimensionalityMetricsDefined()
    {
        _output.WriteLine("=== SSGP.2: DIMENSIONALITY METRICS ===");
        _output.WriteLine("");
        _output.WriteLine("Primary metrics (computed from PCA on S3 [K|d]):");
        _output.WriteLine("");
        _output.WriteLine("M1 — Participation Ratio:");
        _output.WriteLine("  PR = (Σ λ_i)² / Σ λ_i²");
        _output.WriteLine("  Interpretation: effective number of dimensions.");
        _output.WriteLine($"  PR ≤ {LowDimThreshold} → low-dimensional.");
        _output.WriteLine($"  PR > {HighDimThreshold} → high-dimensional.");
        _output.WriteLine("");
        _output.WriteLine("M2 — Effective Rank (90% variance):");
        _output.WriteLine("  r_eff = min k s.t. Σ_{i=1}^k λ_i / Σ λ_i ≥ 0.90");
        _output.WriteLine("  Interpretation: dimensions needed to capture 90% variance.");
        _output.WriteLine("");
        _output.WriteLine("M3 — Cumulative Variance Curve:");
        _output.WriteLine("  Report variance explained by first 1, 2, 3, 5, 10, 20, 50 PCs.");
        _output.WriteLine("");
        _output.WriteLine("Secondary metrics (local):");
        _output.WriteLine("");
        _output.WriteLine("M4 — Local Dimension (MLE via k-NN):");
        _output.WriteLine("  For each point, estimate local dimension from");
        _output.WriteLine("  k-NN distance distribution (k=5, 10, 30).");
        _output.WriteLine("  Report mean local dimension for low-branch and high-branch.");
        _output.WriteLine("");
        _output.WriteLine("DIMENSIONALITY METRICS DEFINED.");
    }

    // ═══════════════════════════════════════════════════════════
    //  SSGP.3 — Embedding Methods
    // ═══════════════════════════════════════════════════════════

    [Fact]
    public void V5_4_SSGP_04_EmbeddingMethodsDefined()
    {
        _output.WriteLine("=== SSGP.3: EMBEDDING METHODS ===");
        _output.WriteLine("");
        _output.WriteLine("Primary embedding: PCA on S3 [K|d]");
        _output.WriteLine("  - Linear, interpretable, preserves global structure.");
        _output.WriteLine("  - First 10 PCs saved for downstream analysis.");
        _output.WriteLine("");
        _output.WriteLine("Secondary embeddings (if computationally feasible):");
        _output.WriteLine("");
        _output.WriteLine("E2 — Diffusion Map:");
        _output.WriteLine("  - Captures nonlinear manifold structure.");
        _output.WriteLine("  - k-NN graph with k=10, Gaussian kernel.");
        _output.WriteLine("  - First 3 diffusion coordinates.");
        _output.WriteLine("");
        _output.WriteLine("E3 — Spectral Embedding:");
        _output.WriteLine("  - Normalized graph Laplacian of k-NN graph (k=10).");
        _output.WriteLine("  - First 3 eigenvectors after trivial first.");
        _output.WriteLine("");
        _output.WriteLine("All embeddings:");
        _output.WriteLine("  - Colored by branch label (low=blue, high=red).");
        _output.WriteLine("  - Colored by Omega value (continuous colormap).");
        _output.WriteLine("  - Cross-N comparison: do embeddings align?");
        _output.WriteLine("");
        _output.WriteLine("EMBEDDING METHODS DEFINED.");
    }

    // ═══════════════════════════════════════════════════════════
    //  SSGP.4 — Bridge Metrics
    // ═══════════════════════════════════════════════════════════

    [Fact]
    public void V5_4_SSGP_05_BridgeMetricsDefined()
    {
        _output.WriteLine("=== SSGP.4: BRIDGE METRICS ===");
        _output.WriteLine("");
        _output.WriteLine("B1 — Cross-Branch k-NN Connectivity:");
        _output.WriteLine("  Fraction of points whose k-NN (k=3,5,10) includes");
        _output.WriteLine("  at least one opposite-branch point.");
        _output.WriteLine("  Interpretation:");
        _output.WriteLine("    0% → fully disconnected basins.");
        _output.WriteLine("    <20% → narrow bridge.");
        _output.WriteLine("    >50% → overlapping distributions.");
        _output.WriteLine("");
        _output.WriteLine("B2 — Bridge Point Fraction:");
        _output.WriteLine("  Points with ≥40% opposite-branch nearest neighbors (k=5).");
        _output.WriteLine("  These are the 'bridge points' connecting the two basins.");
        _output.WriteLine("");
        _output.WriteLine("B3 — Shortest Cross-Branch Path:");
        _output.WriteLine("  Dijkstra shortest path between branch centroids");
        _output.WriteLine("  in the k-NN graph (k=5).");
        _output.WriteLine("  Report: path length, max edge length, bottleneck edge.");
        _output.WriteLine("");
        _output.WriteLine("B4 — Cross-Branch Distance Spectrum:");
        _output.WriteLine("  Min, mean, max cross-branch pairwise distance.");
        _output.WriteLine("  Compare to intra-branch mean distance.");
        _output.WriteLine("  Cross/Intra ratio > 2 → well-separated.");
        _output.WriteLine("");
        _output.WriteLine("BRIDGE METRICS DEFINED.");
    }

    // ═══════════════════════════════════════════════════════════
    //  SSGP.5 — M14 Signature as Control
    // ═══════════════════════════════════════════════════════════

    [Fact]
    public void V5_4_SSGP_06_M14ControlDefined()
    {
        _output.WriteLine("=== SSGP.5: M14 SIGNATURE AS CONTROL ===");
        _output.WriteLine("");
        _output.WriteLine("The M14 LDA signature is used ONLY as a comparison control.");
        _output.WriteLine("It is NOT the primary object of study in V5.4.");
        _output.WriteLine("");
        _output.WriteLine("Control analyses:");
        _output.WriteLine("  1. Correlate M14 LDA score with first 5 PCA coordinates.");
        _output.WriteLine("     Report R² and top-correlated PC.");
        _output.WriteLine("");
        _output.WriteLine("  2. M14 features regressed against PCA coordinates:");
        _output.WriteLine("     For each M14 feature (d_mean, d_std, K_mean, K_std,");
        _output.WriteLine("     λ₁(K), λ₁(d), topK), report variance explained by");
        _output.WriteLine("     first 5 PCs (R²).");
        _output.WriteLine("");
        _output.WriteLine("  3. Classification accuracy comparison:");
        _output.WriteLine("     M14 LDA vs LDA on first k PCs (k=1,2,3,5,10,20).");
        _output.WriteLine("     If k PCs achieve M14-level accuracy, the M14");
        _output.WriteLine("     signature is spanned by the first k dimensions.");
        _output.WriteLine("");
        _output.WriteLine("  4. M14-trained LDA applied to PCA embedding:");
        _output.WriteLine("     Test whether the M14 classifier trained on scalar");
        _output.WriteLine("     features generalizes to full state-space neighbors.");
        _output.WriteLine("");
        _output.WriteLine("M14 CONTROL DEFINED.");
    }

    // ═══════════════════════════════════════════════════════════
    //  SSGP.6 — Decision Gates
    // ═══════════════════════════════════════════════════════════

    [Fact]
    public void V5_4_SSGP_07_DecisionGatesDefined()
    {
        _output.WriteLine("=== SSGP.6: DECISION GATES ===");
        _output.WriteLine("");
        _output.WriteLine("Pre-registered thresholds (FROZEN):");
        _output.WriteLine($"  Low-dimensional:  PR ≤ {LowDimThreshold}");
        _output.WriteLine($"  High-dimensional: PR > {HighDimThreshold}");
        _output.WriteLine("");
        _output.WriteLine("═══ GATE A: LOW-DIMENSIONAL GEOMETRY ═══");
        _output.WriteLine("");
        _output.WriteLine("  Condition:");
        _output.WriteLine($"    PR ≤ {LowDimThreshold} (participation ratio)");
        _output.WriteLine("    AND branches visibly separated in PCA embedding");
        _output.WriteLine("    AND bridge fraction < 30%");
        _output.WriteLine("    AND stable across N=67,69,72");
        _output.WriteLine("");
        _output.WriteLine("  Interpretation:");
        _output.WriteLine("    Branch geometry is governed by ≤5 latent coordinates.");
        _output.WriteLine("    The M14 features likely span the relevant manifold.");
        _output.WriteLine("    V5.3 findings are structurally validated.");
        _output.WriteLine("");
        _output.WriteLine("═══ GATE B: MODERATE-DIMENSIONAL ═══");
        _output.WriteLine("");
        _output.WriteLine("  Condition:");
        _output.WriteLine($"    {LowDimThreshold} < PR ≤ {HighDimThreshold}");
        _output.WriteLine("    AND branches separated in PCA embedding");
        _output.WriteLine("");
        _output.WriteLine("  Interpretation:");
        _output.WriteLine("    Branch geometry requires 6-20 dimensions.");
        _output.WriteLine("    M14 features capture most but not all structure.");
        _output.WriteLine("    Some information is lost in scalar summaries.");
        _output.WriteLine("");
        _output.WriteLine("═══ GATE C: HIGH-DIMENSIONAL ═══");
        _output.WriteLine("");
        _output.WriteLine("  Condition:");
        _output.WriteLine($"    PR > {HighDimThreshold}");
        _output.WriteLine("    OR effective rank > 20");
        _output.WriteLine("");
        _output.WriteLine("  Interpretation:");
        _output.WriteLine("    Branch structure is distributed across many dimensions.");
        _output.WriteLine("    M14 signature is a compressed projection.");
        _output.WriteLine("    V5.3 scalar features miss substantial structure.");
        _output.WriteLine("");
        _output.WriteLine("═══ GATE D: GEOMETRY UNSTABLE ═══");
        _output.WriteLine("");
        _output.WriteLine("  Condition:");
        _output.WriteLine("    PR varies by >50% across N=67,69,72");
        _output.WriteLine("    OR embedding topology differs qualitatively across N");
        _output.WriteLine("    OR bridge fraction changes category across N");
        _output.WriteLine("");
        _output.WriteLine("  Interpretation:");
        _output.WriteLine("    Branch geometry is N-dependent.");
        _output.WriteLine("    The finite-N threshold changes state-space topology.");
        _output.WriteLine("    Cross-N generalization of V5.3 findings is limited.");
        _output.WriteLine("");
        _output.WriteLine("DECISION GATES FROZEN.");
    }

    // ═══════════════════════════════════════════════════════════
    //  SSGP.7 — Execution Plan
    // ═══════════════════════════════════════════════════════════

    [Fact]
    public void V5_4_SSGP_08_ExecutionPlanDefined()
    {
        _output.WriteLine("=== SSGP.7: EXECUTION PLAN ===");
        _output.WriteLine("");
        _output.WriteLine("Phase 1 — State Extraction:");
        _output.WriteLine($"  For each N in [{string.Join(", ", NValues)}]:");
        _output.WriteLine($"    For each seed in [{SeedStart}..{SeedStart + SeedsPerN - 1}]:");
        _output.WriteLine("      Run full RecoverFP (5 epochs + 1 final Sm).");
        _output.WriteLine("      Extract Epoch 5 K matrix and d matrix.");
        _output.WriteLine("      Flatten upper triangle → state vectors S1, S2, S3.");
        _output.WriteLine($"  Total Sm calls: {NValues.Length} × {SeedsPerN} × 6 = {NValues.Length * SeedsPerN * 6}");
        _output.WriteLine("");
        _output.WriteLine("Phase 2 — Dimensionality Analysis:");
        _output.WriteLine("  PCA on S3 [K|d] for each N.");
        _output.WriteLine("  Compute PR, effective rank, cumulative variance.");
        _output.WriteLine("  Local dimension estimation via k-NN MLE.");
        _output.WriteLine("");
        _output.WriteLine("Phase 3 — Embedding:");
        _output.WriteLine("  PCA to 2D and 3D, colored by branch and Omega.");
        _output.WriteLine("  Diffusion map / spectral embedding if feasible.");
        _output.WriteLine("");
        _output.WriteLine("Phase 4 — Bridge Characterization:");
        _output.WriteLine("  k-NN graphs (k=3,5,10,30).");
        _output.WriteLine("  Cross-branch connectivity, bridge points.");
        _output.WriteLine("  Shortest cross-branch path.");
        _output.WriteLine("");
        _output.WriteLine("Phase 5 — M14 Control:");
        _output.WriteLine("  Correlation: M14 score vs PCA coordinates.");
        _output.WriteLine("  Classifier accuracy vs PCA dimension.");
        _output.WriteLine("  M14 feature variance explained by PCs.");
        _output.WriteLine("");
        _output.WriteLine($"Expected runtime: ~{NValues.Length * SeedsPerN * 6 / 300:F0}s (simulation) + ~10s (PCA/embedding).");
        _output.WriteLine("");
        _output.WriteLine("EXECUTION PLAN DEFINED.");
    }

    // ═══════════════════════════════════════════════════════════
    //  SSGP.8 — Forbidden Actions
    // ═══════════════════════════════════════════════════════════

    [Fact]
    public void V5_4_SSGP_09_ForbiddenActionsDefined()
    {
        _output.WriteLine("=== SSGP.8: FORBIDDEN ACTIONS ===");
        _output.WriteLine("");
        _output.WriteLine("  1. Do NOT change the state representation after execution.");
        _output.WriteLine("  2. Do NOT change dimensionality thresholds after execution.");
        _output.WriteLine("  3. Do NOT change embedding parameters after execution.");
        _output.WriteLine("  4. Do NOT reselect seeds.");
        _output.WriteLine("  5. Do NOT change N values.");
        _output.WriteLine("  6. Do NOT change the branch threshold.");
        _output.WriteLine("  7. Do NOT add post-hoc embeddings after seeing results.");
        _output.WriteLine("  8. Do NOT claim physical interpretation of geometry.");
        _output.WriteLine("  9. Do NOT claim attractor decomposition.");
        _output.WriteLine("  10. Do NOT claim M14 signature is the geometry unless");
        _output.WriteLine("      Gate A is reached.");
        _output.WriteLine("  11. Do NOT modify V5.3 conclusions.");
        _output.WriteLine("");
        _output.WriteLine("FORBIDDEN ACTIONS FROZEN.");
    }

    // ═══════════════════════════════════════════════════════════
    //  SSGP.9 — Claim Discipline
    // ═══════════════════════════════════════════════════════════

    [Fact]
    public void V5_4_SSGP_10_ClaimDisciplineAudit()
    {
        _output.WriteLine("=== SSGP.9: CLAIM DISCIPLINE AUDIT ===");
        _output.WriteLine("");
        _output.WriteLine("SUPPORTED:");
        _output.WriteLine("  - Three state representations are structurally defined.");
        _output.WriteLine("  - Four dimensionality metrics are defined.");
        _output.WriteLine("  - Three embedding methods are defined.");
        _output.WriteLine("  - Four bridge metrics are defined.");
        _output.WriteLine("  - M14 control analyses are defined.");
        _output.WriteLine("  - Decision gates and thresholds are pre-registered.");
        _output.WriteLine("  - V5.3 context is frozen and immutable.");
        _output.WriteLine("");
        _output.WriteLine("CONDITIONAL:");
        _output.WriteLine("  - Results depend on state representation choice.");
        _output.WriteLine("  - Results depend on embedding method and parameters.");
        _output.WriteLine($"  - Results depend on N values [{string.Join(", ", NValues)}].");
        _output.WriteLine($"  - Results depend on seed ensemble ({SeedsPerN} seeds/N).");
        _output.WriteLine($"  - Results specific to V4.1 regime (xi={FrozenXi}, K0={FrozenK0}, s={FrozenS}).");
        _output.WriteLine("");
        _output.WriteLine("HYPOTHESIS (tested by V5.4 execution):");
        _output.WriteLine("  - Branch state space has intrinsic dimensionality ≤ 5.");
        _output.WriteLine("  - M14 signature corresponds to principal coordinates.");
        _output.WriteLine("  - Bridge is a narrow topological bottleneck.");
        _output.WriteLine("  - Geometry is stable across N=67,69,72.");
        _output.WriteLine("");
        _output.WriteLine("EXPLICITLY NOT CLAIMED:");
        _output.WriteLine("  - Physical phase transition");
        _output.WriteLine("  - Attractor decomposition");
        _output.WriteLine("  - Physical time, space, length, or c");
        _output.WriteLine("  - Relativity, quantum mechanics, cosmology");
        _output.WriteLine("  - H9-H12 confirmation");
        _output.WriteLine("  - M14 signature IS the geometry (unless Gate A)");
        _output.WriteLine("  - Results generalize beyond tested parameters");
        _output.WriteLine("");
        _output.WriteLine("AUDIT: PASSED.");
    }
}
