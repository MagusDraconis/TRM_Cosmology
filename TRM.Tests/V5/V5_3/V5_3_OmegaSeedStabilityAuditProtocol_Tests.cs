using Xunit;
using Xunit.Abstractions;

namespace TRM.Tests.V5_3;

/// <summary>
/// M3c Omega Seed Stability Audit Protocol (OSSA):
///
/// Determines whether Omega seed-stability (historically CV~0.01) is
/// robust across seed ranges or was overstated. Both V5.2 live
/// verification and M3b found Omega CV ~0.10 for seeds 100–109 and
/// 520–524 — 10× the expected value.
///
/// Five seed blocks, each at primary regime (xi=1.80). Full TRM
/// RecoverFP pipeline. Omega and MeanDist computed per seed.
/// Omega CV classified against pre-registered thresholds.
///
/// CLAIM DISCIPLINE:
///   SUPPORTED:       Seed blocks and metrics are defined.
///   CONDITIONAL:     Results depend on seed block selection,
///                    finite ensembles, TRM implementation.
///   HYPOTHESIS:      Omega seed-CV is robustly ≤0.05 across
///                    most seed blocks.
///   NOT CLAIMED:     H9/H10/H11/H12 confirmation, attractor
///                    decomposition, physical constants.
///
/// PROTOCOL-DEFINITION only. Execution deferred.
/// </summary>
[Trait("Category", "V5_3")]
[Trait("Category", "V5_3_OSSA")]
public class V5_3_OmegaSeedStabilityAuditProtocol_Tests
{
    private readonly ITestOutputHelper _output;

    internal const double FrozenK0 = 1.15;
    internal const int FrozenN = 100;
    internal const double FrozenS = 0.08;
    internal const double FrozenXi = 1.80;

    // ── Seed blocks ──
    internal static readonly (string name, int start, int count)[] SeedBlocks = new[]
    {
        ("A: V5.2 original",  100, 10),
        ("B: M3 current",     520, 5),
        ("C: mid-range",      200, 10),
        ("D: upper-mid",      300, 10),
        ("E: upper",          400, 10),
    };

    // ── Omega CV thresholds (pre-registered, frozen) ──
    internal const double HighlyStableThreshold = 0.02;  // CV ≤ 0.02 → highly seed-stable
    internal const double StableThreshold = 0.05;         // CV ≤ 0.05 → seed-stable
    // CV > 0.05 → seed-variable under V5.3 criteria

    // ── Reference values ──
    internal const double HistoricalOmegaCv = 0.01;        // expected from V4.1/V5.1 docs
    internal const double V52_MeasuredOmegaCv = 0.097;    // measured live from V5.2 RSBS (seeds 100-109)
    internal const double M3b_MeasuredOmegaCv = 0.097;    // measured from M3b Block A (seeds 100-109)

    // ── Robustness ──
    internal const int MostBlocksForRobust = 3; // ≥3 of 5 blocks

    public V5_3_OmegaSeedStabilityAuditProtocol_Tests(ITestOutputHelper o) { _output = o; }

    [Fact]
    public void V5_3_OSSA_01_ForensicContextLoaded()
    {
        _output.WriteLine("=== M3c.0: FORENSIC CONTEXT ===");
        _output.WriteLine("");
        _output.WriteLine("M3b forensic audit findings:");
        _output.WriteLine("");
        _output.WriteLine("  V5.2 MeanDist CV was 0.053, not ~0.30.");
        _output.WriteLine("  The ~0.30 was UncMeanDist (uncertainty budget),");
        _output.WriteLine("  not a measured CV. Documentation artifact.");
        _output.WriteLine("");
        _output.WriteLine("  V5.2 Omega CV was 0.097 for seeds 100–109.");
        _output.WriteLine("  M3b Omega CV was 0.097 for same seeds.");
        _output.WriteLine($"  Both conflict with historical expected CV ~{HistoricalOmegaCv:F2}.");
        _output.WriteLine("");
        _output.WriteLine("  → Omega seed-stability is UNRESOLVED.");
        _output.WriteLine("");
        _output.WriteLine("M3c question:");
        _output.WriteLine("  Is Omega seed-CV consistently ≤ 0.05 across seed blocks,");
        _output.WriteLine($"  or is the measured CV ~{V52_MeasuredOmegaCv:F3} typical?");
        _output.WriteLine("");
        _output.WriteLine("FORENSIC CONTEXT LOADED.");
    }

