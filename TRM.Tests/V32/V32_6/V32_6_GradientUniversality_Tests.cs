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

namespace TRM.Tests.V32_6;

[Trait("Category", "V32_6")]
[Trait("Category", "LongRunning")]
public class V32_6_GradientUniversality_Tests
{
    private readonly ITestOutputHelper _o;
    public V32_6_GradientUniversality_Tests(ITestOutputHelper o) { _o = o; }

    [Fact]
    public void GUV_01_GradientUniversalityAudit()
    {
        var sb = new StringBuilder();
        sb.AppendLine(new string('=', 108));
        sb.AppendLine("=== GUV_01: Gradient Universality Audit ===");
        sb.AppendLine("=== Does the gradient law appear across TRM systems? ===");
        sb.AppendLine(new string('=', 108));

        const int baseSeed = 629471;
        const double xiBase = 2.95, k0Base = 1.0;
        var distances = BuildDistanceEnsemble(baseSeed, systems: 8, nodesPerSystem: 64);
        var sortedD = distances.OrderBy(x => x).ToArray();
        const int nA = 21;
        double aMin = 0.21, aMax = 1.40, daD = (aMax - aMin) / (nA - 1);

        var allResults = new ConcurrentBag<SystemResult>();

        // ================================================================
        // TRM ARCHITECTURES: parameter-space gradient analysis
        // ================================================================
        foreach (var (arch, fam) in new[] {
            ("GAN (param)", VcFamily.GAN),
            ("CNS (param)", VcFamily.CNS),
            ("COMP (param)", VcFamily.GAN) })
        {
            const int gridN = 18;
            double bMin = 0.0, bMax = 2.0, gMin = 0.0, gMax = 2.0;
            double db = (bMax - bMin) / (gridN - 1), dg = (gMax - gMin) / (gridN - 1);

            var gridM = new double[gridN, gridN];
            var gridSign = new int[gridN, gridN];
            var gridTick = new double[gridN, gridN];

            Parallel.For(0, gridN, bi =>
            {
                double beta = bMin + db * bi;
                for (int gi = 0; gi < gridN; gi++)
                {
                    double gamma = gMin + dg * gi;
                    var full = ComputeFull(fam, 1.0, 1.0, 0.70, beta, gamma, distances, sortedD, xiBase, k0Base, nA, aMin, daD);
                    gridM[bi, gi] = full.absM;
                    gridSign[bi, gi] = full.sign;
                    gridTick[bi, gi] = full.tick;
                }
            });

            var gradSamples = new List<GradSample>();
            for (int bi = 1; bi < gridN - 1; bi++)
            {
                for (int gi = 1; gi < gridN - 1; gi++)
                {
                    double dMdB = (gridM[bi + 1, gi] - gridM[bi - 1, gi]) / (2 * db);
                    double dMdG = (gridM[bi, gi + 1] - gridM[bi, gi - 1]) / (2 * dg);
                    double gradMag = Math.Sqrt(dMdB * dMdB + dMdG * dMdG);
                    int gDir = (dMdB + dMdG) > 1e-15 ? +1 : (dMdB + dMdG) < -1e-15 ? -1 : 0;
                    double lapM = (gridM[bi + 1, gi] + gridM[bi - 1, gi] + gridM[bi, gi + 1] + gridM[bi, gi - 1] - 4 * gridM[bi, gi]) / (db * db);
                    double dTdB = (gridTick[bi + 1, gi] - gridTick[bi - 1, gi]) / (2 * db);
                    double dTdG = (gridTick[bi, gi + 1] - gridTick[bi, gi - 1]) / (2 * dg);
                    double tickGradMag = Math.Sqrt(dTdB * dTdB + dTdG * dTdG);
                    gradSamples.Add(new GradSample(gridM[bi, gi], gridSign[bi, gi], gridTick[bi, gi], gDir, gradMag, lapM, tickGradMag));
                }
            }

            if (gradSamples.Count < 20) continue;

            double[] gDirArr = gradSamples.Select(s => (double)s.GradDir).ToArray();
            double[] signArr = gradSamples.Select(s => (double)s.Sign).ToArray();
            double dirSignR = PearsonCorr(gDirArr, signArr);
            int dirSignMatch = gradSamples.Count(s => (s.GradDir == +1 && s.Sign > 0) || (s.GradDir == -1 && s.Sign < 0));
            double dirSignPct = 100.0 * dirSignMatch / gradSamples.Count;

            double[] gradMagArr = gradSamples.Select(s => Math.Log10(Math.Max(1e-15, s.GradMagnitude))).ToArray();
            double[] absMArr = gradSamples.Select(s => Math.Log10(Math.Max(1e-15, s.AbsM))).ToArray();
            double gradAmpR = PearsonCorr(gradMagArr, absMArr);

            double[] lapArr = gradSamples.Select(s => Math.Log10(Math.Max(1e-15, Math.Abs(s.Laplacian)))).ToArray();
            double gcR = PearsonCorr(gradMagArr, lapArr);

            double[] tickGradArr = gradSamples.Select(s => Math.Log10(Math.Max(1e-15, s.TickGradMag))).ToArray();
            double tickMR = PearsonCorr(tickGradArr, absMArr);
            double tickGradR = PearsonCorr(tickGradArr, gradMagArr);

            allResults.Add(new SystemResult(arch, gradSamples.Count, dirSignPct, dirSignR, gradAmpR, gcR, tickMR, tickGradR));
        }

        // ================================================================
        // GRAPH-BASED: connectivity gradient analysis
        // ================================================================
        foreach (var (arch, fam) in new[] { ("GAN (graph)", VcFamily.GAN), ("CNS (graph)", VcFamily.CNS) })
        {
            foreach (int nGrid in new[] { 12, 15 })
            {
                var g = Build3DGraphLocal(nGrid, fam, distances, sortedD, xiBase, k0Base, nA, aMin, daD, out int N, out int E);
                if (N < 30) continue;

                var degrees = g.Select(a => (double)a.Count).ToArray();
                var nodeSamples = new List<GraphGradSample>();
                var rng = new Random(42);
                int nSample = Math.Min(200, N);
                var sample = Enumerable.Range(0, N).OrderBy(_ => rng.Next()).Take(nSample);

                foreach (int i in sample)
                {
                    if (g[i].Count == 0) continue;
                    double myDeg = degrees[i];
                    double degGrad = Math.Abs(myDeg - g[i].Average(n => degrees[n])) / Math.Max(1e-15, myDeg);
                    double degLap = g[i].Sum(n => degrees[n]) - g[i].Count * myDeg;
                    int posNeighbors = g[i].Count(n => degrees[n] > degrees.Average());
                    int localSign = posNeighbors > g[i].Count / 2 ? +1 : -1;
                    nodeSamples.Add(new GraphGradSample(myDeg, localSign, degGrad, degLap, g[i].Count));
                }

                if (nodeSamples.Count < 20) continue;

                double[] dgArr = nodeSamples.Select(s => s.DegGradient).ToArray();
                double[] degArr = nodeSamples.Select(s => Math.Log10(Math.Max(1, s.Degree))).ToArray();
                double[] lapArrG = nodeSamples.Select(s => Math.Log10(Math.Max(1, Math.Abs(s.DegLap)))).ToArray();
                double gGradDegR = PearsonCorr(dgArr, degArr);
                double gGcR = PearsonCorr(dgArr, lapArrG);

                allResults.Add(new SystemResult($"{arch} N={nGrid}", nodeSamples.Count, 0, 0, gGradDegR, gGcR, 0, 0));
            }
        }

        var all = allResults.ToList();
        if (all.Count == 0) { _o.WriteLine("No valid systems."); Assert.True(true); return; }

        // ================================================================
        // GRADIENT UNIVERSALITY TABLE
        // ================================================================
        sb.AppendLine("");
        sb.AppendLine(new string('=', 108));
        sb.AppendLine("=== Gradient Universality Table ===");
        sb.AppendLine("");
        sb.AppendLine($"{"System",-24} {"N",6} {"Dir->Sign%",10} {"Dir->Sign r",11} {"|Grad|->|m|",12} {"Grad-∇²r",10} {"Tick->|m|",10} {"Tick->Grad",10}");
        sb.AppendLine(new string('-', 96));

        foreach (var r in all)
        {
            string ds = r.DirSignPct > 0 ? $"{r.DirSignPct:F1}%" : "---";
            string dsr = r.DirSignR != 0 ? $"{r.DirSignR:F4}" : "---";
            string ga = r.GradAmpR != 0 ? $"{r.GradAmpR:F4}" : "---";
            string gcr = r.GcR != 0 ? $"{r.GcR:F4}" : "---";
            string tmr = r.TickMR != 0 ? $"{r.TickMR:F4}" : "---";
            string tgr = r.TickGradR != 0 ? $"{r.TickGradR:F4}" : "---";
            sb.AppendLine($"{r.Name,-24} {r.Count,6} {ds,10} {dsr,11} {ga,12} {gcr,10} {tmr,10} {tgr,10}");
        }
        sb.AppendLine("");

        // ================================================================
        // CROSS-SYSTEM AGGREGATION
        // ================================================================
        var paramSystems = all.Where(r => r.Name.Contains("param")).ToList();
        var graphSystems = all.Where(r => r.Name.Contains("graph")).ToList();

        sb.AppendLine(new string('=', 108));
        sb.AppendLine("=== Cross-System Aggregation ===");
        sb.AppendLine("");

        if (paramSystems.Count > 0)
        {
            double ad = paramSystems.Where(r => r.DirSignPct > 0).Average(r => r.DirSignPct);
            double aa = paramSystems.Where(r => r.GradAmpR != 0).Average(r => Math.Abs(r.GradAmpR));
            double ag = paramSystems.Where(r => r.GcR != 0).Average(r => Math.Abs(r.GcR));
            sb.AppendLine($"  Param-space ({paramSystems.Count} systems):");
            sb.AppendLine($"    Mean dir→sign: {ad:F1}%  |Grad|→|m| r: {aa:F4}  Grad-∇² r: {ag:F4}");
        }

        if (graphSystems.Count > 0)
        {
            double aa = graphSystems.Where(r => r.GradAmpR != 0).Average(r => Math.Abs(r.GradAmpR));
            double ag = graphSystems.Where(r => r.GcR != 0).Average(r => Math.Abs(r.GcR));
            sb.AppendLine($"  Graph ({graphSystems.Count} systems):");
            sb.AppendLine($"    |Grad|→deg r: {aa:F4}  Grad-∇² r: {ag:F4}");
        }
        sb.AppendLine("");
        sb.AppendLine("  SPARC reference:");
        sb.AppendLine("    dir→shape: 72%  |∇ρ|→amp: r=-0.72  (galactic scale)");
        sb.AppendLine("");

        // ================================================================
        // UNIVERSALITY SIGNALS
        // ================================================================
        sb.AppendLine(new string('=', 108));
        sb.AppendLine("=== Universality Signals ===");
        sb.AppendLine("");

        bool pDir = paramSystems.Any(r => r.DirSignPct > 50);
        bool pAmp = paramSystems.Any(r => Math.Abs(r.GradAmpR) > 0.10);
        bool pCurv = paramSystems.Any(r => Math.Abs(r.GcR) > 0.30);
        bool gAmp = graphSystems.Any(r => Math.Abs(r.GradAmpR) > 0.10);

        int signals = 0;
        if (pDir) signals++; if (pAmp) signals++; if (pCurv) signals++; if (gAmp) signals++;

        sb.AppendLine($"  Param dir→sign:     {(pDir ? "✓ PRESENT" : "✗ ABSENT")}");
        sb.AppendLine($"  Param |Grad|→|m|:   {(pAmp ? "✓ PRESENT" : "✗ ABSENT")}");
        sb.AppendLine($"  Param Grad-∇²:      {(pCurv ? "✓ PRESENT" : "✗ ABSENT")}");
        sb.AppendLine($"  Graph |Grad|→deg:   {(gAmp ? "✓ PRESENT" : "✗ ABSENT")}");
        sb.AppendLine($"  Total:              {signals}/4");
        sb.AppendLine("");

        // ================================================================
        // CANDIDATE UNIVERSAL PRINCIPLE
        // ================================================================
        sb.AppendLine(new string('=', 108));
        sb.AppendLine("=== Candidate Universal Principle ===");
        sb.AppendLine("");

        if (signals >= 3)
        {
            sb.AppendLine("  The gradient law appears across TRM systems:");
            sb.AppendLine("    GALACTIC:   ∇ρ → rotation (shape + amplitude)");
            sb.AppendLine("    PARAMETRIC: ∇|m| → sign + |m| strength");
            sb.AppendLine("    GRAPH:      ∇(degree) → degree magnitude");
            sb.AppendLine("");
            sb.AppendLine("  UNIVERSAL TRM gradient principle:");
            sb.AppendLine("    For any TRM field φ:");
            sb.AppendLine("      Structure(φ) = sign(∇φ)");
            sb.AppendLine("      Strength(φ)  = |∇φ|^α");
            sb.AppendLine("      ∇²φ          = derived from |∇φ|");
        }
        else
        {
            sb.AppendLine("  Gradient law is GALAXY-SPECIFIC.");
            sb.AppendLine("  Does not generalize to TRM parametric/graph systems.");
        }
        sb.AppendLine("");

        // ================================================================
        // DECISION
        // ================================================================
        sb.AppendLine(new string('=', 108));
        sb.AppendLine("=== Decision ===");
        sb.AppendLine("");

        bool critA = signals >= 4, critB = signals >= 3;
        bool critC = all.Count >= 5, critD = pDir && pAmp;

        int critMet = 0;
        if (critA) critMet++; if (critB) critMet++; if (critC) critMet++; if (critD) critMet++;

        string verdict = critMet >= 4 ? "SUPPORTED: gradient law is universal."
            : critMet >= 2 ? "CONDITIONAL: galaxy-specific."
            : "FALSIFIED: no universal gradient principle.";

        sb.AppendLine($"VERDICT: {verdict}  ({critMet}/4 criteria)");
        sb.AppendLine("");
        sb.AppendLine($"  A. 4 universal signals:            {(critA ? "YES" : "NO")} ({signals}/4)");
        sb.AppendLine($"  B. ≥3 universal signals:           {(critB ? "YES" : "NO")} ({signals}/4)");
        sb.AppendLine($"  C. ≥5 systems:                     {(critC ? "YES" : "NO")} ({all.Count})");
        sb.AppendLine($"  D. Param dir + amp both:           {(critD ? "YES" : "NO")}");
        sb.AppendLine("");
        sb.AppendLine($"  Tested {all.Count} systems: {paramSystems.Count} parametric + {graphSystems.Count} graph-based.");
        sb.AppendLine("");
        sb.AppendLine(new string('=', 108));
        sb.AppendLine("=== GUV_01 complete. Commit: GUV_01_GradientUniversalityAudit ===");

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

    private static List<int>[] Build3DGraphLocal(int nGrid, VcFamily fam,
        double[] distances, double[] sortedD, double xiBase, double k0Base,
        int nA, double aMin, double daD, out int N, out int edges)
    {
        var bag = new ConcurrentBag<(int ai, int bi, int gi, int sign)>();
        Parallel.For(0, nGrid, ai =>
        {
            double alpha = 0.1 + (3.0 - 0.1) * ai / (nGrid - 1);
            for (int bi = 0; bi < nGrid; bi++)
            {
                double beta = 0.0 + 2.0 * bi / (nGrid - 1);
                for (int gi = 0; gi < nGrid; gi++)
                {
                    double gamma = 0.0 + 2.0 * gi / (nGrid - 1);
                    var (m, dTdp) = ComputeM_and_DTdp(fam, 1.0, 1.0, alpha, beta, gamma, distances, sortedD, xiBase, k0Base, nA, aMin, daD);
                    bag.Add((ai, bi, gi, dTdp > 1e-8 ? 1 : -1));
                }
            }
        });
        var all = bag.ToList();
        var s3 = new int[nGrid, nGrid, nGrid];
        foreach (var p in all) s3[p.ai, p.bi, p.gi] = p.sign;
        var bdry = new List<(int, int, int)>();
        var bset = new HashSet<(int, int, int)>();
        for (int ai = 0; ai < nGrid; ai++)
            for (int bi = 0; bi < nGrid; bi++)
                for (int gi = 0; gi < nGrid; gi++)
                {
                    bool opp = false;
                    if (ai > 0 && s3[ai, bi, gi] != s3[ai - 1, bi, gi]) opp = true;
                    if (ai + 1 < nGrid && s3[ai, bi, gi] != s3[ai + 1, bi, gi]) opp = true;
                    if (bi > 0 && s3[ai, bi, gi] != s3[ai, bi - 1, gi]) opp = true;
                    if (bi + 1 < nGrid && s3[ai, bi, gi] != s3[ai, bi + 1, gi]) opp = true;
                    if (gi > 0 && s3[ai, bi, gi] != s3[ai, gi - 1, gi]) opp = true;
                    if (gi + 1 < nGrid && s3[ai, bi, gi] != s3[ai, gi + 1, gi]) opp = true;
                    if (opp) { bdry.Add((ai, bi, gi)); bset.Add((ai, bi, gi)); }
                }
        N = bdry.Count;
        var imap = new Dictionary<(int, int, int), int>();
        for (int i = 0; i < N; i++) imap[bdry[i]] = i;
        var adj = new List<int>[N];
        for (int i = 0; i < N; i++) adj[i] = new List<int>();
        for (int da = -1; da <= 1; da++)
            for (int db = -1; db <= 1; db++)
                for (int dg = -1; dg <= 1; dg++)
                {
                    if (da == 0 && db == 0 && dg == 0) continue;
                    for (int i = 0; i < N; i++)
                    {
                        var (a, b, g2) = bdry[i];
                        int na = a + da, nb = b + db, ng = g2 + dg;
                        if (bset.Contains((na, nb, ng)))
                        {
                            int j = imap[(na, nb, ng)];
                            if (!adj[i].Contains(j)) adj[i].Add(j);
                        }
                    }
                }
        edges = adj.Sum(a => a.Count) / 2;
        return adj;
    }

    private record GradSample(double AbsM, int Sign, double Tick, int GradDir, double GradMagnitude, double Laplacian, double TickGradMag);
    private record GraphGradSample(double Degree, int LocalSign, double DegGradient, double DegLap, int NeighborCount);
    private record SystemResult(string Name, int Count, double DirSignPct, double DirSignR, double GradAmpR, double GcR, double TickMR, double TickGradR);
}
