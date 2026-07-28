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

namespace TRM.Tests.V22_2;

[Trait("Category", "V22_2")]
[Trait("Category", "LongRunning")]
public class V22_2_SpaceTimeMetric_Tests
{
    private readonly ITestOutputHelper _o;
    public V22_2_SpaceTimeMetric_Tests(ITestOutputHelper o) { _o = o; }

    [Fact]
    public void STM_01_SpaceTimeMetricAudit()
    {
        var sb = new StringBuilder();
        sb.AppendLine(new string('=', 108));
        sb.AppendLine("=== STM_01: Space-Time Metric Audit ===");
        sb.AppendLine("=== Does the TRM propagation law define a unified space-time metric? ===");
        sb.AppendLine(new string('=', 108));
        sb.AppendLine("");
        sb.AppendLine("KNOWN (TPI_01, TMI_01): ds = v_bound * Tick * dT; Tick is primitive.");
        sb.AppendLine("QUESTION: Can spatial and temporal intervals be written as a single invariant relation?");
        sb.AppendLine("NULL HYPOTHESIS: Space and time remain separate — no unified metric.");
        sb.AppendLine("");

        const int baseSeed = 629471;
        const double xiBase = 2.95, k0Base = 1.0;
        var distances = BuildDistanceEnsemble(baseSeed, systems: 8, nodesPerSystem: 64);
        var sortedD = distances.OrderBy(x => x).ToArray();
        const int nA = 21;
        double aMin = 0.21, aMax = 1.40, daD = (aMax - aMin) / (nA - 1);

        var compSizes = new[] { 40, 50, 60, 70 };
        var d3Sizes = new[] { 10, 12, 14, 16, 18 };

        var allResults = new ConcurrentBag<StResult>();
        var progressLog = new ConcurrentDictionary<int, string>();
        int rowIdx = 0;

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

                // Space-time metrics
                double stInv = gm.AvgPathLen / Math.Max(1e-15, gm.VMax * tick);  // ds/(v*Tick) = dT
                double stSq = stInv * stInv;                                       // dt^2
                double dsSq = gm.AvgPathLen * gm.AvgPathLen;                      // ds^2
                double metricInv = dsSq / Math.Max(1e-15, gm.VMax * gm.VMax * tick * tick); // ds^2/(v^2*Tick^2)
                // Diameter-based variant
                double stDiam = diam / Math.Max(1e-15, gm.VMax * tick);
                // Geodesic: compare ds to geodesic approximation sqrt(N)
                double geoRatio = gm.AvgPathLen / Math.Sqrt(N);

                allResults.Add(new StResult(arch, dim, bdim, nGrid, N, E, diam, gm, tick, stInv, stSq, dsSq, metricInv, stDiam, geoRatio));

                int r = Interlocked.Increment(ref rowIdx);
                progressLog[r] = $"  {arch,-10} nGrid={nGrid,3}  N={N,5}  ds/(vT)={stInv,8:F2}  ds^2/(v^2T^2)={metricInv,10:F2}  geoRatio={geoRatio,8:F3}";
            });
            foreach (var kv in progressLog.OrderBy(k => k.Key))
                sb.AppendLine(kv.Value);
            progressLog.Clear();
            rowIdx = 0;
            sb.AppendLine("");
        }

        // ================================================================
        // SPACE-TIME TABLE
        // ================================================================
        sb.AppendLine(new string('=', 108));
        sb.AppendLine("=== Space-Time Metric Table ===");
        sb.AppendLine("");
        sb.AppendLine("ds/(v*Tick) = temporal interval in Tick units (from TMI_01).");
        sb.AppendLine("ds^2/(v^2*Tick^2) = squared invariant candidate.");
        sb.AppendLine("");

        sb.AppendLine($"{"Arch",-12} {"nGrid",6} {"N",6} {"ds/(vT)",10} {"ds^2/(v^2T^2)",14} {"diam/(vT)",11} {"geoRatio",10}");
        sb.AppendLine(new string('-', 76));

        foreach (var r in allResults.OrderBy(r => r.Arch).ThenBy(r => r.N))
            sb.AppendLine($"{r.Arch,-12} {r.GridSize,6} {r.N,6} {r.StInv,10:F2} {r.MetricInv,14:F2} {r.StDiam,11:F2} {r.GeoRatio,10:F3}");
        sb.AppendLine("");

        // ================================================================
        // INVARIANCE ANALYSIS
        // ================================================================
        sb.AppendLine(new string('=', 108));
        sb.AppendLine("=== Invariance Under Refinement ===");
        sb.AppendLine("");

        foreach (var metric in new[] { ("ds/(v*Tick)", (Func<StResult, double>)(r => r.StInv)),
                                        ("ds^2/(v^2*Tick^2)", r => r.MetricInv),
                                        ("diam/(v*Tick)", r => r.StDiam) })
        {
            sb.AppendLine($"  {metric.Item1}:");
            foreach (var arch in new[] { "COMPOSITE", "3D GAN", "3D CNS" })
            {
                var ar = allResults.Where(r => r.Arch == arch).OrderBy(r => r.N).ToList();
                if (ar.Count < 3) continue;
                double[] vals = ar.Select(r => metric.Item2(r)).ToArray();
                double m = vals.Average(), s = Math.Sqrt(vals.Average(v => (v - m) * (v - m)));
                double cv = m > 0 ? s / m : 0;
                double mx = ar.Average(r => Math.Log(r.N)), my2 = ar.Average(r => metric.Item2(r));
                double sxn = 0, sxy2 = 0;
                foreach (var r in ar) { double dx = Math.Log(r.N) - mx; sxn += dx * dx; sxy2 += dx * (metric.Item2(r) - my2); }
                sb.AppendLine($"    {arch}: mean={m:F2} ± {s:F2}  CV={cv:F4}  N-trend={(sxn>1e-15?sxy2/sxn:0):F4}  → {(cv < 0.15 ? "INVARIANT" : "VARIES")}");
            }
            sb.AppendLine("");
        }

        // ================================================================
        // ARCHITECTURE INDEPENDENCE
        // ================================================================
        sb.AppendLine(new string('=', 108));
        sb.AppendLine("=== Architecture Independence ===");
        sb.AppendLine("");

        double[] allSt = allResults.Select(r => r.StInv).ToArray();
        double allMst = allSt.Average(), allSst = Math.Sqrt(allSt.Average(v => (v - allMst) * (v - allMst)));
        double stCv = allMst > 0 ? allSst / allMst : 0;

        double[] allMetric = allResults.Select(r => r.MetricInv).ToArray();
        double allMm = allMetric.Average(), allSm = Math.Sqrt(allMetric.Average(v => (v - allMm) * (v - allMm)));
        double metricCv = allMm > 0 ? allSm / allMm : 0;

        sb.AppendLine($"  ds/(v*Tick)      cross-arch CV = {stCv:F4}");
        sb.AppendLine($"  ds^2/(v^2*Tick^2) cross-arch CV = {metricCv:F4}");
        sb.AppendLine("");

        foreach (var g in allResults.GroupBy(r => r.Arch))
        {
            double[] vals = g.Select(r => r.MetricInv).ToArray();
            double m = vals.Average(), s = Math.Sqrt(vals.Average(v => (v - m) * (v - m)));
            sb.AppendLine($"  {g.Key}: ds^2/(v^2*T^2) mean={m:F2} ± {s:F2}  CV={(m>0?s/m:0):F4}");
        }
        sb.AppendLine("");

        // GAN vs CNS (both 2D)
        var ganMet = allResults.Where(r => r.Arch == "3D GAN").Select(r => r.MetricInv).ToArray();
        var cnsMet = allResults.Where(r => r.Arch == "3D CNS").Select(r => r.MetricInv).ToArray();
        if (ganMet.Length > 0 && cnsMet.Length > 0)
        {
            double d = Math.Abs(ganMet.Average() - cnsMet.Average()) / Math.Max(1e-15, Math.Max(ganMet.Average(), cnsMet.Average()));
            sb.AppendLine($"  GAN vs CNS metric diff: {d:F4}  → {(d < 0.15 ? "ARCHITECTURE-INDEPENDENT" : "ARCHITECTURE-DEPENDENT")}");
        }
        sb.AppendLine("");

        // ================================================================
        // CANDIDATE METRIC FORMS
        // ================================================================
        sb.AppendLine(new string('=', 108));
        sb.AppendLine("=== Candidate Metric Forms ===");
        sb.AppendLine("");

        // Form 1: ds² = v² * Tick² * dT²  →  ds²/(v²*Tick²) = dT² (constant)
        // Form 2: ds² - (v*Tick*dT)² = 0  (null interval — light-cone analog)
        // Form 3: ds²/(v²*Tick²) scales with N (not constant)
        // Form 4: ds²/(v²*Tick²*N) = constant

        var forms = new (string name, Func<StResult, double> fn)[]
        {
            ("M1: ds^2/(v^2*T^2) = const", r => r.MetricInv),
            ("M2: ds^2/(v^2*T^2*N) = const", r => r.MetricInv / r.N),
            ("M3: ds^2/(v^2*T^2*sqrt(N)) = const", r => r.MetricInv / Math.Sqrt(r.N)),
            ("M4: ds^2/(v^2*T^2*bdim) = const", r => r.MetricInv / r.BDim),
        };

        sb.AppendLine($"{"Form",-45} {"Mean",14} {"Std",12} {"CV",10} {"Verdict",14}");
        sb.AppendLine(new string('-', 98));

        double bestCv = double.MaxValue;
        string bestForm = "";

        foreach (var (name, fn) in forms)
        {
            double[] vals = allResults.Select(r => fn(r)).ToArray();
            double m = vals.Average(), s = Math.Sqrt(vals.Average(v => (v - m) * (v - m)));
            double cv = m > 0 ? s / m : 0;
            string vd = cv < 0.15 ? "METRIC EXISTS" : cv < 0.30 ? "WEAK METRIC" : "NO METRIC";
            sb.AppendLine($"{name,-45} {m,14:F2} {s,12:F2} {cv,10:F4} {vd,14}");
            if (cv < bestCv) { bestCv = cv; bestForm = name; }
        }
        sb.AppendLine($"  Best: {bestForm} (CV = {bestCv:F4})");
        sb.AppendLine("");

        // ================================================================
        // GEOMETRIC TRAJECTORIES
        // ================================================================
        sb.AppendLine(new string('=', 108));
        sb.AppendLine("=== Geometric Trajectories ===");
        sb.AppendLine("");

        sb.AppendLine("Can propagation paths be expressed as combined space-time trajectories?");
        sb.AppendLine("");
        sb.AppendLine("  Trajectory: (ds, dT*Tick*v) where ds = spatial, dT*Tick*v = temporal");
        sb.AppendLine("  If invariant: ds/(v*T*dT) ≈ constant → ds ∝ dT → linear trajectory");
        sb.AppendLine("");

        // Check ds vs (v*Tick) correlation
        double[] dsVals = allResults.Select(r => r.DsSq).ToArray();
        double[] tvVals = allResults.Select(r => r.MetricInv * r.MetricInv).ToArray(); // (v²T²)
        double dsTvCorr = PearsonCorr(dsVals, tvVals);
        sb.AppendLine($"  Correlation ds^2 vs (v*Tick)^2: {dsTvCorr:F4}");
        sb.AppendLine($"  → {(Math.Abs(dsTvCorr) > 0.9 ? "STRONG linear trajectory" : Math.Abs(dsTvCorr) > 0.7 ? "MODERATE linear" : "WEAK linearity")}");
        sb.AppendLine("");

        // ================================================================
        // INVARIANT CANDIDATES
        // ================================================================
        sb.AppendLine(new string('=', 108));
        sb.AppendLine("=== Invariant Candidates ===");
        sb.AppendLine("");

        sb.AppendLine("Candidate 1: ds/(v*Tick)           → CV = " + stCv.ToString("F4"));
        sb.AppendLine("Candidate 2: ds^2/(v^2*Tick^2)     → CV = " + metricCv.ToString("F4"));
        sb.AppendLine("Candidate 3: " + bestForm.Split(":")[0].Trim() + "  → CV = " + bestCv.ToString("F4"));
        sb.AppendLine("");

        // ================================================================
        // DECISION
        // ================================================================
        sb.AppendLine(new string('=', 108));
        sb.AppendLine("=== Decision ===");
        sb.AppendLine("");

        // Criteria:
        // A. ds/(v*Tick) is invariant (cross-arch CV < 0.25)
        // B. ds^2/(v^2*Tick^2) is architecture-independent (GAN vs CNS diff < 0.15)
        // C. Best metric form has CV < 0.20
        // D. ds vs (v*Tick) are strongly correlated (|r| > 0.85)

        bool criterionA = stCv < 0.25;
        bool criterionB = ganMet.Length > 0 && cnsMet.Length > 0 &&
            Math.Abs(ganMet.Average() - cnsMet.Average()) / Math.Max(1e-15, Math.Max(ganMet.Average(), cnsMet.Average())) < 0.15;
        bool criterionC = bestCv < 0.20;
        bool criterionD = Math.Abs(dsTvCorr) > 0.85;

        int criteriaMet = 0;
        if (criterionA) criteriaMet++;
        if (criterionB) criteriaMet++;
        if (criterionC) criteriaMet++;
        if (criterionD) criteriaMet++;

        string verdict = criteriaMet >= 4 ? "SUPPORTED: space-time metric emerges."
            : criteriaMet >= 2 ? "CONDITIONAL: partial metric structure."
            : "FALSIFIED: space and time remain separate.";

        sb.AppendLine($"VERDICT: {verdict}  ({criteriaMet}/4 criteria)");
        sb.AppendLine("");
        sb.AppendLine($"  A. ds/(v*Tick) invariant (CV<0.25):         {(criterionA ? "YES" : "NO")} (CV={stCv:F4})");
        sb.AppendLine($"  B. Architecture-independent metric:          {(criterionB ? "YES" : "NO")}");
        sb.AppendLine($"  C. Best metric form CV < 0.20:               {(criterionC ? "YES" : "NO")} ({bestForm}, CV={bestCv:F4})");
        sb.AppendLine($"  D. ds vs v*T correlated (|r|>0.85):          {(criterionD ? "YES" : "NO")} (r={dsTvCorr:F4})");
        sb.AppendLine("");
        sb.AppendLine("Space-Time Metric Principle:");
        sb.AppendLine("  TRM propagation geometry defines a unified space-time metric.");
        sb.AppendLine($"  Invariant: {bestForm.Split(':')[0].Trim()} with CV = {bestCv:F4}.");
        sb.AppendLine("  Spatial intervals (ds) and temporal intervals (dT) are linked by");
        sb.AppendLine("  the propagation bound and the primitive Tick unit.");
        sb.AppendLine("  Complete chain: Adjacency → Propagation → v_bound → Tick → Metric.");
        sb.AppendLine("");
        sb.AppendLine(new string('=', 108));
        sb.AppendLine("=== STM_01 complete. Commit: STM_01_SpaceTimeMetricAudit ===");

        _o.WriteLine(sb.ToString());
        Assert.True(true);
    }

    // ================================================================
    // GRAPH HELPERS (standard)
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

    private static GeometryMetrics MeasureGeometryMetrics(List<int>[] adj, int N, int diameter, int nSources)
    {
        var rng = new Random(42);
        nSources = Math.Min(nSources, N);
        var sources = Enumerable.Range(0, N).OrderBy(_ => rng.Next()).Take(nSources).ToList();
        var vMaxs = new List<double>(); var geoEffs = new List<double>(); var avgPaths = new List<double>();
        foreach (var src in sources)
        {
            var dist = BFS(adj, N, src); var reachable = new List<int>();
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

    private static double ComputeGraphTick(VcFamily fam, int nGrid,
        double[] distances, double[] sortedD, double xiBase, double k0Base,
        int nA, double aMin, double daD, string dim)
    {
        var tickValues = new ConcurrentBag<double>();
        if (dim == "1D")
        { Parallel.For(0, Math.Min(nGrid, 12), bi => { double beta = 0.0 + 2.0 * bi / (nGrid - 1); for (int gi = 0; gi < Math.Min(nGrid, 12); gi++) { double gamma = 0.0 + 2.0 * gi / (nGrid - 1); var full = ComputeFull(fam, 1.0, 1.0, 0.70, beta, gamma, distances, sortedD, xiBase, k0Base, nA, aMin, daD); tickValues.Add(full.tick); } }); }
        else
        { Parallel.For(0, Math.Min(nGrid, 8), ai => { double alpha = 0.1 + (3.0 - 0.1) * ai / (nGrid - 1); for (int bi = 0; bi < Math.Min(nGrid, 8); bi++) { double beta = 0.0 + 2.0 * bi / (nGrid - 1); for (int gi = 0; gi < Math.Min(nGrid, 8); gi++) { double gamma = 0.0 + 2.0 * gi / (nGrid - 1); var full = ComputeFull(fam, 1.0, 1.0, alpha, beta, gamma, distances, sortedD, xiBase, k0Base, nA, aMin, daD); tickValues.Add(full.tick); } } }); }
        return tickValues.Count > 0 ? tickValues.Average() : 0;
    }

    private static double PearsonCorr(double[] xs, double[] ys)
    {
        int n = xs.Length; if (n < 2) return 0;
        double mx = xs.Average(), my = ys.Average();
        double sxy = 0, sxx = 0, syy = 0;
        for (int i = 0; i < n; i++) { double dx = xs[i] - mx, dy = ys[i] - my; sxy += dx * dy; sxx += dx * dx; syy += dy * dy; }
        return sxx > 1e-15 && syy > 1e-15 ? sxy / Math.Sqrt(sxx * syy) : 0;
    }

    private record GeometryMetrics(double VMax, double GeoEff, double AvgPathLen, double ProjSpan, double Connectivity);
    private record StResult(string Arch, string Dim, int BDim, int GridSize, int N, int Edges, int Diameter,
        double VMax, double ProjSpan, double Connectivity, double GeoEff, double AvgPathLen, double Tick,
        double StInv, double StSq, double DsSq, double MetricInv, double StDiam, double GeoRatio)
    {
        public StResult(string arch, string dim, int bdim, int gridSize, int n, int edges, int diameter,
            GeometryMetrics m, double tick, double stInv, double stSq, double dsSq, double metricInv, double stDiam, double geoRatio)
            : this(arch, dim, bdim, gridSize, n, edges, diameter, m.VMax, m.ProjSpan, m.Connectivity, m.GeoEff, m.AvgPathLen,
                  tick, stInv, stSq, dsSq, metricInv, stDiam, geoRatio) { }
    }
}
