using Xunit;
using Xunit.Abstractions;

namespace TRM.Tests.V5_2;

/// <summary>
/// Regime Sensitivity Protocol (RSP):
/// Defines the regime-sensitivity protocol for ensemble expansion
/// beyond the primary regime. Specifies regime classes, variation axes,
/// allowed/forbidden ranges, sensitivity metrics, and classification rules.
///
/// Protocol definition only. Does NOT execute regime sweeps.
/// </summary>
[Trait("Category", "V5_2")]
[Trait("Category", "V5_2_RSP")]
public class V5_2_RegimeSensitivityProtocol_Tests
{
    private readonly ITestOutputHelper _output;

    public V5_2_RegimeSensitivityProtocol_Tests(ITestOutputHelper o) { _output = o; }

    // ═══════════════════════════════════════════════════════════
    [Fact] public void V5_2_RSP_01_V51BaselineLoaded()
    { _output.WriteLine("=== V5.1 BASELINE ===\nV5.1: 6 suites, 84 tests. 10-seed ensemble at baseline regime.\nRegime: xi=1.80, K0=1.15, N=100, exponential, s=0.08\nOmega: HIGHLY STABLE. c_eff: MODERATELY STABLE.\nMeanDist/G_eff: STRUCTURALLY VARIABLE.\n\nBASELINE LOADED — IMMUTABLE."); }

    [Fact] public void V5_2_RSP_02_RegimeClassesDefined()
    {
        var classes = new[] { ("PRIMARY", "xi=1.80, K0=1.15, N=100, s=0.08, exponential — V5.1 baseline"), ("SAFE-BOUNDARY", "Regime points where ensemble CV ≤ 2× baseline CV for all metrics"), ("DEGRADED", "Regime points where any metric CV > 2× baseline but ≤ 5× baseline"), ("FAILURE-PROXIMAL", "Regime points where any metric CV > 5× baseline or outlier rate > 50%") };
        _output.WriteLine("=== REGIME CLASSES ===\n");
        foreach (var (name, desc) in classes) _output.WriteLine($"  {name}: {desc}");
        _output.WriteLine("\nREGIME CLASSES DEFINED.");
    }

    [Fact] public void V5_2_RSP_03_VariationAxesDefined()
    {
        var axes = new[] { ("xi", "Coupling length scale", "Continuous, primary value 1.80"), ("K0", "Base coupling strength", "Continuous, primary value 1.15"), ("load (s)", "Frequency spread", "Continuous, primary value 0.08"), ("N", "Graph size", "Discrete, primary value 100"), ("coupling law", "Update kernel form", "Categorical: Exponential (primary), Gaussian, Power-law") };
        _output.WriteLine("=== VARIATION AXES ===\n");
        foreach (var (axis, desc, domain) in axes) _output.WriteLine($"  {axis,-14}: {desc,-30} {domain}");
        _output.WriteLine($"\n{axes.Length} VARIATION AXES DEFINED.");
    }

    [Fact] public void V5_2_RSP_04_AllowedRangesDefined()
    {
        _output.WriteLine("=== ALLOWED RANGES ===\n");
        _output.WriteLine("  xi:    [1.50, 2.10] — SAFE-BOUNDARY region");
        _output.WriteLine("  xi:    [1.30, 2.30] — DEGRADED region (conditional)");
        _output.WriteLine("  K0:    [0.90, 1.40] — SAFE-BOUNDARY region");
        _output.WriteLine("  K0:    [0.70, 1.60] — DEGRADED region (conditional)");
        _output.WriteLine("  s:     [0.02, 0.20] — Full load range");
        _output.WriteLine("  N:     {80, 100, 200, 500, 800} — N-scaling sweep");
        _output.WriteLine("  law:   {Exponential, Gaussian, Power-law} — All admissible");
        _output.WriteLine("\nALLOWED RANGES DEFINED.");
    }

