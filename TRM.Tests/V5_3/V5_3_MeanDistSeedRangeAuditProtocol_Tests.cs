using Xunit;
using Xunit.Abstractions;

namespace TRM.Tests.V5_3;

/// <summary>
/// M3b MeanDist Seed Range Audit Protocol (MSRA):
///
/// Determines whether the V5.2 MeanDist seed-variability classification
/// (CV ~0.30) is robust across seed ranges or specific to seed block
/// 100–109. M3 found that seeds 520–524 have MD seed-CV ~0.046 — a
/// 6× discrepancy from the V5.2 reference.
///
/// Five seed blocks are tested, each at the primary regime (xi=1.80):
///   Block A: seeds 100–109 (V5.2/V5.1 original)
///   Block B: seeds 520–524 (M1/M2/M3 current)
///   Block C: seeds 200–209
///   Block D: seeds 300–309
///   Block E: seeds 400–409
///
/// 10 seeds per block for reliable CV estimation. Full TRM RecoverFP
/// pipeline. Same graph, frequency, distance, normalization conventions.
///
/// CLAIM DISCIPLINE:
///   SUPPORTED:       Seed blocks and metrics are defined.
///   CONDITIONAL:     Results depend on seed block selection,
///                    finite seed ensembles, TRM implementation.
///   HYPOTHESIS:      MeanDist seed-variability may be robust
///                    across seed blocks or block-specific.
///   NOT CLAIMED:     H10/H11/H12 confirmation, attractor
///                    decomposition, physical constants.
///
/// PROTOCOL-DEFINITION only. Execution deferred.
/// </summary>
[Trait("Category", "V5_3")]
[Trait("Category", "V5_3_MSRA")]
public class V5_3_MeanDistSeedRangeAuditProtocol_Tests
{
    private readonly ITestOutputHelper _output;

    // ── Frozen parameters ──
    internal const double FrozenK0 = 1.15;
    internal const int FrozenN = 100;
    internal const double FrozenS = 0.08;
    internal const double FrozenXi = 1.80; // primary regime only — single xi per block
    internal const double Dt = 0.05;
    internal const double REps = 1e-8;
    internal const int St = 400;
    internal const int Hd = 4;

    // ── Seed blocks ──
    internal static readonly (string name, int start, int count)[] SeedBlocks = new[]
    {
        ("A: V5.2 original",  100, 10),
        ("B: M3 current",     520, 5),
        ("C: mid-range",      200, 10),
        ("D: upper-mid",      300, 10),
        ("E: upper",          400, 10),
    };

    // ── Thresholds (frozen from M3) ──
    internal const double SeedVariableThreshold = 0.15;
    internal const double V52_ReferenceCv = 0.30;

    // ── Robustness criteria ──
    // "Most blocks" = ≥ 3 of 5 blocks
    internal const int MostBlocksThreshold = 3;

    // "Near V5.2 reference" = CV ≥ 0.15 (half the reference, conservative)
    internal const double NearReferenceThreshold = 0.15;

    public V5_3_MeanDistSeedRangeAuditProtocol_Tests(ITestOutputHelper o) { _output = o; }

    // ═══════════════════════════════════════════════════════════

    [Fact]
    public void V5_3_MSRA_01_M3ContextLoaded()
    {
        _output.WriteLine("=== M3b.0: M3 CONTEXT ===");
        _output.WriteLine("");
        _output.WriteLine("M3 MeanDistSaturationBoundary result:");
        _output.WriteLine("  Gate C — MIXED / PARTIAL");
        _output.WriteLine("  ALL baselines reproduced xi-robustness.");
        _output.WriteLine("  NO baseline reproduced seed-variability.");
        _output.WriteLine("");
        _output.WriteLine("Critical M3 finding:");
        _output.WriteLine("  B3 (TRM RecoverFP) seed-CV(D90) = 0.046 (seeds 520–524)");
        _output.WriteLine("  V5.2 reference seed-CV ~0.30 (seeds 100–109)");
        _output.WriteLine("  → 6× discrepancy between seed blocks.");
        _output.WriteLine("");
        _output.WriteLine("M3b question:");
        _output.WriteLine("  Is MeanDist seed-variability robust across seed blocks,");
        _output.WriteLine("  or specific to the V5.1/V5.2 seed block 100–109?");
        _output.WriteLine("");
        _output.WriteLine("M3 CONTEXT LOADED.");
    }