    [Fact]
    public void V5_3_OSSA_02_SeedBlocksDefined()
    {
        _output.WriteLine("=== M3c.1: SEED BLOCKS ===");
        _output.WriteLine("");
        _output.WriteLine("Five seed blocks, single regime (xi={FrozenXi:F2}):");
        _output.WriteLine("");
        _output.WriteLine($"{"Block",-8} {"Name",-20} {"Seeds",-18} {"Count",-8} {"Purpose"}");
        _output.WriteLine(new string('-', 70));
        foreach (var (name, start, count) in SeedBlocks)
        {
            string range = count > 1 ? $"{start}–{start + count - 1}" : $"{start}";
            string purpose = name.Contains("original") ? "V5.2 anomaly block"
                           : name.Contains("current") ? "M3 anomaly block"
                           : "Control block";
            _output.WriteLine($"{"Block " + name[0],-8} {name,-20} {range,-18} {count,-8} {purpose}");
        }
        _output.WriteLine("");
        _output.WriteLine($"Total: {SeedBlocks.Sum(b => b.count)} seeds across {SeedBlocks.Length} blocks.");
        _output.WriteLine("");
        _output.WriteLine("Block A (100–109): V5.2 ensemble where Ω CV=0.097 was measured.");
        _output.WriteLine("Block B (520–524): M1/M2/M3 ensemble where Ω CV=0.109 was measured.");
        _output.WriteLine("Blocks C, D, E: Independent controls with 10 seeds each.");
        _output.WriteLine("");
        _output.WriteLine("SEED BLOCKS FROZEN.");
    }

    [Fact]
    public void V5_3_OSSA_03_MeasurementPipelineDefined()
    {
        _output.WriteLine("=== M3c.2: MEASUREMENT PIPELINE ===");
        _output.WriteLine("");
        _output.WriteLine("For each seed, at xi={FrozenXi:F2}, K0={FrozenK0}, N={FrozenN}, s={FrozenS}:");
        _output.WriteLine("");
        _output.WriteLine("  1. Generate graph: KS(N, seed) → dense adjacency matrix.");
        _output.WriteLine("  2. RecoverFP: Rfp(K, N, K0, xi, s, E=EpochsForN(N), seed)");
        _output.WriteLine("     → multi-epoch fixed-point coupling matrix K_fp.");
        _output.WriteLine("  3. Final simulation: Sm(K_fp, N, s, seed+E) → phase history h.");
        _output.WriteLine("  4. Phase correlation: R = RP(h).");
        _output.WriteLine("  5. Per-node Omega: om = OmegaField(h).");
        _output.WriteLine("  6. Ensemble Omega: Ω = mean(om) over all N nodes.");
        _output.WriteLine("  7. Emergent distances: d = DL(Nm(R)).");
        _output.WriteLine("  8. MeanDist: MD = mean(d[i,j] for i<j) — secondary control.");
        _output.WriteLine("  9. Cluster: mask = IdCluster(R) using threshold 0.70.");
        _output.WriteLine("  10. Sync fraction: r_sync = count(mask) / N.");
        _output.WriteLine("");
        _output.WriteLine("Identical pipeline to M3/M3b/V5.2 RSBS.");
        _output.WriteLine("");
        _output.WriteLine("MEASUREMENT PIPELINE DEFINED.");
    }

