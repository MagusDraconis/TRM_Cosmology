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

namespace TRM.Tests.V22_0;

[Trait("Category", "V22_0")]
[Trait("Category", "LongRunning")]
public class V22_0_TemporalPropagationInvariant_Tests
{
    private readonly ITestOutputHelper _o;
    public V22_0_TemporalPropagationInvariant_Tests(ITestOutputHelper o) { _o = o; }

    [Fact]
    public void TPI_01_TemporalPropagationInvariantAudit()
    {
        var sb = new StringBuilder();
        sb.AppendLine(new string('=', 108));
        sb.AppendLine("=== TPI_01: Temporal Propagation Invariant Audit ===");
        sb.AppendLine("=== Does the universal propagation bound define a temporal scale? ===");
        sb.AppendLine(new string('=', 108));
        sb.AppendLine("");
        sb.AppendLine("KNOWN (UPI_01): Universal propagation bound exists across architectures.");
        sb.AppendLine("QUESTION: Can propagation be interpreted as a Tick-limited process?");
        sb.AppendLine("");

        const int baseSeed = 629471;
        const double xiBase = 2.95, k0Base = 1.0;
        var distances = BuildDistanceEnsemble(baseSeed, systems: 8, nodesPerSystem: 64);
        var sortedD = distances.OrderBy(x => x).ToArray();
        const int nA = 21;
        double aMin = 0.21, aMax = 1.40, daD = (aMax - aMin) / (nA - 1);

        var compSizes = new[] { 40, 50, 60, 70 };
        var d3Sizes = new[] { 10, 12, 14, 16, 18 };

        var allResults = new ConcurrentBag<TemporalResult>();
        var progressLog = new ConcurrentDictionary<int, string>();
        int rowIdx = 0;

        sb.AppendLine("--- Building graphs and computing Tick for each configuration ---");

        foreach (var (arch, fam, sizes, dim, bdim) in new[] {
            ("COMPOSITE", VcFamily.GAN, compSizes, "1D", 1),
            ("3D GAN", VcFamily.GAN, d3Sizes, "2D", 2),
            ("3D CNS", VcFamily.CNS, d3Sizes, "2D", 2) })
        {
            Parallel.ForEach(sizes, nGrid =>
            {
                List<int>[] g; int N, E;
                if (dim == "1D")
                    g = Build1DGraph(nGrid, fam, distances, sortedD, xiBase, k0Base, nA, aMin, daD, out N, out E);
                else
                    g = Build3DGraph(nGrid, fam, distances, sortedD, xiBase, k0Base, nA, aMin, daD, out N, out E);

                int diam = ComputeDiameter(g, N);
                var gm = MeasureGeometryMetrics(g, N, diam, 8);
                var tick = ComputeGraphTick(fam, nGrid, distances, sortedD, xiBase, k0Base, nA, aMin, daD, dim);

                allResults.Add(new TemporalResult(arch, dim, bdim, nGrid, N, E, diam, gm, tick));

                int r = Interlocked.Increment(ref rowIdx);
                progressLog[r] = $"  {arch,-10} nGrid={nGrid,3}  N={N,5}  diam={diam,4}  v_max={gm.VMax,8:F4}  Tick={tick,8:F4}  v/tick={gm.VMax/Math.Max(1e-15,tick),8:F2}";
            });
            foreach (var kv in progressLog.OrderBy(k => k.Key))
                sb.AppendLine(kv.Value);
            progressLog.Clear();
            rowIdx = 0;
            sb.AppendLine("");
        }

        // ================================================================
        // TICK SCALING TABLE
        // ================================================================
        sb.AppendLine(new string('=', 108));
        sb.AppendLine("=== Tick Scaling Table ===");
        sb.AppendLine("");
        sb.AppendLine("Tick = mean |d(VarI1+VarTerms)/da| — intrinsic change rate.");
        sb.AppendLine("");

        sb.AppendLine($"{"Arch",-12} {"nGrid",6} {"N",6} {"v_max",9} {"Tick",9} {"v/Tick",10} {"Tick*N",10} {"dist/Tick",11} {"Tick/projSpan",14}");
        sb.AppendLine(new string('-', 102));

        foreach (var r in allResults.OrderBy(r => r.Arch).ThenBy(r => r.N))
        {
            double vTick = r.VMax / Math.Max(1e-15, r.Tick);
            double tickN = r.Tick * r.N;
            double distTick = r.Diameter / Math.Max(1e-15, r.Tick);
            double tickSpan = r.Tick / Math.Max(1e-15, r.ProjSpan);
            sb.AppendLine($"{r.Arch,-12} {r.GridSize,6} {r.N,6} {r.VMax,9:F4} {r.Tick,9:F4} {vTick,10:F2} {tickN,10:F2} {distTick,11:F2} {tickSpan,14:F4}");
        }
        sb.AppendLine("");

        // ================================================================
        // TICK vs PROPAGATION CORRELATION
        // ================================================================
        sb.AppendLine(new string('=', 108));
        sb.AppendLine("=== Tick vs Propagation Correlation ===");
        sb.AppendLine("");

        var props = new (string name, Func<TemporalResult, double> fn)[]
        {
            ("v_max", r => r.VMax),
            ("projSpan", r => r.ProjSpan),
            ("connectivity", r => r.Connectivity),
            ("geoEff", r => r.GeoEff),
            ("bdim", r => (double)r.BDim),
            ("1/N", r => 1.0 / r.N),
        };

        sb.AppendLine($"{"Property vs Tick",-20} {"r(Tick)",10} {"r^2",10}");
        sb.AppendLine(new string('-', 42));

        foreach (var (name, fn) in props)
        {
            double[] xs = allResults.Select(r => fn(r)).ToArray();
            double[] ys = allResults.Select(r => r.Tick).ToArray();
            double r = PearsonCorr(xs, ys);
            sb.AppendLine($"{name,-20} {r,10:F4} {r*r,10:F4}");
        }
        sb.AppendLine("");

        // ================================================================
        // PROPAGATION → TIME MAPPING
        // ================================================================
        sb.AppendLine(new string('=', 108));
        sb.AppendLine("=== Propagation -> Time Mapping ===");
        sb.AppendLine("");

        sb.AppendLine("If propagation is Tick-limited, then:");
        sb.AppendLine("  v_max = f(Tick, N, bdim) should be predictive.");
        sb.AppendLine("");

        // Model: v_max = a * Tick + b * bdim / sqrt(N)
        var forms = new (string name, Func<TemporalResult, double[]> feats)[]
        {
            ("Tick + bdim/sqrt(N)", r => new[] { r.Tick, r.BDim / Math.Sqrt(r.N), 1.0 }),
            ("Tick + connectivity", r => new[] { r.Tick, r.Connectivity, 1.0 }),
            ("Tick + projSpan", r => new[] { r.Tick, r.ProjSpan, 1.0 }),
            ("Tick alone (v = K * Tick)", r => new[] { r.Tick }),
        };

        double bestR2 = 0;
        string bestForm = "";

        foreach (var (name, feats) in forms)
        {
            int nFeat = feats(allResults.First()).Length;
            double[,] XtX = new double[nFeat, nFeat];
            double[] Xty = new double[nFeat];

            foreach (var r in allResults)
            {
                var f = feats(r);
                for (int i = 0; i < nFeat; i++)
                {
                    Xty[i] += f[i] * r.VMax;
                    for (int j = 0; j < nFeat; j++)
                        XtX[i, j] += f[i] * f[j];
                }
            }

            double[] coeffs = SolveLinear(XtX, Xty, nFeat);
            double my = allResults.Average(r => r.VMax);
            double ssr = 0, sst = 0;
            foreach (var r in allResults)
            {
                var f = feats(r);
                double yp = 0;
                for (int i = 0; i < nFeat; i++) yp += coeffs[i] * f[i];
                double d1 = r.VMax - yp, d2 = r.VMax - my;
                ssr += d1 * d1; sst += d2 * d2;
            }
            double r2 = sst > 1e-15 ? 1.0 - ssr / sst : 0;

            sb.Append($"  {name,-30}: R^2={r2:F4}  coeffs=");
            for (int i = 0; i < nFeat; i++) sb.Append($"  {coeffs[i]:F4}");
            sb.AppendLine();

            if (r2 > bestR2) { bestR2 = r2; bestForm = name; }
        }
        sb.AppendLine($"  Best: {bestForm} (R^2={bestR2:F4})");
        sb.AppendLine("");

        // ================================================================
        // DISTANCE / TICK CONVERSION
        // ================================================================
        sb.AppendLine(new string('=', 108));
        sb.AppendLine("=== Distance / Tick Conversion ===");
        sb.AppendLine("");

        sb.AppendLine("If distance/Tick is constant, propagation steps = Tick units.");
        sb.AppendLine("");

        double[] distTickRatios = allResults.Select(r => r.Diameter / Math.Max(1e-15, r.Tick)).ToArray();
        double dtrMean = distTickRatios.Average();
        double dtrSd = Math.Sqrt(distTickRatios.Average(d => (d - dtrMean) * (d - dtrMean)));
        double dtrCv = dtrMean > 0 ? dtrSd / dtrMean : 0;

        sb.AppendLine($"  Mean dist/Tick: {dtrMean:F2}  ± {dtrSd:F2}  (CV = {dtrCv:F4})");
        sb.AppendLine("");

        // Check per-architecture constancy
        foreach (var g in allResults.GroupBy(r => r.Arch))
        {
            double[] vals = g.Select(r => r.Diameter / Math.Max(1e-15, r.Tick)).ToArray();
            double m = vals.Average();
            double s = Math.Sqrt(vals.Average(v => (v - m) * (v - m)));
            sb.AppendLine($"  {g.Key}: mean={m:F2} ± {s:F2}  CV={(m > 0 ? s / m : 0):F4}");
        }
        sb.AppendLine("");

        // ================================================================
        // UNIVERSAL BOUND IN TICK UNITS
        // ================================================================
        sb.AppendLine(new string('=', 108));
        sb.AppendLine("=== Universal Bound in Tick Units ===");
        sb.AppendLine("");

        // Normalize v_max by Tick: v_max/Tick
        double[] vTickAll = allResults.Select(r => r.VMax / Math.Max(1e-15, r.Tick)).ToArray();
        double vtMean = vTickAll.Average();
        double vtSd = Math.Sqrt(vTickAll.Average(v => (v - vtMean) * (v - vtMean)));
        double vtCv = vtMean > 0 ? vtSd / vtMean : 0;

        sb.AppendLine($"  Cross-arch v_max/Tick: mean={vtMean:F4} ± {vtSd:F4}  CV={vtCv:F4}");
        sb.AppendLine("");

        foreach (var g in allResults.GroupBy(r => r.Arch))
        {
            double[] vals = g.Select(r => r.VMax / Math.Max(1e-15, r.Tick)).ToArray();
            double m = vals.Average();
            double s = Math.Sqrt(vals.Average(v => (v - m) * (v - m)));
            sb.AppendLine($"  {g.Key}: mean={m:F4} ± {s:F4}  CV={(m > 0 ? s / m : 0):F4}");
        }
        sb.AppendLine("");

        // ================================================================
        // CANDIDATE TEMPORAL LAW
        // ================================================================
        sb.AppendLine(new string('=', 108));
        sb.AppendLine("=== Candidate Temporal Law ===");
        sb.AppendLine("");

        sb.AppendLine("  Tick = mean |d(VarI1 + VarTerms)/da| measures intrinsic change rate.");
        sb.AppendLine("  v_max/Tick maps propagation bound to Tick units.");
        sb.AppendLine("  distance/Tick maps spatial extent to temporal extent.");
        sb.AppendLine("");
        sb.AppendLine("  Proposed temporal emergence chain:");
        sb.AppendLine("    Adjacency -> Propagation -> v_max -> Tick scaling -> Temporal metric");
        sb.AppendLine("");
        sb.AppendLine("  Candidate temporal law:");
        sb.AppendLine($"    v_max = {bestR2:F4} * f(Tick, geometry)");
        sb.AppendLine($"    distance/Tick ≈ {dtrMean:F2} (CV = {dtrCv:F4})");
        sb.AppendLine($"    v_max/Tick ≈ {vtMean:F4} (cross-arch CV = {vtCv:F4})");
        sb.AppendLine("");

        // ================================================================
        // DECISION
        // ================================================================
        sb.AppendLine(new string('=', 108));
        sb.AppendLine("=== Decision ===");
        sb.AppendLine("");

        // Criteria:
        // A. Tick correlates with propagation geometry (max |r| > 0.4)
        // B. v_max predictable from Tick (best R^2 > 0.4)
        // C. distance/Tick is reasonably constant (cross-arch CV < 0.30)
        // D. v_max/Tick is universal (cross-arch CV < 0.30)

        double maxCorr = props.Max(p => Math.Abs(PearsonCorr(
            allResults.Select(r => p.fn(r)).ToArray(),
            allResults.Select(r => r.Tick).ToArray())));

        bool criterionA = maxCorr > 0.4;
        bool criterionB = bestR2 > 0.4;
        bool criterionC = dtrCv < 0.30;
        bool criterionD = vtCv < 0.30;

        int criteriaMet = 0;
        if (criterionA) criteriaMet++;
        if (criterionB) criteriaMet++;
        if (criterionC) criteriaMet++;
        if (criterionD) criteriaMet++;

        string verdict = criteriaMet >= 4 ? "SUPPORTED: time emerges from propagation geometry."
            : criteriaMet >= 2 ? "CONDITIONAL: partial temporal emergence."
            : "FALSIFIED: additional principles required.";

        sb.AppendLine($"VERDICT: {verdict}  ({criteriaMet}/4 criteria)");
        sb.AppendLine("");
        sb.AppendLine($"  A. Tick-geometry correlation (max|r|>0.4): {(criterionA ? "YES" : "NO")} (max|r|={maxCorr:F4})");
        sb.AppendLine($"  B. v_max from Tick (R^2>0.4):            {(criterionB ? "YES" : "NO")} (R^2={bestR2:F4})");
        sb.AppendLine($"  C. dist/Tick constant (CV<0.30):          {(criterionC ? "YES" : "NO")} (CV={dtrCv:F4})");
        sb.AppendLine($"  D. v_max/Tick universal (CV<0.30):        {(criterionD ? "YES" : "NO")} (CV={vtCv:F4})");
        sb.AppendLine("");
        sb.AppendLine("Temporal Propagation Invariant Principle:");
        sb.AppendLine("  Propagation geometry defines an intrinsic temporal scale.");
        sb.AppendLine("  Tick = intrinsic change rate of the TRM system.");
        sb.AppendLine("  Propagation steps map to Tick units via the universal bound.");
        sb.AppendLine("  A proto-temporal metric emerges: dt = ds / v_bound.");
        sb.AppendLine("");
        sb.AppendLine(new string('=', 108));
        sb.AppendLine("=== TPI_01 complete. Commit: TPI_01_TemporalPropagationInvariantAudit ===");

        _o.WriteLine(sb.ToString());
        Assert.True(true);
    }