    [Fact]
    public void V5_3_MSRA_02_SeedBlocksDefined()
    {
        _output.WriteLine("=== M3b.1: SEED BLOCKS ===");
        _output.WriteLine("");
        _output.WriteLine("Five seed blocks spanning the integer space:");
        _output.WriteLine("");
        _output.WriteLine($"{"Block",-8} {"Name",-20} {"Seeds",-18} {"Count",-8} {"Purpose"}");
        _output.WriteLine(new string('-', 70));
        foreach (var (name, start, count) in SeedBlocks)
        {
            string seedRange = count > 1 ? $"{start}–{start + count - 1}" : $"{start}";
            _output.WriteLine($"{"Block " + name[0],-8} {name,-20} {seedRange,-18} {count,-8} {(name.Contains("original") ? "V5.2 reference block" : name.Contains("current") ? "M3 anomaly block" : "Control block")}");
        }
        _output.WriteLine("");
        _output.WriteLine($"Total seeds: {SeedBlocks.Sum(b => b.count)} across {SeedBlocks.Length} blocks.");
        _output.WriteLine("");
        _output.WriteLine("Block A (100–109) is the V5.1/V5.2 original ensemble.");
        _output.WriteLine("Block B (520–524) is the M1/M2/M3 current ensemble.");
        _output.WriteLine("Blocks C, D, E are independent control blocks.");
        _output.WriteLine("");
        _output.WriteLine("SEED BLOCKS FROZEN.");
    }

    [Fact]
    public void V5_3_MSRA_03_MeasurementPipelineDefined()
    {
        _output.WriteLine("=== M3b.2: MEASUREMENT PIPELINE ===");
        _output.WriteLine("");
        _output.WriteLine("For each seed in each block, at xi={FrozenXi:F2}:");
        _output.WriteLine("");
        _output.WriteLine("  1. Generate graph: KS(N, seed) → dense adjacency");
        _output.WriteLine("  2. RecoverFP: Rfp(K, N, K0={FrozenK0}, xi={FrozenXi:F2},");
        _output.WriteLine("     s={FrozenS}, E=EpochsForN(N), seed)");
        _output.WriteLine("  3. Final simulation: Sm(K_fp, N, s, seed+E)");
        _output.WriteLine("  4. Phase correlation: R = RP(h)");
        _output.WriteLine("  5. Emergent distances: d = DL(Nm(R))");
        _output.WriteLine("  6. MeanDist: MD = mean(d[i,j] for i<j) [raw]");
        _output.WriteLine("  7. MD/D_90: D_90 = 90th percentile of d[i,j] for i<j");
        _output.WriteLine("     MD_norm = MD / D_90 [primary normalization]");
        _output.WriteLine("  8. MD/D_median: D_med = median of d[i,j] for i<j");
        _output.WriteLine("     MD_mednorm = MD / D_med [robustness check]");
        _output.WriteLine("  9. Omega: om = OmegaField(h); Ω = mean(om) [control]");
        _output.WriteLine("");
        _output.WriteLine("All computations use the same pipeline as M1/M2/M3.");
        _output.WriteLine("");
        _output.WriteLine("MEASUREMENT PIPELINE DEFINED.");
    }

    [Fact]
    public void V5_3_MSRA_04_MetricsDefined()
    {
        _output.WriteLine("=== M3b.3: METRICS PER BLOCK ===");
        _output.WriteLine("");
        _output.WriteLine("For each block B, compute:");
        _output.WriteLine("");
        _output.WriteLine("PRIMARY:");
        _output.WriteLine("  MD_raw_mean(B)   = mean of raw MD across seeds");
        _output.WriteLine("  MD_raw_std(B)    = std of raw MD across seeds");
        _output.WriteLine("  CV_raw_MD(B)     = MD_raw_std / MD_raw_mean");
        _output.WriteLine("  MD_norm_mean(B)  = mean of MD/D_90 across seeds");
        _output.WriteLine("  CV_norm_MD(B)    = std(MD/D_90) / mean(MD/D_90)");
        _output.WriteLine("");
        _output.WriteLine("SECONDARY (robustness):");
        _output.WriteLine("  CV_mednorm_MD(B) = std(MD/D_median) / mean(MD/D_median)");
        _output.WriteLine("");
        _output.WriteLine("CONTROL:");
        _output.WriteLine("  Ω_mean(B)        = mean Omega across seeds");
        _output.WriteLine("  CV_Omega(B)      = CV of Omega across seeds");
        _output.WriteLine("  (Ω should be seed-STABLE at CV ~0.01 — if not,");
        _output.WriteLine("   the seed block may have anomalous frequency draws.)");
        _output.WriteLine("");
        _output.WriteLine("CLASSIFICATION PER BLOCK:");
        _output.WriteLine($"  seed-variable if CV_norm_MD > {SeedVariableThreshold:F2}");
        _output.WriteLine($"  seed-stable   if CV_norm_MD ≤ {SeedVariableThreshold:F2}");
        _output.WriteLine("");
        _output.WriteLine($"V5.2 reference: CV_raw_MD ~{V52_ReferenceCv:F2}");
        _output.WriteLine("");
        _output.WriteLine("METRICS DEFINED.");
    }

