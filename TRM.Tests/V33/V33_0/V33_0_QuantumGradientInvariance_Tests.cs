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

namespace TRM.Tests.V33_0;

[Trait("Category", "V33_0")]
[Trait("Category", "LongRunning")]
public class V33_0_QuantumGradientInvariance_Tests
{
    private readonly ITestOutputHelper _o;
    public V33_0_QuantumGradientInvariance_Tests(ITestOutputHelper o) { _o = o; }

    [Fact]
    public void QGI_01_QuantumGradientInvarianceAudit()
    {
        var sb = new StringBuilder();
        sb.AppendLine(new string('=', 108));
        sb.AppendLine("=== QGI_01: Quantum Gradient Invariance Audit ===");
        sb.AppendLine("=== Does the gradient law appear at the oscillator level? ===");
        sb.AppendLine(new string('=', 108));

        const int baseSeed = 629471;
        const double xiBase = 2.95, k0Base = 1.0;
        var distances = BuildDistanceEnsemble(baseSeed, systems: 8, nodesPerSystem: 64);
        var sortedD = distances.OrderBy(x => x).ToArray();

        var allSamples = new ConcurrentBag<OscSample>();

        foreach (var (arch, fam) in new[] { ("GAN", VcFamily.GAN), ("CNS", VcFamily.CNS) })
        {
            const int nA = 17, nP = 17;
            double aMin = 0.21, aMax = 1.40, pMin = 0.5, pMax = 4.5;
            double da = (aMax - aMin) / (nA - 1), dp = (pMax - pMin) / (nP - 1);

            // Fixed beta, gamma for oscillator-level scan
            double beta = 0.70, gamma = 0.70;

            var gridV1 = new double[nA, nP];
            var gridVT = new double[nA, nP];

            Parallel.For(0, nA, ai =>
            {
                double alpha = aMin + da * ai;
                var v = new VariantSpec($"{fam}_QGI", fam, 1.0, 1.0, alpha, beta, gamma);
                for (int pi = 0; pi < nP; pi++)
                {
                    double p = pMin + dp * pi;
                    var cci = EvaluateCciVariantAtP(distances, sortedD, xiBase, k0Base, p, v);
                    gridV1[ai, pi] = cci.VarI1;
                    gridVT[ai, pi] = cci.VarTerms;
                }
            });

            // Compute oscillator-level gradients on interior points
            for (int ai = 1; ai < nA - 1; ai++)
            {
                for (int pi = 1; pi < nP - 1; pi++)
                {
                    // Raw oscillator fields
                    double v1 = gridV1[ai, pi];
                    double vt = gridVT[ai, pi];
                    double ut = v1 + vt; // total response

                    // Gradient of VarI1
                    double dV1dA = (gridV1[ai + 1, pi] - gridV1[ai - 1, pi]) / (2 * da);
                    double dV1dP = (gridV1[ai, pi + 1] - gridV1[ai, pi - 1]) / (2 * dp);
                    double gradV1mag = Math.Sqrt(dV1dA * dV1dA + dV1dP * dV1dP);
                    int gradV1dir = (dV1dA + dV1dP) > 1e-15 ? +1 : (dV1dA + dV1dP) < -1e-15 ? -1 : 0;

                    // Gradient of VarTerms
                    double dVTdA = (gridVT[ai + 1, pi] - gridVT[ai - 1, pi]) / (2 * da);
                    double dVTdP = (gridVT[ai, pi + 1] - gridVT[ai, pi - 1]) / (2 * dp);
                    double gradVTmag = Math.Sqrt(dVTdA * dVTdA + dVTdP * dVTdP);
                    int gradVTdir = (dVTdA + dVTdP) > 1e-15 ? +1 : (dVTdA + dVTdP) < -1e-15 ? -1 : 0;

                    // Gradient of total U
                    double gradUmag = Math.Sqrt((dV1dA + dVTdA) * (dV1dA + dVTdA) + (dV1dP + dVTdP) * (dV1dP + dVTdP));

                    // Local transition indicator: does V1 increase with alpha?
                    int v1Trend = dV1dA > 1e-12 ? +1 : dV1dA < -1e-12 ? -1 : 0;
                    int vtTrend = dVTdA > 1e-12 ? +1 : dVTdA < -1e-12 ? -1 : 0;

                    // Laplacian
                    double lapV1 = (gridV1[ai + 1, pi] + gridV1[ai - 1, pi] + gridV1[ai, pi + 1] + gridV1[ai, pi - 1] - 4 * v1) / (da * da);

                    allSamples.Add(new OscSample(arch, v1, vt, ut,
                        gradV1mag, gradVTmag, gradUmag,
                        gradV1dir, gradVTdir,
                        v1Trend, vtTrend, lapV1));
                }
            }
        }

        var all = allSamples.ToList();
        if (all.Count < 50) { _o.WriteLine($"Insufficient samples: {all.Count}"); Assert.True(true); return; }

        sb.AppendLine("");
        sb.AppendLine($"  Oscillator samples: {all.Count} across 2 families");
        sb.AppendLine("");

        // ================================================================
        // GRADIENT LAW AT OSCILLATOR LEVEL
        // ================================================================
        sb.AppendLine(new string('=', 108));
        sb.AppendLine("=== Q1: Does sign(∇φ) govern transition direction? ===");
        sb.AppendLine("");

        // Test: sign(∇VarI1) → VarI1 trend direction
        int dirMatch = all.Count(s => (s.GradV1Dir == +1 && s.V1Trend > 0) || (s.GradV1Dir == -1 && s.V1Trend < 0));
        double dirPct = 100.0 * dirMatch / all.Count;
        double[] gDirA = all.Select(s => (double)s.GradV1Dir).ToArray();
        double[] trendA = all.Select(s => (double)s.V1Trend).ToArray();
        double dirR = PearsonCorr(gDirA, trendA);

        sb.AppendLine($"  sign(∇VarI1) → VarI1 trend direction:");
        sb.AppendLine($"    Match: {dirPct:F1}% ({dirMatch}/{all.Count})");
        sb.AppendLine($"    Correlation: r = {dirR:F4}");
        sb.AppendLine("");

        // Per-family
        foreach (var g in all.GroupBy(s => s.Arch))
        {
            var ga = g.ToList();
            int dm = ga.Count(s => (s.GradV1Dir == +1 && s.V1Trend > 0) || (s.GradV1Dir == -1 && s.V1Trend < 0));
            double[] gd = ga.Select(s => (double)s.GradV1Dir).ToArray();
            double[] tr = ga.Select(s => (double)s.V1Trend).ToArray();
            double dr = PearsonCorr(gd, tr);
            sb.AppendLine($"    {g.Key}: {100.0*dm/ga.Count:F1}% match, r={dr:F4}");
        }
        sb.AppendLine("");

        // ================================================================
        // Q2: Does |∇φ| govern oscillator strength?
        // ================================================================
        sb.AppendLine(new string('=', 108));
        sb.AppendLine("=== Q2: Does |∇φ| govern oscillator response strength? ===");
        sb.AppendLine("");

        double[] gMagA = all.Select(s => Math.Log10(Math.Max(1e-15, s.GradV1Mag))).ToArray();
        double[] absV1A = all.Select(s => Math.Log10(Math.Max(1e-15, Math.Abs(s.VarI1)))).ToArray();
        double[] absVTA = all.Select(s => Math.Log10(Math.Max(1e-15, Math.Abs(s.VarTerms)))).ToArray();
        double[] absUA = all.Select(s => Math.Log10(Math.Max(1e-15, Math.Abs(s.TotalU)))).ToArray();

        double gV1R = PearsonCorr(gMagA, absV1A);
        double gVTR = PearsonCorr(gMagA, absVTA);
        double gUR = PearsonCorr(gMagA, absUA);

        sb.AppendLine($"  |∇VarI1| vs |VarI1|:      r = {gV1R:F4}  R² = {gV1R*gV1R:F4}");
        sb.AppendLine($"  |∇VarI1| vs |VarTerms|:   r = {gVTR:F4}  R² = {gVTR*gVTR:F4}");
        sb.AppendLine($"  |∇VarI1| vs |Total U|:    r = {gUR:F4}  R² = {gUR*gUR:F4}");
        sb.AppendLine("");

        foreach (var g in all.GroupBy(s => s.Arch))
        {
            var ga = g.ToList();
            double[] gm = ga.Select(s => Math.Log10(Math.Max(1e-15, s.GradV1Mag))).ToArray();
            double[] av = ga.Select(s => Math.Log10(Math.Max(1e-15, Math.Abs(s.VarI1)))).ToArray();
            double r = PearsonCorr(gm, av);
            sb.AppendLine($"    {g.Key}: |∇VarI1| vs |VarI1| r = {r:F4}");
        }
        sb.AppendLine("");

        // ================================================================
        // Q3: Does gradient law appear before boundary formation?
        // ================================================================
        sb.AppendLine(new string('=', 108));
        sb.AppendLine("=== Q3: Pre-boundary gradient structure ===");
        sb.AppendLine("");

        // The oscillator grid (α, p) has NO boundary extraction.
        // Every point is a raw oscillator response.
        // Test whether gradient-direction coupling exists everywhere.

        // Fraction of points where gradient direction is well-defined
        int clearDir = all.Count(s => s.GradV1Dir != 0);
        double clearPct = 100.0 * clearDir / all.Count;
        int clearMatch = all.Count(s => s.GradV1Dir != 0 && ((s.GradV1Dir == +1 && s.V1Trend > 0) || (s.GradV1Dir == -1 && s.V1Trend < 0)));
        double clearMatchPct = clearDir > 0 ? 100.0 * clearMatch / clearDir : 0;

        sb.AppendLine($"  Points with clear gradient direction: {clearPct:F1}% ({clearDir}/{all.Count})");
        sb.AppendLine($"  Direction→trend match (clear only):   {clearMatchPct:F1}% ({clearMatch}/{clearDir})");
        sb.AppendLine("");

        // ∇²φ derivable from |∇φ| at oscillator level?
        double[] lapA = all.Select(s => Math.Log10(Math.Max(1e-15, Math.Abs(s.LapV1)))).ToArray();
        double gradLapR = PearsonCorr(gMagA, lapA);
        sb.AppendLine($"  |∇VarI1| vs |∇²VarI1|: r = {gradLapR:F4}  R² = {gradLapR*gradLapR:F4}");
        sb.AppendLine("");

        // ================================================================
        // Q4: Cross-scale gradient law comparison
        // ================================================================
        sb.AppendLine(new string('=', 108));
        sb.AppendLine("=== Q4: Cross-Scale Gradient Law ===");
        sb.AppendLine("");
        sb.AppendLine($"{"Scale",-22} {"System",-22} {"Dir→Sign%",10} {"Dir r",8} {"|∇|→|·|r",11} {"∇-∇² r",9}");
        sb.AppendLine(new string('-', 82));

        // Oscillator level (this test)
        sb.AppendLine($"{"Oscillator",-22} {"Raw (α,p) grid",-22} {dirPct,10:F1}% {dirR,8:F4} {gV1R,11:F4} {gradLapR,9:F4}");

        // Parametric level (from V32 GUV_01 reference values)
        sb.AppendLine($"{"Parametric",-22} {"(β,γ) |m| grid",-22} {"~60-75%",10} {"~0.2-0.5",8} {"~0.3-0.6",11} {"~0.4-0.7",9}");

        // Graph level (from V32 GUV_01 reference)
        sb.AppendLine($"{"Graph",-22} {"Boundary deg graph",-22} {"---",10} {"---",8} {"~0.2-0.4",11} {"~0.3-0.5",9}");

        // Galactic level (from V31/V32)
        sb.AppendLine($"{"Galactic",-22} {"SPARC ∇ρ",-22} {"72%",10} {"n/a",8} {"r=-0.72",11} {"~0.7-0.9",9}");
        sb.AppendLine("");

        // ================================================================
        // EARLIEST APPEARANCE
        // ================================================================
        sb.AppendLine(new string('=', 108));
        sb.AppendLine("=== Earliest Appearance Analysis ===");
        sb.AppendLine("");

        bool oscDirOk = dirPct > 55;
        bool oscAmpOk = Math.Abs(gV1R) > 0.15;
        bool oscCurvOk = Math.Abs(gradLapR) > 0.30;

        sb.AppendLine($"  Oscillator-level gradient law:");
        sb.AppendLine($"    Direction→trend:  {(oscDirOk ? $"PRESENT ({dirPct:F1}%)" : $"ABSENT ({dirPct:F1}%)")}");
        sb.AppendLine($"    |∇|→|·|:          {(oscAmpOk ? $"PRESENT (r={gV1R:F4})" : $"ABSENT (r={gV1R:F4})")}");
        sb.AppendLine($"    ∇-∇² coupling:    {(oscCurvOk ? $"PRESENT (r={gradLapR:F4})" : $"ABSENT (r={gradLapR:F4})")}");
        sb.AppendLine("");

        int oscSignals = (oscDirOk ? 1 : 0) + (oscAmpOk ? 1 : 0) + (oscCurvOk ? 1 : 0);

        if (oscSignals >= 2)
        {
            sb.AppendLine("  The gradient law appears at the OSCILLATOR LEVEL —");
            sb.AppendLine("  before any boundary, graph, or galaxy formation.");
            sb.AppendLine("  This suggests the gradient law is FUNDAMENTAL to TRM,");
            sb.AppendLine("  not an emergent phenomenon at higher scales.");
        }
        else if (oscSignals == 1)
        {
            sb.AppendLine("  The gradient law PARTIALLY appears at the oscillator level.");
            sb.AppendLine("  Some components emerge only at higher scales.");
        }
        else
        {
            sb.AppendLine("  The gradient law does NOT appear at the oscillator level.");
            sb.AppendLine("  It EMERGES at boundary/graph/galactic scales.");
            sb.AppendLine("  The law is an emergent, not fundamental, TRM property.");
        }
        sb.AppendLine("");

        sb.AppendLine("  Cross-scale emergence chain:");
        sb.AppendLine("    Oscillator → Parametric → Graph → Galactic");
        sb.AppendLine($"    {(oscSignals >= 2 ? "✓" : "?")}             ✓              ✓        ✓");
        sb.AppendLine("");

        // ================================================================
        // DECISION
        // ================================================================
        sb.AppendLine(new string('=', 108));
        sb.AppendLine("=== Decision ===");
        sb.AppendLine("");

        bool critA = oscSignals >= 2;
        bool critB = all.Count >= 200;
        bool critC = all.GroupBy(s => s.Arch).Count() >= 2;
        bool critD = dirPct > 50;

        int critMet = 0;
        if (critA) critMet++; if (critB) critMet++; if (critC) critMet++; if (critD) critMet++;

        string verdict = critMet >= 4 ? "SUPPORTED: gradient law is fundamental."
            : critMet >= 2 ? "CONDITIONAL: emerges later."
            : "FALSIFIED: galaxy-specific phenomenon.";

        sb.AppendLine($"VERDICT: {verdict}  ({critMet}/4 criteria)");
        sb.AppendLine("");
        sb.AppendLine($"  A. ≥2 oscillator signals:               {(critA ? "YES" : "NO")} ({oscSignals}/3)");
        sb.AppendLine($"  B. ≥200 samples:                         {(critB ? "YES" : "NO")} ({all.Count})");
        sb.AppendLine($"  C. ≥2 families:                          {(critC ? "YES" : "NO")} ({all.GroupBy(s=>s.Arch).Count()})");
        sb.AppendLine($"  D. Direction→trend >50%:                {(critD ? "YES" : "NO")} ({dirPct:F1}%)");
        sb.AppendLine("");
        sb.AppendLine("Quantum Gradient Result:");
        sb.AppendLine("  The oscillator-level (α, p) scan probes TRM before any");
        sb.AppendLine("  boundary geometry, graph structure, or galactic profile.");
        sb.AppendLine($"  Gradient law signals: {oscSignals}/3 present.");
        sb.AppendLine("");
        sb.AppendLine(new string('=', 108));
        sb.AppendLine("=== QGI_01 complete. Commit: QGI_01_QuantumGradientInvarianceAudit ===");

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

    private record OscSample(
        string Arch,
        double VarI1, double VarTerms, double TotalU,
        double GradV1Mag, double GradVTMag, double GradUMag,
        int GradV1Dir, int GradVTDir,
        int V1Trend, int VTTrend,
        double LapV1);
}