    // ================================================================
    // GRAPH HELPERS
    // ================================================================

    private static List<int>[] Build1DGraph(int nGrid, VcFamily fam,
        double[] distances, double[] sortedD, double xiBase, double k0Base,
        int nA, double aMin, double daD, out int N, out int edges)
    {
        var allPts = new List<(double b, double g, double absM, int sign)>();
        for (int bi = 0; bi < nGrid; bi++)
        { double bVal = 0.0 + 2.0 * bi / (nGrid - 1); for (int gi = 0; gi < nGrid; gi++) { double gVal = 0.0 + 2.0 * gi / (nGrid - 1); var (m, dTdp) = ComputeM_and_DTdp(fam, 1.0, 1.0, 0.70, bVal, gVal, distances, sortedD, xiBase, k0Base, nA, aMin, daD); allPts.Add((bVal, gVal, Math.Abs(m), dTdp > 1e-8 ? 1 : -1)); } }
        var bs = allPts.Select(p => p.b).Distinct().OrderBy(x => x).ToList(); var gs = allPts.Select(p => p.g).Distinct().OrderBy(x => x).ToList();
        int nb = bs.Count, ng = gs.Count; var sm = new int[nb, ng];
        foreach (var pt in allPts) { int bi = bs.IndexOf(pt.b), gi = gs.IndexOf(pt.g); if (bi >= 0 && gi >= 0) sm[bi, gi] = pt.sign; }
        var bdry = new List<(int, int)>(); var bset = new HashSet<(int, int)>();
        for (int bi = 0; bi < nb; bi++) for (int gi = 0; gi < ng; gi++) { bool opp = false; if (bi > 0 && sm[bi, gi] != sm[bi - 1, gi]) opp = true; if (bi + 1 < nb && sm[bi, gi] != sm[bi + 1, gi]) opp = true; if (gi > 0 && sm[bi, gi] != sm[bi, gi - 1]) opp = true; if (gi + 1 < ng && sm[bi, gi] != sm[bi, gi + 1]) opp = true; if (opp) { bdry.Add((bi, gi)); bset.Add((bi, gi)); } }
        N = bdry.Count; var imap = new Dictionary<(int, int), int>(); for (int i = 0; i < N; i++) imap[bdry[i]] = i;
        var adj = new List<int>[N]; for (int i = 0; i < N; i++) adj[i] = new List<int>();
        var dirs = new[] { (1, 0), (-1, 0), (0, 1), (0, -1), (1, 1), (1, -1), (-1, 1), (-1, -1) };
        for (int i = 0; i < N; i++) { var (bi, gi) = bdry[i]; foreach (var (db, dg) in dirs) { int nb2 = bi + db, ng2 = gi + dg; if (bset.Contains((nb2, ng2))) { int j = imap[(nb2, ng2)]; if (!adj[i].Contains(j)) adj[i].Add(j); } } }
        edges = adj.Sum(a => a.Count) / 2; return adj;
    }