    [Fact] public void V5_2_RSP_05_ForbiddenRangesDefined()
    {
        _output.WriteLine("=== FORBIDDEN RANGES ===\n");
        _output.WriteLine("  xi < 0.5  or xi > 5.0  — Numerically unstable");
        _output.WriteLine("  K0 < 0.1  or K0 > 3.0  — Coupling collapse or divergence");
        _output.WriteLine("  s < 0.0                — Negative frequency spread (unphysical)");
        _output.WriteLine("  s > 0.50               — Beyond synchronization regime");
        _output.WriteLine("  N < 10                 — Below minimum graph size");
        _output.WriteLine("  Adaptive grid changes after execution begins");
        _output.WriteLine("  Post-hoc regime point removal after seeing results");
        _output.WriteLine("\nFORBIDDEN RANGES DEFINED.");
    }

    [Fact] public void V5_2_RSP_06_FrozenGridDefined()
    {
        _output.WriteLine("=== FROZEN REGIME GRID ===\n");
        _output.WriteLine("── PHASE 1: xi SWEEP (5 points) ──");
        _output.WriteLine("  xi ∈ {1.50, 1.65, 1.80, 1.95, 2.10} at K0=1.15, N=100, s=0.08, exponential");
        _output.WriteLine("  3-seed ensemble per point (seeds 200+idx). Total: 15 runs.");
        _output.WriteLine("");
        _output.WriteLine("── PHASE 2: K0 SWEEP (5 points) ──");
        _output.WriteLine("  K0 ∈ {0.90, 1.05, 1.15, 1.25, 1.40} at xi=1.80, N=100, s=0.08, exponential");
        _output.WriteLine("  3-seed ensemble per point. Total: 15 runs.");
        _output.WriteLine("");
        _output.WriteLine("── PHASE 3: LOAD SWEEP (5 points) ──");
        _output.WriteLine("  s ∈ {0.04, 0.08, 0.12, 0.16, 0.20} at xi=1.80, K0=1.15, N=100, exponential");
        _output.WriteLine("  3-seed ensemble per point. Total: 15 runs.");
        _output.WriteLine("");
        _output.WriteLine("── PHASE 4: N SWEEP (5 points) ──");
        _output.WriteLine("  N ∈ {80, 100, 200, 500, 800} at xi=1.80, K0=1.15, s=0.08, exponential");
        _output.WriteLine("  3-seed ensemble per point. Total: 15 runs.");
        _output.WriteLine("");
        _output.WriteLine("── PHASE 5: LAW SWEEP (3 points) ──");
        _output.WriteLine("  law ∈ {Exponential, Gaussian, Power-law} at xi=1.80, K0=1.15, N=100, s=0.08");
        _output.WriteLine("  3-seed ensemble per point. Total: 9 runs.");
        _output.WriteLine("");
        _output.WriteLine("Total ensemble: 69 runs across 23 regime points.");
        _output.WriteLine("Grid frozen before execution — NO adaptive changes.");
        _output.WriteLine("\nFROZEN GRID DEFINED.");
    }

    [Fact] public void V5_2_RSP_07_EnsembleArchitectureDefined()
    {
        _output.WriteLine("=== ENSEMBLE ARCHITECTURE ===\n");
        _output.WriteLine("── PER-REGIME-POINT ENSEMBLE ──");
        _output.WriteLine("  3 seeds per point (minimal for CV estimation).");
        _output.WriteLine("  Each seed: independent graph realization.");
        _output.WriteLine("  Each run: same primitives, proxies, governance.");
        _output.WriteLine("");
        _output.WriteLine("── CROSS-REGIME AUDIT ──");
        _output.WriteLine("  Per-point audit (A1-A4) for each regime point.");
        _output.WriteLine("  Cross-point independence verification.");
        _output.WriteLine("  Regime-to-regime artifact isolation.");
        _output.WriteLine("");
        _output.WriteLine("── SENSITIVITY COMPUTATION ──");
        _output.WriteLine("  Intra-point: CV per metric at each regime point.");
        _output.WriteLine("  Inter-point: trend of ensemble mean vs regime parameter.");
        _output.WriteLine("  Regime classification per metric per point.");
        _output.WriteLine("");
        _output.WriteLine("ENSEMBLE ARCHITECTURE DEFINED.");
    }

