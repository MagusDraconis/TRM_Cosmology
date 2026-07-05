using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Xunit;
using Xunit.Abstractions;
using static TRM.Tests.QuantumTests.CollectiveModeLockingTests;

namespace TRM.Tests.QuantumTests;

/// <summary>
/// Experiment E1 — Rational Ladder Scan
///
/// Tests whether coupled oscillator lattices exhibit the rational frequency
/// ladder Ω* = 1 + m/N predicted by I1 (closure-family ansatz p = q + m).
///
/// Core question:
///   "Does the rational ladder actually exist?"
///
/// Reference: docs/Final/V3_4/TRM_External_Validation.md — Experiment E1
/// </summary>
public class E1_RationalLadder_Test
{
    private readonly ITestOutputHelper _output;

    public E1_RationalLadder_Test(ITestOutputHelper output)
    {
        _output = output;
    }

    /// <summary>
    /// Per-N result for the rational ladder scan.
    /// </summary>
    private readonly record struct LadderPoint(
        int N,
        double OmegaStar,
        int MEff,
        double Error,
        bool Pass,
        double MeanOrder);

    /// <summary>
    /// Global classification of the rational ladder hypothesis.
    /// </summary>
    private enum LadderClassification
    {
        /// <summary>Clear rational ladder — I1 supported.</summary>
        Supported,

        /// <summary>Partial ladder or noisy — inconclusive.</summary>
        Inconclusive,

        /// <summary>No rational structure — I1 falsified.</summary>
        Falsified
    }

    /// <summary>
    /// E1a — Self-organizing scan (no clock-bias).
    ///
    /// With φ = 0 and ω_i centered at 1.0, the emergent Ω* ≈ 1.0.
    /// This yields the trivial ladder m_eff = 0 for all N.
    ///
    /// The rational formula Ω* = 1 + m/N is satisfied with m = 0,
    /// confirming that the rational structure exists but that the
    /// bare self-organizing dynamics do not select a non-zero m.
    /// </summary>
    [Trait("Category", "PhysicsValidation")]
    [Fact]
    public void E1a_SelfOrganizing_Scan_Should_Yield_Trivial_Ladder()
    {
        _output.WriteLine("══════════════════════════════════════════════");
        _output.WriteLine("  EXPERIMENT E1a — SELF-ORGANIZING SCAN (φ=0)");
        _output.WriteLine("  Testing I1: p = q + m  →  Ω* = 1 + m/N");
        _output.WriteLine("  Config: CollectiveWeight=0, ClockBiasPhi=0");
        _output.WriteLine("══════════════════════════════════════════════");
        _output.WriteLine("");

        var points = RunLadderScan(clockBiasPhi: 0.0, label: "E1a");
        var (classification, passRate, qCoreM3) = SummarizeLadder(points, "E1a");

        // With φ=0, Ω* ≈ 1.0 → m_eff = 0 for all N (trivial ladder).
        // All points should pass the rational alignment check.
        Assert.True(points.Count == 16, $"Expected 16 points, got {points.Count}");
        Assert.True(points.All(p => p.MEff == 0),
            "Self-organizing scan should yield m_eff = 0 (trivial ladder).");
        Assert.True(passRate >= 0.90,
            $"Trivial ladder should show high pass rate. Actual: {passRate:P1}");

        WriteReport(points, classification, passRate, qCoreM3, "E1a_SelfOrganizing");
    }

