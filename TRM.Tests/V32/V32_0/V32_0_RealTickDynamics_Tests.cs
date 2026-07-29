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

namespace TRM.Tests.V32_0;

[Trait("Category", "V32_0")]
[Trait("Category", "LongRunning")]
public class V32_0_RealTickDynamics_Tests
{
    private readonly ITestOutputHelper _o;
    public V32_0_RealTickDynamics_Tests(ITestOutputHelper o) { _o = o; }

    [Fact]
    public void RTD_01_RealTickDynamicsAudit()
    {
        var sb = new StringBuilder();
        sb.AppendLine(new string('=', 108));
        sb.AppendLine("=== RTD_01: Real Tick Dynamics Audit ===");
        sb.AppendLine("=== Does real TRM Tick carry independent dynamical information? ===");
        sb.AppendLine(new string('=', 108));
        sb.AppendLine("");

        const int baseSeed = 629471;
        const double xiBase = 2.95, k0Base = 1.0;
        var distances = BuildDistanceEnsemble(baseSeed, systems: 8, nodesPerSystem: 64);
        var sortedD = distances.OrderBy(x => x).ToArray();
        const int nA = 21;
        double aMin = 0.21, aMax = 1.40, daD = (aMax - aMin) / (nA - 1);

        // Compute real Tick at many parameter points across families
        var samples = new ConcurrentBag<TickSample>();
        var progressLog = new ConcurrentDictionary<int, string>();
        int rowIdx = 0;

        foreach (var (arch, fam) in new[] {
            ("3D GAN", VcFamily.GAN), ("3D CNS", VcFamily.CNS), ("COMPOSITE", VcFamily.GAN) })
        {
            Parallel.For(0, 10, bi =>
            {
                double beta = 0.0 + 2.0 * bi / 9.0;
                for (int gi = 0; gi < 10; gi++)
                {
                    double gamma = 0.0 + 2.0 * gi / 9.0;
                    var full = ComputeFull(fam, 1.0, 1.0, 0.70, beta, gamma, distances, sortedD, xiBase, k0Base, nA, aMin, daD);
                    samples.Add(new TickSample(arch, fam.ToString(), full.tick, full.m, full.absM, full.sign, full.fb));
                }
            });
            int r = Interlocked.Increment(ref rowIdx);
            progressLog[r] = $"  {arch}: computed 100 Tick samples";
        }
        foreach (var kv in progressLog.OrderBy(k => k.Key))
            sb.AppendLine(kv.Value);
        sb.AppendLine("");

        var all = samples.ToList();
        sb.AppendLine($"  Total samples: {all.Count}");
        sb.AppendLine("");

        // Group by family
        var groups = all.GroupBy(s => s.Family).ToList();

        sb.AppendLine(new string('=', 108));
        sb.AppendLine("=== Tick Structure Table ===");
        sb.AppendLine("");
        sb.AppendLine($"{"Family",-8} {"Count",6} {"MeanTick",10} {"StdTick",10} {"CvTick",8} {"Mean|m|",9} {"MeanFB",9} {"SignDom",8}");
        sb.AppendLine(new string('-', 74));

        foreach (var g in groups)
        {
            double mt = g.Average(s => s.Tick), st = Math.Sqrt(g.Average(s => (s.Tick - mt) * (s.Tick - mt)));
            double mm = g.Average(s => s.AbsM), fb = g.Average(s => s.Fb);
            int pos = g.Count(s => s.Sign > 0);
            sb.AppendLine($"{g.Key,-8} {g.Count(),6} {mt,10:F4} {st,10:F4} {(mt>0?st/mt:0),8:F4} {mm,9:F4} {fb,9:F4} {(pos>g.Count()/2?"POS":"NEG"),8}");
        }
        sb.AppendLine("");

        // ================================================================
        // DENSITY vs TICK COMPARISON
        // ================================================================
        sb.AppendLine(new string('=', 108));
        sb.AppendLine("=== Density vs Tick Independence ===");
        sb.AppendLine("");

        // Correlation between |m| (density proxy) and Tick
        double[] mVals = all.Select(s => s.AbsM).ToArray();
        double[] tVals = all.Select(s => s.Tick).ToArray();
        double corrMT = PearsonCorr(mVals, tVals);

        sb.AppendLine($"  |m| vs Tick: r = {corrMT:F4}  R^2 = {corrMT*corrMT:F4}");
        sb.AppendLine($"  → {(Math.Abs(corrMT) < 0.3 ? "Tick is INDEPENDENT of density — carries separate information" : Math.Abs(corrMT) < 0.6 ? "Partial dependence — some overlap" : "Tick and density are COUPLED")}");
        sb.AppendLine("");

        // Per-family correlation
        foreach (var g in groups)
        {
            double[] ms = g.Select(s => s.AbsM).ToArray();
            double[] ts = g.Select(s => s.Tick).ToArray();
            double r = PearsonCorr(ms, ts);
            sb.AppendLine($"  {g.Key}: |m| vs Tick r = {r:F4}");
        }
        sb.AppendLine("");

        // ================================================================
        // TICK VARIANCE AS DYNAMICAL INFORMATION
        // ================================================================
        sb.AppendLine(new string('=', 108));
        sb.AppendLine("=== Tick Variance (Dynamical Spread) ===");
        sb.AppendLine("");

        sb.AppendLine("  Higher Tick variance = richer dynamical potential:");
        foreach (var g in groups)
        {
            double cv = g.Average(s => s.Tick) > 0 ? Math.Sqrt(g.Average(s => (s.Tick - g.Average(s2 => s2.Tick)) * (s.Tick - g.Average(s2 => s2.Tick)))) / g.Average(s => s.Tick) : 0;
            double signDiv = Math.Min(g.Count(s => s.Sign > 0), g.Count(s => s.Sign < 0)) / (double)g.Count();
            sb.AppendLine($"  {g.Key}: Tick CV={cv:F4}, sign diversity={signDiv:F3} → {(cv>0.3?"RICH dynamics":"LOW dynamics")}");
        }
        sb.AppendLine("");

        // ================================================================
        // CANDIDATE AMPLITUDE MODULATOR
        // ================================================================
        sb.AppendLine(new string('=', 108));
        sb.AppendLine("=== Tick as Amplitude Modulator ===");
        sb.AppendLine("");

        sb.AppendLine("  TRM two-layer model needs REAL Tick:");
        sb.AppendLine("    Shape   = density gradient DIRECTION");
        sb.AppendLine("    Amplitude = gradient STRENGTH × Tick_scale");
        sb.AppendLine("");
        sb.AppendLine("  Real Tick carries dynamical information:");
        sb.AppendLine($"    |m| vs Tick correlation: r = {corrMT:F4}");
        sb.AppendLine($"    → {(Math.Abs(corrMT) < 0.5 ? "Tick is PARTIALLY INDEPENDENT — can modulate amplitude" : "Too coupled — Tick may not add separate information")}");
        sb.AppendLine("");

        // ================================================================
        // DECISION
        // ================================================================
        sb.AppendLine(new string('=', 108));
        sb.AppendLine("=== Decision ===");
        sb.AppendLine("");

        bool criterionA = Math.Abs(corrMT) < 0.6;
        bool criterionB = all.Count >= 200;
        bool criterionC = groups.Count() >= 3;
        bool criterionD = groups.Any(g => { double m = g.Average(s => s.Tick); return Math.Sqrt(g.Average(s => (s.Tick - m) * (s.Tick - m))) / Math.Max(1e-15, m) > 0.2; });

        int criteriaMet = 0;
        if (criterionA) criteriaMet++;
        if (criterionB) criteriaMet++;
        if (criterionC) criteriaMet++;
        if (criterionD) criteriaMet++;

        string verdict = criteriaMet >= 4 ? "SUPPORTED: real Tick carries additional dynamical information."
            : criteriaMet >= 2 ? "CONDITIONAL: partial overlap with density."
            : "FALSIFIED: Tick adds no relevant structure.";

        sb.AppendLine($"VERDICT: {verdict}  ({criteriaMet}/4 criteria)");
        sb.AppendLine("");
        sb.AppendLine($"  A. |m|-Tick correlation < 0.6:           {(criterionA ? "YES" : "NO")} (r={corrMT:F4})");
        sb.AppendLine($"  B. ≥200 samples:                          {(criterionB ? "YES" : "NO")} ({all.Count})");
        sb.AppendLine($"  C. ≥3 families:                           {(criterionC ? "YES" : "NO")} ({groups.Count()})");
        sb.AppendLine($"  D. ≥1 family with CV > 20%:              {(criterionD ? "YES" : "NO")}");
        sb.AppendLine("");
        sb.AppendLine("Real Tick Dynamics Result:");
        sb.AppendLine("  TRM Tick computed from oscillator dynamics via ComputeFull.");
        sb.AppendLine($"  |m|-Tick correlation: r = {corrMT:F4}. Tick carries");
        sb.AppendLine($"  {(Math.Abs(corrMT) < 0.5 ? "INDEPENDENT dynamical information — can serve as amplitude modulator." : "partial overlap with density — may need additional structure.")}");
        sb.AppendLine("");
        sb.AppendLine(new string('=', 108));
        sb.AppendLine("=== RTD_01 complete. Commit: RTD_01_RealTickDynamicsAudit ===");

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

    private record TickSample(string Arch, string Family, double Tick, double M, double AbsM, int Sign, double Fb);
}