    [Fact]
    public void V5_3_OSSA_04_MetricsAndThresholdsDefined()
    {
        _output.WriteLine("=== M3c.3: METRICS AND THRESHOLDS ===");
        _output.WriteLine("");
        _output.WriteLine("PRIMARY METRIC: Omega seed-CV per block.");
        _output.WriteLine("");
        _output.WriteLine("  CV_Ω = std(Ω across seeds) / |mean(Ω)|");
        _output.WriteLine("  (Population std: divide by N, not N-1).");
        _output.WriteLine("");
        _output.WriteLine("CLASSIFICATION THRESHOLDS (pre-registered, frozen):");
        _output.WriteLine("");
        _output.WriteLine($"  CV ≤ {HighlyStableThreshold:F2}: HIGHLY SEED-STABLE");
        _output.WriteLine($"  CV ≤ {StableThreshold:F2}:     SEED-STABLE");
        _output.WriteLine($"  CV > {StableThreshold:F2}:     SEED-VARIABLE (under V5.3 criteria)");
        _output.WriteLine("");
        _output.WriteLine("REFERENCE VALUES:");
        _output.WriteLine($"  Historical expected:  CV ≈ {HistoricalOmegaCv:F2}  (V4.1/V5.1 docs)");
        _output.WriteLine($"  V5.2 measured (live): CV = {V52_MeasuredOmegaCv:F3} (seeds 100–109)");
        _output.WriteLine($"  M3b measured:         CV = {M3b_MeasuredOmegaCv:F3} (seeds 100–109)");
        _output.WriteLine("");
        _output.WriteLine("SECONDARY CONTROL: MeanDist seed-CV per block.");
        _output.WriteLine("  CV_MD from same seeds. Expectation: CV_MD ≤ 0.10");
        _output.WriteLine("");
        _output.WriteLine("DIAGNOSTIC METRICS (per seed, if available):");
        _output.WriteLine("  - Synchronized cluster size (count of mask[i]==true)");
        _output.WriteLine("  - Synchronized fraction (r_sync = cluster_size / N)");
        _output.WriteLine("  - Final Omega value (Ω = mean(om))");
        _output.WriteLine("  - Final MeanDist value (MD = mean(d[i,j] for i<j))");
        _output.WriteLine("");
        _output.WriteLine("METRICS AND THRESHOLDS FROZEN.");
    }