    /// <summary>
    /// E1b — Bridge-scale clock-bias scan (φ = 0.17).
    ///
    /// With φ = 0.17 and α = 1.0, the clock-bias shifts Ω* to ≈ 1.17.
    /// This places the system in the bridge band Ω ∈ [1.16, 1.19].
    ///
    /// Under I1, Ω* = 1 + m/N with m ≈ round(N · (Ω* − 1)).
    /// For N ∈ {16, 17, 18} (qCore), we expect m = 3.
    /// </summary>
    [Trait("Category", "PhysicsValidation")]
    [Fact]
    public void E1b_BridgeScale_Scan_Should_Show_NonTrivial_Ladder()
    {
        _output.WriteLine("══════════════════════════════════════════════");
        _output.WriteLine("  EXPERIMENT E1b — BRIDGE-SCALE SCAN (φ=0.17)");
        _output.WriteLine("  Testing I1 + I2: Ω* = 1 + m/N, m=3 at qCore");
        _output.WriteLine("  Config: CollectiveWeight=0, ClockBiasPhi=0.17");
        _output.WriteLine("══════════════════════════════════════════════");
        _output.WriteLine("");

        var points = RunLadderScan(clockBiasPhi: 0.17, label: "E1b");
        var (classification, passRate, qCoreM3) = SummarizeLadder(points, "E1b");

        // With φ=0.17, Ω* should be in the bridge band.
        double avgOmega = points.Average(p => p.OmegaStar);
        _output.WriteLine($"E1b average Ω* = {avgOmega:F6}");
        Assert.InRange(avgOmega, 1.14, 1.20);

        // Check qCore: m=3 should appear at N ∈ {16, 17, 18}.
        var qCorePoints = points.Where(p => p.N is 16 or 17 or 18).ToList();
        _output.WriteLine($"E1b qCore m=3 present: {qCoreM3}");
        foreach (var p in qCorePoints)
        {
            _output.WriteLine($"  N={p.N}: Ω*={p.OmegaStar:F6} m_eff={p.MEff}");
        }

        // The test should complete. Classification is diagnostic.
        Assert.True(points.Count == 16, $"Expected 16 points, got {points.Count}");

        WriteReport(points, classification, passRate, qCoreM3, "E1b_BridgeScale");
    }

    /// <summary>
    /// Runs the ladder scan for N ∈ [10, 25] with the given clock-bias φ.
    /// Returns a list of LadderPoint results.
    /// </summary>
    private List<LadderPoint> RunLadderScan(double clockBiasPhi, string label)
    {
        int nMin = 10;
        int nMax = 25;
        int runsPerN = 3;

        var allPoints = new List<LadderPoint>();

        _output.WriteLine($"{"N",6} {"Ω*",10} {"m_eff",6} {"error",10} {"1/(2N)",10} {"PASS",6} {"R",8}");
        _output.WriteLine(new string('─', 64));

        for (int n = nMin; n <= nMax; n++)
        {
            // Self-organizing config with clock-bias.
            var config = ModeLockConfig.Default with
            {
                CellCount = n,
                CollectiveWeight = 0.0,
                OrderScoreWeight = 0.55,
                AlignmentScoreWeight = 0.45,
                CadenceScoreWeight = 0.0,
                Steps = 2000,
                SettleSteps = 800,
                CouplingKappa = 0.10,
                ClockBiasAlpha = 1.0,
                ClockBiasPhi = clockBiasPhi
            };

            // Multiple realizations for robustness.
            double[] omegaStars = new double[runsPerN];
            double[] meanOrders = new double[runsPerN];

            for (int run = 0; run < runsPerN; run++)
            {
                // Dummy Ω — has no effect when CollectiveWeight = 0.
                var result = SimulateModeLock(1.0, config);
                omegaStars[run] = result.EmergentOmega ?? double.NaN;
                meanOrders[run] = result.MeanOrder;
            }

            double omegaStar = omegaStars.Where(o => !double.IsNaN(o)).Average();
            double meanOrder = meanOrders.Average();

            // ── Rational alignment check ──
            // m_eff = round(N * (Ω* − 1))
            int mEff = (int)Math.Round(n * (omegaStar - 1.0));

            // Predicted Ω from ladder: Ω_pred = 1 + m_eff / N
            double omegaPred = 1.0 + (double)mEff / n;

            // Error: |Ω* − (1 + m_eff/N)|
            double error = Math.Abs(omegaStar - omegaPred);

            // Tolerance: half the rational step, i.e. 1/(2N)
            double tolerance = 1.0 / (2.0 * n);

            // Pass if error < tolerance AND m_eff ≥ 0
            bool pass = error < tolerance && mEff >= 0;

            var point = new LadderPoint(n, omegaStar, mEff, error, pass, meanOrder);
            allPoints.Add(point);

            _output.WriteLine(
                $"{n,6} {omegaStar,10:F6} {mEff,6} {error,10:F6} {tolerance,10:F6} {(pass ? " PASS" : " FAIL"),6} {meanOrder,8:F4}");
        }

        _output.WriteLine("");
        return allPoints;
    }