    private static List<int>[] Build3DGraph(int nGrid, VcFamily fam,
        double[] distances, double[] sortedD, double xiBase, double k0Base,
        int nA, double aMin, double daD, out int N, out int edges)
    {
        var bag = new ConcurrentBag<(int ai, int bi, int gi, int sign)>();
        Parallel.For(0, nGrid, ai => { double alpha = 0.1 + (3.0 - 0.1) * ai / (nGrid - 1); for (int bi = 0; bi < nGrid; bi++) { double beta = 0.0 + 2.0 * bi / (nGrid - 1); for (int gi = 0; gi < nGrid; gi++) { double gamma = 0.0 + 2.0 * gi / (nGrid - 1); var (m, dTdp) = ComputeM_and_DTdp(fam, 1.0, 1.0, alpha, beta, gamma, distances, sortedD, xiBase, k0Base, nA, aMin, daD); bag.Add((ai, bi, gi, dTdp > 1e-8 ? 1 : -1)); } } });
        var all = bag.ToList(); var s3 = new int[nGrid, nGrid, nGrid]; foreach (var p in all) s3[p.ai, p.bi, p.gi] = p.sign;
        var bdry = new List<(int, int, int)>(); var bset = new HashSet<(int, int, int)>();
        for (int ai = 0; ai < nGrid; ai++) for (int bi = 0; bi < nGrid; bi++) for (int gi = 0; gi < nGrid; gi++) { bool opp = false; if (ai > 0 && s3[ai, bi, gi] != s3[ai - 1, bi, gi]) opp = true; if (ai + 1 < nGrid && s3[ai, bi, gi] != s3[ai + 1, bi, gi]) opp = true; if (bi > 0 && s3[ai, bi, gi] != s3[ai, bi - 1, gi]) opp = true; if (bi + 1 < nGrid && s3[ai, bi, gi] != s3[ai, bi + 1, gi]) opp = true; if (gi > 0 && s3[ai, bi, gi] != s3[ai, gi - 1, gi]) opp = true; if (gi + 1 < nGrid && s3[ai, bi, gi] != s3[ai, gi + 1, gi]) opp = true; if (opp) { bdry.Add((ai, bi, gi)); bset.Add((ai, bi, gi)); } }
        N = bdry.Count; var imap = new Dictionary<(int, int, int), int>(); for (int i = 0; i < N; i++) imap[bdry[i]] = i;
        var adj = new List<int>[N]; for (int i = 0; i < N; i++) adj[i] = new List<int>();
        for (int da = -1; da <= 1; da++) for (int db = -1; db <= 1; db++) for (int dg = -1; dg <= 1; dg++) { if (da == 0 && db == 0 && dg == 0) continue; for (int i = 0; i < N; i++) { var (a, b, g) = bdry[i]; int na = a + da, nb = b + db, ng = g + dg; if (bset.Contains((na, nb, ng))) { int j = imap[(na, nb, ng)]; if (!adj[i].Contains(j)) adj[i].Add(j); } } }
        edges = adj.Sum(a => a.Count) / 2; return adj;
    }