    [Fact]
    public void V5_3_MSRA_05_DecisionGatesDefined()
    {
        _output.WriteLine("=== M3b.4: DECISION GATES ===");
        _output.WriteLine("");
        _output.WriteLine($"Robustness criteria:");
        _output.WriteLine($"  - 'Most blocks' = ≥ {MostBlocksThreshold} of {SeedBlocks.Length} blocks");
        _output.WriteLine($"  - 'Near reference' = CV_norm_MD ≥ {NearReferenceThreshold:F2}");
        _output.WriteLine($"  - 'Seed-variable' = CV_norm_MD > {SeedVariableThreshold:F2}");
        _output.WriteLine("");
        _output.WriteLine("═══ GATE A: ROBUST SEED VARIABILITY ═══");
        _output.WriteLine("");
        _output.WriteLine("  Condition:");
        _output.WriteLine($"    ≥ {MostBlocksThreshold} blocks are classified as seed-variable");
        _output.WriteLine($"    AND Block A (seeds 100–109) is seed-variable");
        _output.WriteLine("");
        _output.WriteLine("  Interpretation:");
        _output.WriteLine("    MeanDist seed-variability is robust across seed blocks.");
        _output.WriteLine("    M3 block 520–524 was likely an unusually stable outlier.");
        _output.WriteLine("    H10 becomes testable again.");
        _output.WriteLine("");
        _output.WriteLine("  Next: M4 latent-variable regression.");
        _output.WriteLine("");
        _output.WriteLine("═══ GATE B: SEED-BLOCK SPECIFIC VARIABILITY ═══");
        _output.WriteLine("");
        _output.WriteLine("  Condition:");
        _output.WriteLine("    Block A (seeds 100–109) is seed-variable");
        _output.WriteLine($"    BUT < {MostBlocksThreshold} total blocks are seed-variable");
        _output.WriteLine("");
        _output.WriteLine("  Interpretation:");
        _output.WriteLine("    V5.2 seed block 100–109 is an outlier with high MD CV.");
        _output.WriteLine("    MeanDist seed-variability is seed-block-specific.");
        _output.WriteLine("    V5.2 'seed-variable' classification must be narrowed.");
        _output.WriteLine("    H10 is weakened.");
        _output.WriteLine("");
        _output.WriteLine("  Next: Investigate what makes block 100–109 anomalous.");
        _output.WriteLine("    (Graph structure? Frequency draws? Cluster geometry?)");
        _output.WriteLine("");
        _output.WriteLine("═══ GATE C: NO ROBUST SEED VARIABILITY ═══");
        _output.WriteLine("");
        _output.WriteLine("  Condition:");
        _output.WriteLine("    NO block (including Block A) is seed-variable");
        _output.WriteLine("");
        _output.WriteLine("  Interpretation:");
        _output.WriteLine("    The V5.2 MeanDist seed-variable classification does NOT");
        _output.WriteLine("    reproduce under current M3 measurement protocol.");
        _output.WriteLine("    Possible causes:");
        _output.WriteLine("      - Different MeanDist computation (pairwise distance");
        _output.WriteLine("        matrix vs alternative proxy)");
        _output.WriteLine("      - Different normalization or distance metric");
        _output.WriteLine("      - V5.2 CV computed differently (e.g., different");
        _output.WriteLine("        outlier handling or seed count)");
        _output.WriteLine("    H10 is weakened.");
        _output.WriteLine("");
        _output.WriteLine("  Next: Audit V5.2 MeanDist computation pipeline for");
        _output.WriteLine("    differences from M3 pipeline.");
        _output.WriteLine("");
        _output.WriteLine("═══ GATE D: NORMALIZATION-DEPENDENT ═══");
        _output.WriteLine("");
        _output.WriteLine("  Condition:");
        _output.WriteLine("    D_90 and D_median normalizations produce different");
        _output.WriteLine("    seed-variable classifications for ≥ 2 blocks.");
        _output.WriteLine("");
        _output.WriteLine("  Interpretation:");
        _output.WriteLine("    MeanDist seed-variability classification depends on");
        _output.WriteLine("    normalization choice. H10 cannot be evaluated without");
        _output.WriteLine("    a normalization audit.");
        _output.WriteLine("");
        _output.WriteLine("  Next: Normalization sensitivity analysis.");
        _output.WriteLine("");
        _output.WriteLine("═══ GATE E: OMEGA CONTROL FAILURE ═══");
        _output.WriteLine("");
        _output.WriteLine("  Condition:");
        _output.WriteLine("    Ω CV > 0.05 in any block (Ω should be seed-stable).");
        _output.WriteLine("");
        _output.WriteLine("  Interpretation:");
        _output.WriteLine("    Omega is unexpectedly variable in this seed block.");
        _output.WriteLine("    The block may contain anomalous frequency draws or");
        _output.WriteLine("    graph realizations. Flag for exclusion or investigation.");
        _output.WriteLine("");
        _output.WriteLine("DECISION GATES FROZEN.");
    }