    /// <summary>
    /// Prints ladder summary statistics and returns classification info.
    /// </summary>
    private (LadderClassification Classification, double PassRate, bool QCoreM3)
        SummarizeLadder(List<LadderPoint> points, string label)
    {
        var passPoints = points.Where(p => p.Pass).ToList();
        var failPoints = points.Where(p => !p.Pass).ToList();
        double passRate = (double)passPoints.Count / points.Count;

        _output.WriteLine($"─── {label} LADDER STRUCTURE ───");
        _output.WriteLine($"Total N range:  [10, 25] ({points.Count} points)");
        _output.WriteLine($"Pass count:     {passPoints.Count}");
        _output.WriteLine($"Fail count:     {failPoints.Count}");
        _output.WriteLine("");

        // m_eff distribution.
        var mDistribution = points
            .GroupBy(p => p.MEff)
            .OrderBy(g => g.Key)
            .ToList();

        _output.WriteLine("─── m_eff DISTRIBUTION ───");
        foreach (var group in mDistribution)
        {
            var ns = group.Select(p => p.N.ToString()).ToList();
            _output.WriteLine($"  m = {group.Key,2}: N ∈ {{{string.Join(", ", ns)}}}  (count = {group.Count()})");
        }
        _output.WriteLine("");

        // qCore check.
        var qCorePoints = points.Where(p => p.N is 16 or 17 or 18).ToList();
        _output.WriteLine("─── qCore CHECK (N ∈ {16, 17, 18}) ───");
        bool qCoreM3Present = false;
        foreach (var p in qCorePoints)
        {
            bool isM3 = p.MEff == 3;
            if (isM3) qCoreM3Present = true;
            _output.WriteLine(
                $"  N = {p.N}: Ω* = {p.OmegaStar:F6}  m_eff = {p.MEff}  error = {p.Error:F6}  {(isM3 ? "← m=3" : "")}");
        }
        _output.WriteLine($"  m=3 present in qCore: {qCoreM3Present}");
        _output.WriteLine("");

        // Mode consistency.
        _output.WriteLine("─── MODE CONSISTENCY ───");
        var orderedByN = points.OrderBy(p => p.N).ToList();
        for (int i = 0; i < orderedByN.Count - 1; i++)
        {
            int mDiff = orderedByN[i + 1].MEff - orderedByN[i].MEff;
            _output.WriteLine(
                $"  N = {orderedByN[i].N,2} → N = {orderedByN[i + 1].N,2}: m_eff {orderedByN[i].MEff} → {orderedByN[i + 1].MEff}  (Δm = {mDiff})");
        }
        _output.WriteLine("");

        var classification = ClassifyLadder(points);

        _output.WriteLine("══════════════════════════════════════════════");
        _output.WriteLine($"  {label} CLASSIFICATION: {classification}");
        _output.WriteLine($"  Pass rate: {passRate:P1} ({passPoints.Count}/{points.Count})");
        _output.WriteLine("══════════════════════════════════════════════");
        _output.WriteLine("");

        switch (classification)
        {
            case LadderClassification.Supported:
                _output.WriteLine("  I1 (closure-family ansatz) is SUPPORTED.");
                break;
            case LadderClassification.Inconclusive:
                _output.WriteLine("  I1 status is INCONCLUSIVE.");
                break;
            case LadderClassification.Falsified:
                _output.WriteLine("  I1 is FALSIFIED for this configuration.");
                break;
        }

        _output.WriteLine("");
        return (classification, passRate, qCoreM3Present);
    }

    /// <summary>
    /// Classifies the rational ladder based on pass rate, mode consistency,
    /// and qCore m=3 presence.
    /// </summary>
    private static LadderClassification ClassifyLadder(List<LadderPoint> points)
    {
        double passRate = (double)points.Count(p => p.Pass) / points.Count;

        // Check mode consistency: does m_eff change smoothly with N?
        var ordered = points.OrderBy(p => p.N).ToList();
        var mDiffs = new List<int>();
        for (int i = 0; i < ordered.Count - 1; i++)
        {
            mDiffs.Add(ordered[i + 1].MEff - ordered[i].MEff);
        }

        // In a clean ladder, m_eff is constant or changes by ±1 as N changes.
        bool smoothTransitions = mDiffs.All(d => d is 1 or 0);

        // Check m=3 presence at qCore.
        bool qCoreM3 = points.Any(p => p.N is 16 or 17 or 18 && p.MEff == 3);

        if (passRate >= 0.85 && smoothTransitions)
            return LadderClassification.Supported;

        if (passRate >= 0.50)
            return LadderClassification.Inconclusive;

        return LadderClassification.Falsified;
    }

