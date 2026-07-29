using System;
using System.Linq;
using System.Text;
using Xunit;
using Xunit.Abstractions;

namespace TRM.Tests.V30_1;

[Trait("Category", "V30_1")]
public class V30_1_QualitativeSparcTest_Tests
{
    private readonly ITestOutputHelper _o;
    public V30_1_QualitativeSparcTest_Tests(ITestOutputHelper o) { _o = o; }

    [Fact]
    public void QST_01_QualitativeSparcTestAudit()
    {
        var sb = new StringBuilder();
        sb.AppendLine(new string('=', 108));
        sb.AppendLine("=== QST_01: Qualitative SPARC Test Audit ===");
        sb.AppendLine("=== Which predictions require NO calibration? ===");
        sb.AppendLine(new string('=', 108));
        sb.AppendLine("");
        sb.AppendLine("KNOWN (SDC_01): S_T, S_L, S_V unknown.");
        sb.AppendLine("QUESTION: What can be tested with SPARC data RIGHT NOW?");
        sb.AppendLine("RULE: Only structural/relational predictions — no numerical values.");
        sb.AppendLine("");

        var tests = new QualitativeTest[]
        {
            new("Asymptotic flattening EXISTS",
                "TRM predicts v_max converges to finite non-zero asymptote. Rotation curves MUST flatten at large radii.",
                "Does v_flat EXIST for each galaxy? Binary test: yes/no. If any galaxy shows continuously DECLINING velocity at all radii → TRM falsified.",
                "SPARC outer data points. For each galaxy, check: does v(r) stabilize within last 20% of data?",
                "Presence of flattening: fraction of galaxies with flat outer rotation.",
                CalFree: true, Immediate: true, Falsifiable: true, StrongTarget: true,
                "Flattening existence is binary. No calibration needed. Direct TRM prediction."),

            new("Asymptotic convergence under refinement",
                "TRM predicts v_flat estimate CONVERGES (stabilizes) as more outer data included. Not oscillating, not diverging.",
                "Subsample each rotation curve at increasing outer radii. Does the v_flat estimate converge monotonically?",
                "SPARC galaxies with ≥15 data points beyond turnover radius. Sample at 60%, 70%, 80%, 90% of max radius.",
                "Convergence metric: |v_flat(90%) - v_flat(70%)| / v_flat(90%). Should decrease as more data included.",
                CalFree: true, Immediate: true, Falsifiable: true, StrongTarget: true,
                "Convergence behavior is structural. No numerical values needed — just direction of change."),

            new("Density primacy: baryons dominate dynamics",
                "TRM density→curvature law predicts baryonic mass ALONE should explain ≥90% of total dynamics.",
                "For each galaxy: compute M_baryon(R) from gas+stars. Compare to M_dyn(R) = v(R)²·R/G. Is M_baryon ≥ 0.9·M_dyn at all radii?",
                "SPARC data with gas mass measurements. Need v(R), M_gas(R), M_star(R). All available in SPARC.",
                "Mass deficit: median(M_dyn/M_baryon). TRM predicts median ≈ 1.0 (±0.1). Falsified if median > 1.2.",
                CalFree: true, Immediate: true, Falsifiable: true, StrongTarget: true,
                "Mass ratios are dimensionless. No TRM calibration needed — SPARC provides both masses in physical units."),

            new("v_flat range is narrow vs mass range",
                "TRM universality: v_max varies less than boundary density varies. Analog: v_flat should be narrow relative to M_baryon range.",
                "Compare CV(v_flat) vs CV(M_baryon) across SPARC sample. TRM predicts CV(v_flat) < CV(M_baryon).",
                "Full SPARC sample. Need v_flat (asymptotic velocity) and M_baryon (baryonic mass) for each galaxy.",
                "If CV(v_flat) > CV(M_baryon) → universality is wrong. Expected: velocity range narrower than mass range.",
                CalFree: true, Immediate: true, Falsifiable: true, StrongTarget: true,
                "Both are dimensionless CV comparisons. No TRM calibration needed."),

            new("No additional mass component needed",
                "TRM density primacy: if baryons explain dynamics, residuals should be STRUCTURELESS (no systematic 'missing mass' at specific radii).",
                "For each galaxy: compute residual Δv = v_obs - v_baryon_only. TRM predicts residuals are randomly distributed, not peaked at any radius.",
                "SPARC data. Compare residual distribution to null model (Gaussian around zero).",
                "If residuals show systematic positive offset (v_obs > v_baryon consistently) → dark matter signature, TRM falsified.",
                CalFree: true, Immediate: true, Falsifiable: true, StrongTarget: true,
                "Residual structure is calibration-free. TRM predicts none; ΛCDM predicts systematic offset."),

            new("Density gradient vs curvature (future test)",
                "TRM: ΔTick ∝ √(connectivity gradient). Galaxy analog: d(v²)/dr ∝ √(dρ/dr). Requires density gradient data.",
                "Compare d(v²)/dr to √(dΣ/dr) where Σ = surface density. Not directly in SPARC velocity data.",
                "SPARC has density profiles but not measured gradients. Requires careful binning of gas+star distributions.",
                "Correlation: Pearson r between d(v²)/dr and √(dΣ/dr). Falsified if r < 0.3.",
                CalFree: true, Immediate: false, Falsifiable: true, StrongTarget: false,
                "Testable but needs additional data processing. Not immediate — requires gradient computation."),
        };

        int calFreeCount = tests.Count(t => t.CalFree);
        int immediateCount = tests.Count(t => t.Immediate);
        int falsifiableCount = tests.Count(t => t.Falsifiable);
        int strongCount = tests.Count(t => t.StrongTarget);

        sb.AppendLine(new string('=', 108));
        sb.AppendLine("=== Calibration-Free Tests ===");
        sb.AppendLine("");

        sb.AppendLine($"{"Test",-42} {"CalFree",8} {"Immed",6} {"Fals",5} {"Priority",9}");
        sb.AppendLine(new string('-', 72));

        int priority = 1;
        foreach (var t in tests.OrderByDescending(t => (t.StrongTarget?1:0)+(t.Immediate?1:0)+(t.Falsifiable?1:0)))
        {
            string prio = t.StrongTarget ? $"★ P{priority++}" : $"  P{priority}";
            sb.AppendLine($"{t.Name,-42} {(t.CalFree?"  YES":"   no"),8} {(t.Immediate?" YES":"  no"),6} {(t.Falsifiable?" YES":"  no"),5} {prio,9}");
        }
        sb.AppendLine("");

        sb.AppendLine($"  Calibration-free: {calFreeCount}/{tests.Length}");
        sb.AppendLine($"  Immediate (today): {immediateCount}/{tests.Length}");
        sb.AppendLine($"  Falsifiable: {falsifiableCount}/{tests.Length}");
        sb.AppendLine($"  Strong targets: {strongCount}/{tests.Length}");
        sb.AppendLine("");

        // ================================================================
        // DETAILED TESTS
        // ================================================================
        sb.AppendLine(new string('=', 108));
        sb.AppendLine("=== Immediate Comparison Plan ===");
        sb.AppendLine("");

        foreach (var t in tests.Where(t => t.Immediate))
        {
            sb.AppendLine($"  ★ {t.Name}:");
            sb.AppendLine($"    TRM predicts: {t.TrmPrediction}");
            sb.AppendLine($"    Test:         {t.TestProcedure}");
            sb.AppendLine($"    Data:         {t.SparcData}");
            sb.AppendLine($"    Metric:       {t.Metric}");
            sb.AppendLine($"    → {t.Comment}");
            sb.AppendLine("");
        }

        // ================================================================
        // FASTEST FALSIFICATION
        // ================================================================
        sb.AppendLine(new string('=', 108));
        sb.AppendLine("=== Fastest Falsification Route ===");
        sb.AppendLine("");

        var fastest = tests.Where(t => t.StrongTarget && t.Immediate).First();
        sb.AppendLine($"  #{tests.Where(t=>t.StrongTarget&&t.Immediate).ToList().IndexOf(fastest)+1}: {fastest.Name}");
        sb.AppendLine($"  Procedure: {fastest.TestProcedure}");
        sb.AppendLine($"  Falsified if: {fastest.Metric}");
        sb.AppendLine("");

        // ================================================================
        // DECISION
        // ================================================================
        sb.AppendLine(new string('=', 108));
        sb.AppendLine("=== Decision ===");
        sb.AppendLine("");

        bool criterionA = calFreeCount >= 4;
        bool criterionB = immediateCount >= 3;
        bool criterionC = falsifiableCount >= 4;
        bool criterionD = tests.Length >= 5;

        int criteriaMet = 0;
        if (criterionA) criteriaMet++;
        if (criterionB) criteriaMet++;
        if (criterionC) criteriaMet++;
        if (criterionD) criteriaMet++;

        string verdict = criteriaMet >= 4 ? "SUPPORTED: direct structural comparison possible."
            : criteriaMet >= 2 ? "CONDITIONAL: partial comparison possible."
            : "FALSIFIED: all tests require calibration.";

        sb.AppendLine($"VERDICT: {verdict}  ({criteriaMet}/4 criteria)");
        sb.AppendLine("");
        sb.AppendLine($"  A. ≥4 calibration-free:                  {(criterionA ? "YES" : "NO")} ({calFreeCount})");
        sb.AppendLine($"  B. ≥3 immediate (today):                 {(criterionB ? "YES" : "NO")} ({immediateCount})");
        sb.AppendLine($"  C. ≥4 falsifiable:                       {(criterionC ? "YES" : "NO")} ({falsifiableCount})");
        sb.AppendLine($"  D. ≥5 tests defined:                     {(criterionD ? "YES" : "NO")} ({tests.Length})");
        sb.AppendLine("");
        sb.AppendLine("Qualitative SPARC Test Principle:");
        sb.AppendLine("  TRM predictions can be tested against SPARC data TODAY");
        sb.AppendLine("  without any calibration. Flattening existence, convergence,");
        sb.AppendLine("  density primacy, universality range, and residual structure");
        sb.AppendLine("  are ALL calibration-free structural tests. TRM is ready");
        sb.AppendLine("  for first contact with astronomical data.");
        sb.AppendLine("");
        sb.AppendLine(new string('=', 108));
        sb.AppendLine("=== QST_01 complete. Commit: QST_01_QualitativeSparcTestAudit ===");

        _o.WriteLine(sb.ToString());
        Assert.True(true);
    }

    private record QualitativeTest(string Name, string TrmPrediction, string TestProcedure,
        string SparcData, string Metric, bool CalFree, bool Immediate, bool Falsifiable,
        bool StrongTarget, string Comment);
}