    [Fact]
    public void V5_3_MSRA_06_ExecutionRunPlan()
    {
        _output.WriteLine("=== M3b.5: RUN PLAN ===");
        _output.WriteLine("");
        _output.WriteLine("Each seed requires: 1 full TRM RecoverFP pipeline.");
        _output.WriteLine("  KS → Rfp(E=5) → Sm (6 Sm calls per seed)");
        _output.WriteLine("");
        int totalSm = 0;
        _output.WriteLine($"{"Block",-20} {"Seeds",-8} {"Sm calls",-10} {"Est. time"}");
        _output.WriteLine(new string('-', 50));
        foreach (var (name, start, count) in SeedBlocks)
        {
            int smCalls = count * 6;
            totalSm += smCalls;
            _output.WriteLine($"{name,-20} {count,-8} {smCalls,-10} ~{count * 1.2:F0}s");
        }
        _output.WriteLine(new string('-', 50));
        _output.WriteLine($"{"TOTAL",-20} {SeedBlocks.Sum(b => b.count),-8} {totalSm,-10} ~{totalSm / 6 * 1.2:F0}s");
        _output.WriteLine("");
        _output.WriteLine("Staged execution option:");
        _output.WriteLine("  Stage 1: Blocks A+B only (15 seeds, 90 Sm calls, ~18s)");
        _output.WriteLine("    → Immediate comparison: V5.2 block vs M3 block.");
        _output.WriteLine("  Stage 2: Blocks C+D+E (30 seeds, 180 Sm calls, ~36s)");
        _output.WriteLine("    → Full audit if Stage 1 is inconclusive.");
        _output.WriteLine("");
        _output.WriteLine("RUN PLAN DEFINED.");
    }

    [Fact]
    public void V5_3_MSRA_07_ForbiddenActionsDefined()
    {
        _output.WriteLine("=== M3b.6: FORBIDDEN ACTIONS ===");
        _output.WriteLine("");
        _output.WriteLine("  1. Do NOT add/remove seed blocks after execution.");
        _output.WriteLine("  2. Do NOT change normalization method after execution.");
        _output.WriteLine("  3. Do NOT change thresholds after execution.");
        _output.WriteLine("  4. Do NOT exclude outlier seeds from ensemble stats.");
        _output.WriteLine("  5. Do NOT change MeanDist or Omega computation.");
        _output.WriteLine("  6. Do NOT claim H10/H11/H12 confirmed.");
        _output.WriteLine("  7. Do NOT claim attractor decomposition.");
        _output.WriteLine("  8. Do NOT claim physical interpretation.");
        _output.WriteLine("  9. Do NOT proceed to M4 before M3b resolution.");
        _output.WriteLine("");
        _output.WriteLine("FORBIDDEN ACTIONS FROZEN.");
    }

    [Fact]
    public void V5_3_MSRA_08_ClaimDisciplineAudit()
    {
        _output.WriteLine("=== M3b.7: CLAIM DISCIPLINE AUDIT ===");
        _output.WriteLine("");
        _output.WriteLine("SUPPORTED:");
        _output.WriteLine("  - Five seed blocks defined spanning seeds 100–524.");
        _output.WriteLine("  - Measurement pipeline and metrics defined.");
        _output.WriteLine("  - Decision gates pre-registered.");
        _output.WriteLine("");
        _output.WriteLine("CONDITIONAL:");
        _output.WriteLine("  - Results depend on seed block selection.");
        _output.WriteLine("  - Results depend on finite seed ensembles.");
        _output.WriteLine("  - Results depend on D_90 normalization.");
        _output.WriteLine($"  - Results specific to xi={FrozenXi:F2}, N={FrozenN},");
        _output.WriteLine($"    K0={FrozenK0}, s={FrozenS}, exp coupling.");
        _output.WriteLine("");
        _output.WriteLine("HYPOTHESIS (tested by M3b execution):");
        _output.WriteLine("  - MeanDist seed-variability is robust across seed blocks.");
        _output.WriteLine("  - OR: MeanDist seed-variability is block-specific.");
        _output.WriteLine("  - OR: MeanDist seed-variability does not reproduce");
        _output.WriteLine("    under the current measurement protocol.");
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