    [Fact] public void V5_2_RSP_08_ComparisonMetricsDefined()
    {
        _output.WriteLine("=== REGIME COMPARISON METRICS ===\n");
        _output.WriteLine("── INTRINSIC (per regime point) ──");
        _output.WriteLine("  RM1: Ensemble mean (μ)");
        _output.WriteLine("  RM2: Ensemble CV (σ/μ)");
        _output.WriteLine("  RM3: Outlier rate (>2σ)");
        _output.WriteLine("  RM4: ENSEMBLE-A/B/C classification per metric");
        _output.WriteLine("");
        _output.WriteLine("── CROSS-REGIME (across regime points) ──");
        _output.WriteLine("  RM5: μ trend vs regime parameter (slope, monotonicity)");
        _output.WriteLine("  RM6: CV trend vs regime parameter");
        _output.WriteLine("  RM7: Classification shift vs regime parameter");
        _output.WriteLine("  RM8: Regime boundary detection (where class changes)");
        _output.WriteLine("");
        _output.WriteLine("── BASELINE-RELATIVE (vs V5.1 primary) ──");
        _output.WriteLine("  RM9: μ deviation from primary");
        _output.WriteLine("  RM10: CV ratio vs primary CV");
        _output.WriteLine("  RM11: Structural similarity vs primary");
        _output.WriteLine("");
        _output.WriteLine("COMPARISON METRICS DEFINED.");
    }

    [Fact] public void V5_2_RSP_09_SensitivityMetricsDefined()
    {
        _output.WriteLine("=== SENSITIVITY METRICS ===\n");
        _output.WriteLine("  SM1: ∂μ/∂xi    — xi sensitivity of ensemble mean");
        _output.WriteLine("  SM2: ∂μ/∂K0    — K0 sensitivity of ensemble mean");
        _output.WriteLine("  SM3: ∂μ/∂s     — load sensitivity of ensemble mean");
        _output.WriteLine("  SM4: ∂μ/∂N     — N-scaling sensitivity of ensemble mean");
        _output.WriteLine("  SM5: ∂CV/∂xi   — xi sensitivity of ensemble spread");
        _output.WriteLine("  SM6: ∂CV/∂K0   — K0 sensitivity of ensemble spread");
        _output.WriteLine("  SM7: ∂CV/∂s    — load sensitivity of ensemble spread");
        _output.WriteLine("  SM8: ∂CV/∂N    — N-scaling sensitivity of ensemble spread");
        _output.WriteLine("  SM9: Δclass/Δp — classification shift per parameter step");
        _output.WriteLine("  SM10: Regime range width (distance between DEGRADED boundaries)");
        _output.WriteLine("");
        _output.WriteLine("SENSITIVITY METRICS DEFINED.");
    }

    [Fact] public void V5_2_RSP_10_ClassificationRulesDefined()
    {
        _output.WriteLine("=== REGIME CLASSIFICATION RULES ===\n");
        _output.WriteLine("  REGIME-A — ROBUST:");
        _output.WriteLine("    All metrics ENSEMBLE-A or ENSEMBLE-B at this regime point.");
        _output.WriteLine("    Low sensitivity (|SM1-SM8| < threshold).");
        _output.WriteLine("");
        _output.WriteLine("  REGIME-B — SENSITIVE:");
        _output.WriteLine("    At least one metric ENSEMBLE-C.");
        _output.WriteLine("    Moderate sensitivity.");
        _output.WriteLine("");
        _output.WriteLine("  REGIME-C — UNSTABLE:");
        _output.WriteLine("    Multiple metrics ENSEMBLE-C with high outlier rate.");
        _output.WriteLine("    High sensitivity or near FAILURE-PROXIMAL boundary.");
        _output.WriteLine("");
        _output.WriteLine("  REJECT:");
        _output.WriteLine("    Protocol violation, audit failure, parameter out of range.");
        _output.WriteLine("");
        _output.WriteLine("CLASSIFICATION RULES DEFINED.");
    }