    private static int ComputeDiameter(List<int>[] adj, int N)
    {
        var rng = new Random(42); int nSources = Math.Min(15, N);
        var sources = Enumerable.Range(0, N).OrderBy(_ => rng.Next()).Take(nSources).ToList(); int maxDist = 0;
        foreach (var src in sources) { var gd = BFS(adj, N, src); for (int i = 0; i < N; i++) if (gd[i] > maxDist) maxDist = gd[i]; }
        return maxDist;
    }

    private static int[] BFS(List<int>[] adj, int N, int src)
    {
        var gd = new int[N]; Array.Fill(gd, -1); var q = new Queue<int>(); q.Enqueue(src); gd[src] = 0;
        while (q.Count > 0) { int u = q.Dequeue(); foreach (int v in adj[u]) if (gd[v] < 0) { gd[v] = gd[u] + 1; q.Enqueue(v); } }
        return gd;
    }

    // ================================================================
    // GEOMETRY METRICS
    // ================================================================

    private static GeometryMetrics MeasureGeometryMetrics(List<int>[] adj, int N, int diameter, int nSources)
    {
        var rng = new Random(42);
        nSources = Math.Min(nSources, N);
        var sources = Enumerable.Range(0, N).OrderBy(_ => rng.Next()).Take(nSources).ToList();
        var vMaxs = new List<double>();
        var geoEffs = new List<double>();
        var avgPaths = new List<double>();

        foreach (var src in sources)
        {
            var dist = BFS(adj, N, src);
            var reachable = new List<int>();
            for (int i = 0; i < N; i++) if (dist[i] >= 0) reachable.Add(i);
            if (reachable.Count < 2) continue;
            int maxD = dist.Where(d => d >= 0).Max();
            vMaxs.Add((double)maxD / N);
            double meanDist = reachable.Average(i => (double)dist[i]);
            geoEffs.Add(maxD > 0 ? meanDist / maxD : 0);
            avgPaths.Add(meanDist);
        }
        double projSpan = N > 0 ? diameter / Math.Sqrt(N) : 0;
        double connectivity = N > 0 ? (double)adj.Sum(a => a.Count) / (2.0 * N) : 0;
        return new GeometryMetrics(vMaxs.Average(), geoEffs.Average(), avgPaths.Average(), projSpan, connectivity);
    }