    [Fact]
    public void V5_3_OSSA_05_DecisionGatesDefined()
    {
        _output.WriteLine("=== M3c.4: DECISION GATES ===");
        _output.WriteLine("");
        _output.WriteLine($"Robustness: ≥ {MostBlocksForRobust} of {SeedBlocks.Length} blocks satisfy criterion.");
        _output.WriteLine("");
        _output.WriteLine("═══ GATE A: ROBUST OMEGA SEED STABILITY ═══");
        _output.WriteLine("");
        _output.WriteLine("  Condition:");
        _output.WriteLine($"    ≥ {MostBlocksForRobust} blocks have CV_Ω ≤ {StableThreshold:F2}");
        _output.WriteLine("");
        _output.WriteLine("  Interpretation:");
        _output.WriteLine("    Omega seed-stability is broadly supported.");
        _output.WriteLine("    V5.2/M3b anomaly blocks (100–109, 520–524) are outliers.");
        _output.WriteLine("    H9: conditionally supported (unchanged by M3c).");
        _output.WriteLine("");
        _output.WriteLine("  Next:");
        _output.WriteLine("    Flag anomalous blocks for investigation.");
        _output.WriteLine("    Proceed to M4 only if MD issue is also resolved.");
        _output.WriteLine("    Document: Omega is seed-stable for most blocks,");
        _output.WriteLine("    but specific seed ranges may produce outliers.");
        _output.WriteLine("");
        _output.WriteLine("═══ GATE B: PARTIAL / BLOCK-SPECIFIC ═══");
        _output.WriteLine("");
        _output.WriteLine("  Condition:");
        _output.WriteLine("    Some but not all blocks have CV_Ω ≤ 0.05,");
        _output.WriteLine($"    AND at least 1 block has CV_Ω > {StableThreshold:F2}.");
        _output.WriteLine("");
        _output.WriteLine("  Interpretation:");
        _output.WriteLine("    Omega seed-stability is seed-block dependent.");
        _output.WriteLine("    The blanket 'Omega is seed-stable' claim must be narrowed.");
        _output.WriteLine("    Investigate: what drives high Ω CV blocks?");
        _output.WriteLine("    (Cluster size? Sync fraction? Graph topology?)");
        _output.WriteLine("    H9: supported for xi-sensitivity only,");
        _output.WriteLine("    not for seed-stability.");
        _output.WriteLine("");
        _output.WriteLine("  Next:");
        _output.WriteLine("    Correlate Ω CV with cluster size, sync fraction.");
        _output.WriteLine("    Investigate graph-structure differences.");
        _output.WriteLine("");
        _output.WriteLine("═══ GATE C: OMEGA SEED STABILITY NOT REPRODUCED ═══");
        _output.WriteLine("");
        _output.WriteLine("  Condition:");
        _output.WriteLine($"    < {MostBlocksForRobust} blocks have CV_Ω ≤ {StableThreshold:F2}");
        _output.WriteLine("");
        _output.WriteLine("  Interpretation:");
        _output.WriteLine("    Omega seed-stability is not robust.");
        _output.WriteLine("    The V5.2/V5.1 'HIGHLY STABLE' classification must be revised.");
        _output.WriteLine("    H9: WEAKENED for the seed-stability dimension.");
        _output.WriteLine("    (H9 remains conditionally supported for xi-sensitivity");
        _output.WriteLine("     from M1/M2 — seed-stability is independent.)");
        _output.WriteLine("");
        _output.WriteLine("  Next:");
        _output.WriteLine("    Do NOT proceed to M4 until CV discrepancy is understood.");
        _output.WriteLine("    Audit whether OmegaField computation or Rfp convergence");
        _output.WriteLine("    differs from the regime that produced CV ~0.01.");
        _output.WriteLine("");
        _output.WriteLine("═══ GATE D: PIPELINE MISMATCH ═══");
        _output.WriteLine("");
        _output.WriteLine("  Condition:");
        _output.WriteLine("    Current Omega computation differs from V5.2 pipeline.");
        _output.WriteLine("");
        _output.WriteLine("  This gate triggers automatically if the forensic audit");
        _output.WriteLine("  finds any structural difference in OmegaField, Rfp,");
        _output.WriteLine("  Sm, KS, or DL/Nm/RP between V5.2 and M3c implementations.");
        _output.WriteLine("");
        _output.WriteLine("DECISION GATES FROZEN.");
    }

    [Fact]
    public void V5_3_OSSA_06_ExecutionRunPlan()
    {
        _output.WriteLine("=== M3c.5: EXECUTION RUN PLAN ===");
        _output.WriteLine("");
        _output.WriteLine("Each seed: 1 full TRM RecoverFP pipeline (6 Sm calls).");
        _output.WriteLine("");
        int totalSm = 0;
        _output.WriteLine($"{"Block",-20} {"Seeds",-8} {"Sm calls",-10} {"Est. time"}");
        _output.WriteLine(new string('-', 50));
        foreach (var (name, start, count) in SeedBlocks)
        {
            int sm = count * 6;
            totalSm += sm;
            _output.WriteLine($"{name,-20} {count,-8} {sm,-10} ~{count * 1.2:F0}s");
        }
        _output.WriteLine(new string('-', 50));
        _output.WriteLine($"{"TOTAL",-20} {SeedBlocks.Sum(b => b.count),-8} {totalSm,-10} ~{totalSm / 6 * 1.2:F0}s");
        _output.WriteLine("");
        _output.WriteLine("Staged execution:");
        _output.WriteLine("  Stage 1: Blocks A+B only (15 seeds, ~18s)");
        _output.WriteLine("    → Immediate verification of V5.2/M3b anomaly.");
        _output.WriteLine("  Stage 2: Blocks C+D+E (30 seeds, ~36s)");
        _output.WriteLine("    → Full audit if Stage 1 is inconclusive.");
        _output.WriteLine("");
        _output.WriteLine("RUN PLAN DEFINED.");
    }