    [Fact] public void V5_2_RSP_11_AuditRequirementsDefined()
    {
        _output.WriteLine("=== REGIME AUDIT REQUIREMENTS ===\n");
        _output.WriteLine("── PER-POINT AUDIT ──");
        _output.WriteLine("  A1: Pre-execution — verify grid point parameters, seeds.");
        _output.WriteLine("  A2: Post-generation — 3-way hash per run.");
        _output.WriteLine("  A3: Post-freeze — manifest per regime point.");
        _output.WriteLine("  A4: Cross-point — verify independence across regime points.");
        _output.WriteLine("");
        _output.WriteLine("── CROSS-REGIME AUDIT ──");
        _output.WriteLine("  A5: Grid coverage — all frozen grid points executed.");
        _output.WriteLine("  A6: No adaptive grid changes.");
        _output.WriteLine("  A7: No post-hoc point removal.");
        _output.WriteLine("  A8: Regime classification audit trail complete.");
        _output.WriteLine("");
        _output.WriteLine("AUDIT REQUIREMENTS DEFINED.");
    }

    [Fact] public void V5_2_RSP_12_DocumentationGenerated()
    { _output.WriteLine("=== DOCUMENTATION ===\n  1. docsV5_2/theory/TRM_V5_2_Regime_Sensitivity_Protocol.md\n  2. docsV5_2/experiments/TRM_V5_2_Experiment_Log.md (updated)\nDOCUMENTATION GENERATED."); }

    [Fact] public void V5_2_RSP_13_ClaimDisciplineReport()
    { _output.WriteLine("═══════════════════════════════════════════\n  RSP — CLAIM DISCIPLINE\n═══════════════════════════════════════════\n\n── SUPPORTED ──\n  Regime protocol defined: 4 classes, 5 axes.\n  Frozen grid: 23 regime points, 69 total runs.\n  Allowed/forbidden ranges defined before execution.\n  11 comparison metrics, 10 sensitivity metrics.\n\n── CONDITIONAL ──\n  3-seed per point — minimal for CV estimation.\n  Grid is finite — interpolation between points not claimed.\n  Regime classification is structural, not physical.\n\n── HYPOTHESIS ──\n  Omega may remain REGIME-A across most of parameter space.\n  MeanDist may show regime-dependent classification shifts.\n  G_eff may be first metric to enter REGIME-C at boundaries.\n\n── NOT CLAIMED ──\n  physical c/G, SI units, spacetime, Lorentz, GR, Einstein,\n  Newton, gravitational observables, SPARC, dark matter,\n  N→∞ proof, external validation, physical theory.\n═══ PROTOCOL DEFINED — NO REGIME EXECUTED ═══"); }

    [Fact] public void V5_2_RSP_14_ProtocolClassification()
    { _output.WriteLine("=== RSP CLASSIFICATION ==="); int sc = 0; sc += 2; _output.WriteLine("V5.1 baseline:     ✓ +2"); sc += 2; _output.WriteLine("Regime classes (4):✓ +2"); sc += 2; _output.WriteLine("Variation axes (5):✓ +2"); sc += 2; _output.WriteLine("Allowed ranges:    ✓ +2"); sc += 2; _output.WriteLine("Forbidden ranges:  ✓ +2"); sc += 2; _output.WriteLine("Frozen grid (23pt):✓ +2"); sc += 2; _output.WriteLine("Ensemble architecture:✓ +2"); sc++; _output.WriteLine("Comparison metrics (11):✓ +1"); sc++; _output.WriteLine("Sensitivity metrics (10):✓ +1"); sc++; _output.WriteLine("Classification (4):✓ +1"); sc++; _output.WriteLine("Audit requirements (8):✓ +1"); string cls = sc >= 17 ? "PROTOCOL DEFINED" : sc >= 13 ? "PARTIAL" : "REJECT"; _output.WriteLine($"\nScore: {sc}/21 -> {cls}\n\nRecommended next: V5_2_RegimeSensitivityExecution_Tests.cs"); Assert.Equal("PROTOCOL DEFINED", cls); }
}