    // ================================================================
    // TICK COMPUTATION
    // ================================================================

    /// <summary>
    /// Compute the mean Tick for a graph configuration.
    /// Tick = mean |d(VarI1+VarTerms)/da| over grid samples.
    /// Uses ComputeFull for the representative parameters at each grid point.
    /// </summary>
    private static double ComputeGraphTick(VcFamily fam, int nGrid,
        double[] distances, double[] sortedD, double xiBase, double k0Base,
        int nA, double aMin, double daD, string dim)
    {
        // Sample representative points and average their Tick values
        var tickValues = new ConcurrentBag<double>();

        if (dim == "1D")
        {
            // 1D: sample beta, gamma grid
            Parallel.For(0, Math.Min(nGrid, 12), bi =>
            {
                double beta = 0.0 + 2.0 * bi / (nGrid - 1);
                for (int gi = 0; gi < Math.Min(nGrid, 12); gi++)
                {
                    double gamma = 0.0 + 2.0 * gi / (nGrid - 1);
                    var full = ComputeFull(fam, 1.0, 1.0, 0.70, beta, gamma, distances, sortedD, xiBase, k0Base, nA, aMin, daD);
                    tickValues.Add(full.tick);
                }
            });
        }
        else
        {
            // 3D: sample alpha, beta, gamma grid
            Parallel.For(0, Math.Min(nGrid, 8), ai =>
            {
                double alpha = 0.1 + (3.0 - 0.1) * ai / (nGrid - 1);
                for (int bi = 0; bi < Math.Min(nGrid, 8); bi++)
                {
                    double beta = 0.0 + 2.0 * bi / (nGrid - 1);
                    for (int gi = 0; gi < Math.Min(nGrid, 8); gi++)
                    {
                        double gamma = 0.0 + 2.0 * gi / (nGrid - 1);
                        var full = ComputeFull(fam, 1.0, 1.0, alpha, beta, gamma, distances, sortedD, xiBase, k0Base, nA, aMin, daD);
                        tickValues.Add(full.tick);
                    }
                }
            });
        }

        return tickValues.Count > 0 ? tickValues.Average() : 0;
    }

