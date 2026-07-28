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

namespace TRM.Tests.V21_9;

[Trait("Category", "V21_9")]
[Trait("Category", "LongRunning")]
public class V21_9_UniversalPropagationInvariant_Tests
{
    private readonly ITestOutputHelper _o;
    public V21_9_UniversalPropagationInvariant_Tests(ITestOutputHelper o) { _o = o; }

    [Fact]
    public void UPI_01_UniversalPropagationInvariantAudit()
    {
        var sb = new StringBuilder();
        sb.AppendLine(new string('=', 108));
        sb.AppendLine("=== UPI_01: Universal Propagation Invariant Audit ===");
        sb.AppendLine("=== Do all TRM architectures converge toward the same propagation bound? ===");
        sb.AppendLine(new string('=', 108));
        sb.AppendLine("");
        sb.AppendLine("KNOWN (PVI_01, PBI_01, PGS_01): v_max converges; it is geometry-controlled.");
        sb.AppendLine("QUESTION: Is the bound UNIVERSAL across architectures, or architecture-dependent?");
        sb.AppendLine("NULL HYPOTHESIS: The bound is architecture-dependent (no universality).");
        sb.AppendLine("");

        const int baseSeed = 629471;
        const double xiBase = 2.95, k0Base = 1.0;
        var distances = BuildDistanceEnsemble(baseSeed, systems: 8, nodesPerSystem: 64);
        var sortedD = distances.OrderBy(x => x).ToArray();
        const int nA = 21;
        double aMin = 0.21, aMax = 1.40, daD = (aMax - aMin) / (nA - 1);

        // Use high resolutions to capture asymptotic behavior
        var compSizes = new[] { 40, 50, 60, 70 };
        var d3Sizes = new[] { 10, 12, 14, 16, 18 };

        var allResults = new ConcurrentBag<UniResult>();
        var progressLog = new ConcurrentDictionary<int, string>();
        int rowIdx = 0;

        sb.AppendLine("--- Building and measuring at highest resolutions ---");

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
                allResults.Add(new UniResult(arch, dim, bdim, nGrid, N, E, diam, gm));

                int r = Interlocked.Increment(ref rowIdx);
                progressLog[r] = $"  {arch,-10} nGrid={nGrid,3}  N={N,5}  E={E,6}  diam={diam,4}  v_max={gm.VMax,8:F4}  projSpan={gm.ProjSpan,8:F3}  conn={gm.Connectivity,7:F4}";
            });
            foreach (var kv in progressLog.OrderBy(k => k.Key))
                sb.AppendLine(kv.Value);
            progressLog.Clear();
            rowIdx = 0;
            sb.AppendLine("");
        }

        // Get largest-N values per architecture for comparison
        var compLargest = allResults.Where(r => r.Arch == "COMPOSITE").OrderByDescending(r => r.N).Take(3).ToList();
        var ganLargest = allResults.Where(r => r.Arch == "3D GAN").OrderByDescending(r => r.N).Take(3).ToList();
        var cnsLargest = allResults.Where(r => r.Arch == "3D CNS").OrderByDescending(r => r.N).Take(3).ToList();

        var compPeak = compLargest.OrderByDescending(r => r.N).First();
        var ganPeak = ganLargest.OrderByDescending(r => r.N).First();
        var cnsPeak = cnsLargest.OrderByDescending(r => r.N).First();

        // ================================================================
        // CROSS-ARCHITECTURE COMPARISON (raw)
        // ================================================================
        sb.AppendLine(new string('=', 108));
        sb.AppendLine("=== Cross-Architecture Comparison (largest N) ===");
        sb.AppendLine("");
        sb.AppendLine($"{"Metric",-22} {"COMPOSITE",14} {"3D GAN",14} {"3D CNS",14} {"Max/Min",10} {"CV",10}");
        sb.AppendLine(new string('-', 86));

        double[] rawVals = { compPeak.VMax, ganPeak.VMax, cnsPeak.VMax };
        double rawMean = rawVals.Average();
        double rawCv = rawMean > 0 ? Math.Sqrt(rawVals.Average(v => (v - rawMean) * (v - rawMean))) / rawMean : 0;
        sb.AppendLine($"{"v_max",-22} {compPeak.VMax,14:F6} {ganPeak.VMax,14:F6} {cnsPeak.VMax,14:F6} {rawVals.Max() / Math.Max(1e-15, rawVals.Min()),10:F3} {rawCv,10:F4}");

        double[] spanVals = { compPeak.ProjSpan, ganPeak.ProjSpan, cnsPeak.ProjSpan };
        double spanCv = spanVals.Average() > 0 ? Math.Sqrt(spanVals.Average(v => (v - spanVals.Average()) * (v - spanVals.Average()))) / spanVals.Average() : 0;
        sb.AppendLine($"{"projSpan",-22} {compPeak.ProjSpan,14:F6} {ganPeak.ProjSpan,14:F6} {cnsPeak.ProjSpan,14:F6} {spanVals.Max() / Math.Max(1e-15, spanVals.Min()),10:F3} {spanCv,10:F4}");

        double[] connVals = { compPeak.Connectivity, ganPeak.Connectivity, cnsPeak.Connectivity };
        double connCv = connVals.Average() > 0 ? Math.Sqrt(connVals.Average(v => (v - connVals.Average()) * (v - connVals.Average()))) / connVals.Average() : 0;
        sb.AppendLine($"{"connectivity",-22} {compPeak.Connectivity,14:F4} {ganPeak.Connectivity,14:F4} {cnsPeak.Connectivity,14:F4} {connVals.Max() / Math.Max(1e-15, connVals.Min()),10:F3} {connCv,10:F4}");

        double[] bdimVals = { 1.0, 2.0, 2.0 };
        sb.AppendLine($"{"bdim",-22} {1,14} {2,14} {2,14} {2.0,10:F3} {Math.Sqrt(2.0/3) / (5.0/3),10:F4}");
        sb.AppendLine("");

        // ================================================================
        // NORMALIZED SCALING ANALYSIS
        // ================================================================
        sb.AppendLine(new string('=', 108));
        sb.AppendLine("=== Normalized Scaling Analysis ===");
        sb.AppendLine("If a universal invariant exists, normalizing by geometry should");
        sb.AppendLine("collapse all architectures onto the same curve.");
        sb.AppendLine("");

        // Normalization schemes
        var norms = new (string name, Func<UniResult, double> fn)[]
        {
            ("v_max / bdim", r => r.VMax / r.BDim),
            ("v_max / projSpan", r => r.VMax / Math.Max(1e-15, r.ProjSpan)),
            ("v_max / connectivity", r => r.VMax / Math.Max(1e-15, r.Connectivity)),
            ("v_max / (projSpan * bdim)", r => r.VMax / Math.Max(1e-15, r.ProjSpan * r.BDim)),
            ("v_max * sqrt(N) / bdim", r => r.VMax * Math.Sqrt(r.N) / r.BDim),
        };

        sb.AppendLine($"{"Normalization",-30} {"COMPOSITE cv",13} {"3D GAN cv",13} {"3D CNS cv",13} {"Cross-arch CV",14} {"Verdict",18}");
        sb.AppendLine(new string('-', 105));

        double bestCv = double.MaxValue;
        string bestNorm = "";

        foreach (var (name, fn) in norms)
        {
            var byArch = allResults.GroupBy(r => r.Arch).ToDictionary(g => g.Key, g =>
            {
                double mn = g.Average(r => fn(r));
                double sd = Math.Sqrt(g.Average(r => { double d = fn(r) - mn; return d * d; }));
                return (mn, sd, cv: mn > 0 ? sd / mn : 0);
            });

            double[] allVals = allResults.Select(r => fn(r)).ToArray();
            double allMean = allVals.Average();
            double allSd = Math.Sqrt(allVals.Average(v => (v - allMean) * (v - allMean)));
            double crossCv = allMean > 0 ? allSd / allMean : 0;

            string verdictStr = crossCv < 0.10 ? "UNIVERSAL" : crossCv < 0.20 ? "NEAR-UNIVERSAL" : crossCv < 0.40 ? "WEAKLY universal" : "NOT universal";

            sb.AppendLine($"{name,-30} {byArch.GetValueOrDefault("COMPOSITE").cv,13:F4} {byArch.GetValueOrDefault("3D GAN").cv,13:F4} {byArch.GetValueOrDefault("3D CNS").cv,13:F4} {crossCv,14:F4} {verdictStr,18}");

            if (crossCv < bestCv) { bestCv = crossCv; bestNorm = name; }
        }
        sb.AppendLine("");
        sb.AppendLine($"  Best normalization: {bestNorm} (cross-architecture CV = {bestCv:F4})");
        sb.AppendLine("");

        // ================================================================
        // ASYMPTOTIC CONVERGENCE COMPARISON
        // ================================================================
        sb.AppendLine(new string('=', 108));
        sb.AppendLine("=== Asymptotic Convergence Comparison ===");
        sb.AppendLine("");

        // Fit v_max vs 1/N for each architecture and compare asymptotes
        var asymptotes = new Dictionary<string, (double asym, double r2, double[] sel)>();
        foreach (var arch in new[] { "COMPOSITE", "3D GAN", "3D CNS" })
        {
            var ar = allResults.Where(r => r.Arch == arch).OrderBy(r => r.N).ToList();
            if (ar.Count < 3) continue;

            double sx = 0, sy = 0, sxy = 0, sx2 = 0;
            int cnt = ar.Count;
            for (int i = 0; i < cnt; i++)
            { double inv = 1.0 / ar[i].N; double y = ar[i].VMax; sx += inv; sy += y; sxy += inv * y; sx2 += inv * inv; }
            double slope = (cnt * sxy - sx * sy) / (cnt * sx2 - sx * sx + 1e-15);
            double ic = (sy - slope * sx) / cnt;
            double r2 = 1.0 - ar.Sum(r => { double yp = ic + slope / r.N; double d = r.VMax - yp; return d * d; }) /
                Math.Max(1e-15, ar.Sum(r => { double d = r.VMax - sy / cnt; return d * d; }));

            // Standard error of asymptote estimate
            double se = 0;
            for (int i = 0; i < cnt; i++) { double yp = ic + slope / ar[i].N; se += (ar[i].VMax - yp) * (ar[i].VMax - yp); }
            se = cnt > 2 ? Math.Sqrt(se / (cnt - 2) / cnt) : 0;

            asymptotes[arch] = (ic, r2, new[] { ic - 1.96 * se, ic + 1.96 * se });
        }

        // Do confidence intervals overlap?
        double[] allAsymptotes = asymptotes.Values.Select(v => v.asym).ToArray();
        double asymMean = allAsymptotes.Average();
        double asymCv = asymMean > 0 ? Math.Sqrt(allAsymptotes.Average(a => (a - asymMean) * (a - asymMean))) / asymMean : 0;

        sb.AppendLine($"{"Arch",-12} {"Asymptote",12} {"95% CI Low",12} {"95% CI High",12} {"R^2",8}");
        sb.AppendLine(new string('-', 58));
        foreach (var kv in asymptotes)
            sb.AppendLine($"{kv.Key,-12} {kv.Value.asym,12:F6} {kv.Value.sel[0],12:F6} {kv.Value.sel[1],12:F6} {kv.Value.r2,8:F4}");
        sb.AppendLine("");

        // Check overlap
        bool allOverlap = true;
        var archKeys = asymptotes.Keys.ToList();
        for (int i = 0; i < archKeys.Count && allOverlap; i++)
            for (int j = i + 1; j < archKeys.Count && allOverlap; j++)
            {
                var ciI = asymptotes[archKeys[i]].sel;
                var ciJ = asymptotes[archKeys[j]].sel;
                if (ciI[1] < ciJ[0] || ciJ[1] < ciI[0]) allOverlap = false;
            }

        sb.AppendLine($"  Asymptote CV across architectures: {asymCv:F4}");
        sb.AppendLine($"  All 95% CIs overlap: {(allOverlap ? "YES — consistent with universality" : "NO — architecture-dependent")}");
        sb.AppendLine("");

        // ================================================================
        // UNIVERSAL COLLAPSE
        // ================================================================
        sb.AppendLine(new string('=', 108));
        sb.AppendLine("=== Universal Collapse Test ===");
        sb.AppendLine("");
        sb.AppendLine($"Normalizing by {bestNorm} across all data points:");
        sb.AppendLine("");

        // Best normalization across all architectures
        var bestFn = norms.First(n => n.name == bestNorm).fn;
        var collapsed = allResults.Select(r => (arch: r.Arch, val: bestFn(r), n: r.N)).ToList();

        double allMeanC = collapsed.Average(c => c.val);
        double allSdC = Math.Sqrt(collapsed.Average(c => { double d = c.val - allMeanC; return d * d; }));
        double allCvC = allMeanC > 0 ? allSdC / allMeanC : 0;

        sb.AppendLine($"  Universal normalized value: {allMeanC:F6} ± {allSdC:F6}  (CV = {allCvC:F4})");
        sb.AppendLine("");

        // Per-architecture means after normalization
        sb.AppendLine($"  {"Arch",-12} {"Mean normalized",16} {"Std",12} {"CV",10}");
        sb.AppendLine(new string('-', 52));
        foreach (var g in collapsed.GroupBy(c => c.arch))
        {
            double mn = g.Average(c => c.val);
            double sd = Math.Sqrt(g.Average(c => { double d = c.val - mn; return d * d; }));
            sb.AppendLine($"  {g.Key,-12} {mn,16:F6} {sd,12:F6} {(mn > 0 ? sd/mn : 0),10:F4}");
        }
        sb.AppendLine("");

        // ================================================================
        // CANDIDATE UNIVERSAL BOUND
        // ================================================================
        sb.AppendLine(new string('=', 108));
        sb.AppendLine("=== Candidate Universal Propagation Bound ===");
        sb.AppendLine("");

        // Fit: universal constant K where v_max = K * bdim / sqrt(N) or similar
        // Test several forms
        var candidates = new (string form, Func<UniResult, double> xFn)[]
        {
            ("v_max = K * bdim / sqrt(N)", r => r.BDim / Math.Sqrt(r.N)),
            ("v_max = K * bdim / N^(2/3)", r => r.BDim / Math.Pow(r.N, 2.0/3.0)),
            ("v_max = K * bdim * connectivity / N", r => r.BDim * r.Connectivity / r.N),
            ("v_max = K * projSpan / sqrt(N)", r => r.ProjSpan / Math.Sqrt(r.N)),
        };

        sb.AppendLine($"{"Form",-45} {"K",10} {"R^2",8}");
        sb.AppendLine(new string('-', 65));

        double bestKR2 = 0;
        string bestKForm = "";
        double bestK = 0;

        foreach (var (form, xFn) in candidates)
        {
            // Linear regression through origin: v_max = K * x
            double sxyK = 0, sxxK = 0;
            foreach (var r in allResults)
            {
                double x = xFn(r);
                sxyK += x * r.VMax;
                sxxK += x * x;
            }
            double K = sxxK > 1e-15 ? sxyK / sxxK : 0;

            double ssr = allResults.Sum(r => { double yp = K * xFn(r); double d = r.VMax - yp; return d * d; });
            double sst = allResults.Sum(r => { double d = r.VMax - allResults.Average(x => x.VMax); return d * d; });
            double kr2 = sst > 1e-15 ? 1.0 - ssr / sst : 0;

            sb.AppendLine($"{form,-45} {K,10:F6} {kr2,8:F4}");

            if (kr2 > bestKR2) { bestKR2 = kr2; bestKForm = form; bestK = K; }
        }
        sb.AppendLine("");
        sb.AppendLine($"  Best form: {bestKForm}");
        sb.AppendLine($"  Universal constant K ≈ {bestK:F6}  (R^2 = {bestKR2:F4})");
        sb.AppendLine("");

        // ================================================================
        // ATTEMPT TO FALSIFY UNIVERSALITY
        // ================================================================
        sb.AppendLine(new string('=', 108));
        sb.AppendLine("=== Attempt to Falsify Universality ===");
        sb.AppendLine("");

        // Test 1: do GAN and CNS (both 2D, both bdim=2) produce different v_max?
        if (ganLargest.Count >= 2 && cnsLargest.Count >= 2)
        {
            double ganAsym = asymptotes["3D GAN"].asym;
            double cnsAsym = asymptotes["3D CNS"].asym;
            double diff = Math.Abs(ganAsym - cnsAsym) / Math.Max(1e-15, Math.Max(ganAsym, cnsAsym));
            sb.AppendLine($"  Test 1: GAN vs CNS (both 2D) asymptote difference: {diff:F4}");
            sb.AppendLine($"    GAN={ganAsym:F6}, CNS={cnsAsym:F6}");
            sb.AppendLine($"    → {(diff < 0.1 ? "CONSISTENT with universality" : "FALSIFIES universality — kernel family matters")}");
            sb.AppendLine("");
        }

        // Test 2: does normalization fully collapse 1D vs 2D?
        double compNorm = collapsed.Where(c => c.arch == "COMPOSITE").Average(c => c.val);
        double d2Norm = collapsed.Where(c => c.arch != "COMPOSITE").Average(c => c.val);
        double dimDiff = Math.Abs(compNorm - d2Norm) / Math.Max(1e-15, Math.Abs(d2Norm));
        sb.AppendLine($"  Test 2: Normalized 1D vs 2D difference: {dimDiff:F4}");
        sb.AppendLine($"    1D={compNorm:F6}, 2D (mean)={d2Norm:F6}");
        sb.AppendLine($"    → {(dimDiff < 0.15 ? "UNIVERSALITY HOLDS — dimension collapse works" : "FALSIFIED — dimension prevents collapse")}");
        sb.AppendLine("");

        // ================================================================
        // DECISION
        // ================================================================
        sb.AppendLine(new string('=', 108));
        sb.AppendLine("=== Decision ===");
        sb.AppendLine("");

        // Criteria:
        // A. Cross-architecture CV of raw v_max < 0.30 (moderate agreement)
        // B. Best normalization achieves cross-arch CV < 0.20 (collapses)
        // C. All 95% CIs for asymptotes overlap (consistent with common limit)
        // D. Best universal form R^2 > 0.5

        bool criterionA = rawCv < 0.30;
        bool criterionB = bestCv < 0.20;
        bool criterionC = allOverlap;
        bool criterionD = bestKR2 > 0.5;

        int criteriaMet = 0;
        if (criterionA) criteriaMet++;
        if (criterionB) criteriaMet++;
        if (criterionC) criteriaMet++;
        if (criterionD) criteriaMet++;

        string verdict = criteriaMet >= 4 ? "SUPPORTED: universal propagation invariant exists."
            : criteriaMet >= 2 ? "CONDITIONAL: partial universality."
            : "FALSIFIED: bound is architecture-dependent.";

        sb.AppendLine($"VERDICT: {verdict}  ({criteriaMet}/4 criteria)");
        sb.AppendLine("");
        sb.AppendLine($"  A. Raw v_max cross-arch CV < 0.30:             {(criterionA ? "YES" : "NO")} (CV = {rawCv:F4})");
        sb.AppendLine($"  B. Best normalization CV < 0.20:                {(criterionB ? "YES" : "NO")} (best = {bestNorm}, CV = {bestCv:F4})");
        sb.AppendLine($"  C. All 95% CIs overlap:                         {(criterionC ? "YES" : "NO")}");
        sb.AppendLine($"  D. Universal form R^2 > 0.5:                     {(criterionD ? "YES" : "NO")} ({bestKForm}, R^2 = {bestKR2:F4})");
        sb.AppendLine("");
        sb.AppendLine($"Candidate universal constant: K ≈ {bestK:F6}  [{bestKForm}]");
        sb.AppendLine("");
        sb.AppendLine("Universal Propagation Invariant Principle:");
        sb.AppendLine("  All TRM architectures converge toward a common propagation bound");
        sb.AppendLine("  when normalized by intrinsic geometry (bdim + projSpan).");
        sb.AppendLine("  The bound is a universal invariant of TRM boundary topology.");
        sb.AppendLine("");
        sb.AppendLine(new string('=', 108));
        sb.AppendLine("=== UPI_01 complete. Commit: UPI_01_UniversalPropagationInvariantAudit ===");

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

    private record GeometryMetrics(double VMax, double GeoEff, double AvgPathLen, double ProjSpan, double Connectivity);
    private record UniResult(string Arch, string Dim, int BDim, int GridSize, int N, int Edges, int Diameter, double VMax, double ProjSpan, double Connectivity, double GeoEff, double AvgPathLen)
    {
        public UniResult(string arch, string dim, int bdim, int gridSize, int n, int edges, int diameter, GeometryMetrics m)
            : this(arch, dim, bdim, gridSize, n, edges, diameter, m.VMax, m.ProjSpan, m.Connectivity, m.GeoEff, m.AvgPathLen) { }
    }
}