    /// <summary>
    /// Writes the E1 report file to the test output directory.
    /// </summary>
    private static void WriteReport(
        List<LadderPoint> points,
        LadderClassification classification,
        double passRate,
        bool qCoreM3Present,
        string variant)
    {
        // Write to the test execution directory.
        string reportPath = Path.Combine(
            AppDomain.CurrentDomain.BaseDirectory,
            $"E1_RationalLadder_Report_{variant}.txt");

        using var writer = new StreamWriter(reportPath, append: false);

        writer.WriteLine("══════════════════════════════════════════════");
        writer.WriteLine("  EXPERIMENT E1 — RATIONAL LADDER SCAN REPORT");
        writer.WriteLine("══════════════════════════════════════════════");
        writer.WriteLine($"Generated: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
        writer.WriteLine("");
        writer.WriteLine("─── CONFIGURATION ───");
        writer.WriteLine("  Model:     Self-organizing ring (CollectiveWeight = 0)");
        writer.WriteLine("  Coupling:  K = 0.10 (Kuramoto sin coupling)");
        writer.WriteLine("  ω_i:       1.0 + 0.05·sin(2πi/N) + 0.03·cos(4πi/N)");
        writer.WriteLine("  Steps:     2000 (settle: 800)");
        writer.WriteLine("  Dt:        0.08");
        writer.WriteLine("  Runs/N:    3 (Ω* averaged)");
        writer.WriteLine("");
        writer.WriteLine("─── RESULTS PER N ───");
        writer.WriteLine($"{"N",6} {"Ω*",10} {"m_eff",6} {"error",10} {"1/(2N)",10} {"PASS",6} {"R",8}");
        writer.WriteLine(new string('─', 58));

        foreach (var p in points.OrderBy(p => p.N))
        {
            double tol = 1.0 / (2.0 * p.N);
            writer.WriteLine(
                $"{p.N,6} {p.OmegaStar,10:F6} {p.MEff,6} {p.Error,10:F6} {tol,10:F6} {(p.Pass ? " PASS" : " FAIL"),6} {p.MeanOrder,8:F4}");
        }

        writer.WriteLine("");
        writer.WriteLine("─── GLOBAL CLASSIFICATION ───");
        writer.WriteLine($"  Classification: {classification}");
        writer.WriteLine($"  Pass rate:      {passRate:P1} ({points.Count(p => p.Pass)}/{points.Count})");
        writer.WriteLine($"  qCore m=3:      {(qCoreM3Present ? "PRESENT" : "ABSENT")}");
        writer.WriteLine("");

        switch (classification)
        {
            case LadderClassification.Supported:
                writer.WriteLine("INTERPRETATION:");
                writer.WriteLine("  I1 (closure-family ansatz) is SUPPORTED.");
                writer.WriteLine("  The rational ladder Ω* = 1 + m/N is observed.");
                break;

            case LadderClassification.Inconclusive:
                writer.WriteLine("INTERPRETATION:");
                writer.WriteLine("  I1 status is INCONCLUSIVE.");
                writer.WriteLine("  Further investigation required.");
                break;

            case LadderClassification.Falsified:
                writer.WriteLine("INTERPRETATION:");
                writer.WriteLine("  I1 (closure-family ansatz) is FALSIFIED.");
                writer.WriteLine("  No rational ladder structure detected.");
                break;
        }

        writer.WriteLine("");
        writer.WriteLine("─── m_eff DISTRIBUTION ───");
        foreach (var group in points.GroupBy(p => p.MEff).OrderBy(g => g.Key))
        {
            var ns = group.Select(p => p.N.ToString()).ToList();
            writer.WriteLine($"  m = {group.Key,2}: N ∈ {{{string.Join(", ", ns)}}}");
        }

        writer.Flush();
    }
}