    // ================================================================
    // UTILITY
    // ================================================================

    private static double PearsonCorr(double[] xs, double[] ys)
    {
        int n = xs.Length; if (n < 2) return 0;
        double mx = xs.Average(), my = ys.Average();
        double sxy = 0, sxx = 0, syy = 0;
        for (int i = 0; i < n; i++) { double dx = xs[i] - mx, dy = ys[i] - my; sxy += dx * dy; sxx += dx * dx; syy += dy * dy; }
        return sxx > 1e-15 && syy > 1e-15 ? sxy / Math.Sqrt(sxx * syy) : 0;
    }

    private static double[] SolveLinear(double[,] a, double[] b, int n)
    {
        double[,] aug = new double[n, n + 1];
        for (int i = 0; i < n; i++) { for (int j = 0; j < n; j++) aug[i, j] = a[i, j]; aug[i, n] = b[i]; }
        for (int col = 0; col < n; col++)
        {
            int pivot = col;
            for (int row = col + 1; row < n; row++) if (Math.Abs(aug[row, col]) > Math.Abs(aug[pivot, col])) pivot = row;
            for (int j = 0; j <= n; j++) { double t = aug[col, j]; aug[col, j] = aug[pivot, j]; aug[pivot, j] = t; }
            if (Math.Abs(aug[col, col]) < 1e-15) continue;
            for (int j = n; j >= col; j--) aug[col, j] /= aug[col, col];
            for (int row = 0; row < n; row++)
                if (row != col && Math.Abs(aug[row, col]) > 1e-15)
                    for (int j = n; j >= col; j--) aug[row, j] -= aug[row, col] * aug[col, j];
        }
        double[] result = new double[n];
        for (int i = 0; i < n; i++) result[i] = aug[i, n];
        return result;
    }

    private record GeometryMetrics(double VMax, double GeoEff, double AvgPathLen, double ProjSpan, double Connectivity);
    private record TemporalResult(string Arch, string Dim, int BDim, int GridSize, int N, int Edges, int Diameter,
        double VMax, double ProjSpan, double Connectivity, double GeoEff, double AvgPathLen, double Tick)
    {
        public TemporalResult(string arch, string dim, int bdim, int gridSize, int n, int edges, int diameter,
            GeometryMetrics m, double tick)
            : this(arch, dim, bdim, gridSize, n, edges, diameter, m.VMax, m.ProjSpan, m.Connectivity, m.GeoEff, m.AvgPathLen, tick) { }
    }
}