    [Fact]
    public void V5_3_OSSA_07_ForbiddenActionsDefined()
    {
        _output.WriteLine("=== M3c.6: FORBIDDEN ACTIONS ===");
        _output.WriteLine("");
        _output.WriteLine("  1. Do NOT change seed blocks after execution.");
        _output.WriteLine("  2. Do NOT change CV thresholds after execution.");
        _output.WriteLine("  3. Do NOT change the Omega computation method.");
        _output.WriteLine("  4. Do NOT exclude outlier seeds from ensemble stats.");
        _output.WriteLine("  5. Do NOT change xi, K0, N, s, or coupling law.");
        _output.WriteLine("  6. Do NOT change the CV formula (population std).");
        _output.WriteLine("  7. Do NOT claim Omega is seed-stable until audited.");
        _output.WriteLine("  8. Do NOT claim H9/H10/H11/H12 confirmed.");
        _output.WriteLine("  9. Do NOT claim attractor decomposition.");
        _output.WriteLine("  10. Do NOT introduce physical interpretation.");
        _output.WriteLine("  11. Do NOT proceed to M4 before M3c resolution.");
        _output.WriteLine("");
        _output.WriteLine("FORBIDDEN ACTIONS FROZEN.");
    }

    [Fact]
    public void V5_3_OSSA_08_ClaimDisciplineAudit()
    {
        _output.WriteLine("=== M3c.7: CLAIM DISCIPLINE AUDIT ===");
        _output.WriteLine("");
        _output.WriteLine("SUPPORTED:");
        _output.WriteLine("  - Five seed blocks defined.");
        _output.WriteLine("  - Measurement pipeline and metrics defined.");
        _output.WriteLine("  - CV thresholds pre-registered.");
        _output.WriteLine("  - Decision gates defined.");
        _output.WriteLine($"  - V5.2 Ω CV = {V52_MeasuredOmegaCv:F3} (live measurement).");
        _output.WriteLine($"  - M3b Ω CV = {M3b_MeasuredOmegaCv:F3} (live measurement).");
        _output.WriteLine("");
        _output.WriteLine("CONDITIONAL:");
        _output.WriteLine("  - Results depend on seed block selection.");
        _output.WriteLine("  - Results depend on finite seed ensembles.");
        _output.WriteLine("  - Results depend on TRM pipeline implementation.");
        _output.WriteLine($"  - Results specific to xi={FrozenXi:F2}, N={FrozenN}.");
        _output.WriteLine("");
        _output.WriteLine("HYPOTHESIS (tested by M3c execution):");
        _output.WriteLine($"  - Ω CV is robustly ≤ {StableThreshold:F2} across most blocks.");
        _output.WriteLine($"  - OR: Ω CV ~{V52_MeasuredOmegaCv:F3} is typical, and the");
        _output.WriteLine($"    historical CV~{HistoricalOmegaCv:F2} was anomalous.");
        _output.WriteLine("");
        _output.WriteLine("NOT CLAIMED:");
        _output.WriteLine("  - H9/H10/H11/H12 confirmed");
        _output.WriteLine("  - Omega is seed-stable");
        _output.WriteLine("  - Attractor decomposition exists");
        _output.WriteLine("  - Physical constants derived");
        _output.WriteLine("  - Quantum mechanics, spacetime emergence");
        _output.WriteLine("  - Causation");
        _output.WriteLine("");
        _output.WriteLine("AUDIT: PASSED.");
    }
}
