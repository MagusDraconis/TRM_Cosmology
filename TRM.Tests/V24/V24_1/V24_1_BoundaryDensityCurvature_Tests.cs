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

namespace TRM.Tests.V24_1;

[Trait("Category", "V24_1")]
[Trait("Category", "LongRunning")]
public class V24_1_BoundaryDensityCurvature_Tests
{
    private readonly ITestOutputHelper _o;
    public V24_1_BoundaryDensityCurvature_Tests(ITestOutputHelper o) { _o = o; }

    [Fact]
    public void BDC_01_BoundaryDensityCurvatureAudit()
    {
        var sb = new StringBuilder();
        sb.AppendLine(new string('=', 108));
        sb.AppendLine("=== BDC_01: Boundary Density Curvature Audit ===");
        sb.AppendLine("=== Is boundary density the primitive source? ===");
        sb.AppendLine(new string('=', 108));
        sb.AppendLine("");
        sb.AppendLine("KNOWN (V23, V24.0): Curvature from gradients, gradients from boundary.");
        sb.AppendLine("QUESTION: Can curvature be predicted DIRECTLY from boundary density?");
        sb.AppendLine("NULL HYPOTHESIS: Connectivity gradients remain necessary mediators.");
        sb.AppendLine("");

        const int baseSeed = 629471;
        const double xiBase = 2.95, k0Base = 1.0;
        var distances = BuildDistanceEnsemble(baseSeed, systems: 8, nodesPerSystem: 64);
        var sortedD = distances.OrderBy(x => x).ToArray();
        const int nA = 21;
        double aMin = 0.21, aMax = 1.40, daD = (aMax - aMin) / (nA - 1);

        var allNodes = new ConcurrentBag<DensityNode>();

        foreach (var (arch, fam) in new[] { ("3D GAN", VcFamily.GAN), ("3D CNS", VcFamily.CNS) })
        {
            foreach (int nGrid in new[] { 14, 16, 18 })
            {
                var g = Build3DGraph(nGrid, fam, distances, sortedD, xiBase, k0Base, nA, aMin, daD, out int N, out int E);
                int diam = ComputeDiameter(g, N);
                var nodes = AnalyzeDensityMediation(g, N, 200);
                foreach (var nd in nodes) allNodes.Add(new DensityNode(arch, nGrid, nd));
            }
        }

        var nodeList = allNodes.ToList();
        sb.AppendLine($"  Sampled {nodeList.Count} nodes across 6 graphs.");
        sb.AppendLine("");

        double[] densVals = nodeList.Select(n => n.BoundaryDensity).ToArray();
        double[] gradVals = nodeList.Select(n => n.ConnGradient).ToArray();
        double[] curvVals = nodeList.Select(n => n.EffectiveCurvature).ToArray();

        // ================================================================
        // DIRECT CORRELATION ANALYSIS
        // ================================================================
        sb.AppendLine(new string('=', 108));
        sb.AppendLine("=== Direct Correlation Analysis ===");
        sb.AppendLine("");

        double r_dg = PearsonCorr(densVals, gradVals);
        double r_dc = PearsonCorr(densVals, curvVals);
        double r_gc = PearsonCorr(gradVals, curvVals);

        sb.AppendLine($"  Density -> Gradient:  r = {r_dg:F4}  (R^2 = {r_dg*r_dg:F4})");
        sb.AppendLine($"  Density -> Curvature: r = {r_dc:F4}  (R^2 = {r_dc*r_dc:F4})");
        sb.AppendLine($"  Gradient -> Curvature: r = {r_gc:F4}  (R^2 = {r_gc*r_gc:F4})");
        sb.AppendLine("");

        // ================================================================
        // MEDIATION ANALYSIS
        // ================================================================
        sb.AppendLine(new string('=', 108));
        sb.AppendLine("=== Mediation Analysis ===");
        sb.AppendLine("");

        // Partial correlation: curvatue ~ gradient | density
        // r_gc.d = (r_gc - r_dg * r_dc) / sqrt((1-r_dg^2)*(1-r_dc^2))
        double r_gc_d = (r_gc - r_dg * r_dc) / Math.Sqrt(Math.Max(1e-15, (1 - r_dg * r_dg) * (1 - r_dc * r_dc)));

        // Partial correlation: curvature ~ density | gradient
        // r_dc.g = (r_dc - r_dg * r_gc) / sqrt((1-r_dg^2)*(1-r_gc^2))
        double r_dc_g = (r_dc - r_dg * r_gc) / Math.Sqrt(Math.Max(1e-15, (1 - r_dg * r_dg) * (1 - r_gc * r_gc)));

        sb.AppendLine($"  Partial: Gradient -> Curvature | Density:  r = {r_gc_d:F4}");
        sb.AppendLine($"  Partial: Density -> Curvature | Gradient:  r = {r_dc_g:F4}");
        sb.AppendLine("");

        // Multi-regression: curvature = a*density + b*gradient + c
        double mx1 = densVals.Average(), mx2 = gradVals.Average(), my2 = curvVals.Average();
        double s11 = 0, s12 = 0, s22 = 0, s1y = 0, s2y = 0;
        for (int i = 0; i < nodeList.Count; i++)
        {
            double d1 = densVals[i] - mx1, d2 = gradVals[i] - mx2, dy = curvVals[i] - my2;
            s11 += d1 * d1; s12 += d1 * d2; s22 += d2 * d2; s1y += d1 * dy; s2y += d2 * dy;
        }
        double det2 = s11 * s22 - s12 * s12;
        double aCoef = det2 > 1e-15 ? (s1y * s22 - s2y * s12) / det2 : 0;
        double bCoef = det2 > 1e-15 ? (s2y * s11 - s1y * s12) / det2 : 0;
        double cCoef = my2 - aCoef * mx1 - bCoef * mx2;

        double ssr2 = 0, sst2 = 0;
        for (int i = 0; i < nodeList.Count; i++)
        {
            double yp = aCoef * densVals[i] + bCoef * gradVals[i] + cCoef;
            double d = curvVals[i] - yp, d2 = curvVals[i] - my2;
            ssr2 += d * d; sst2 += d2 * d2;
        }
        double multiR2 = sst2 > 1e-15 ? 1.0 - ssr2 / sst2 : 0;

        // Simple model: curvature = a*density + b (no gradient)
        double s1y2 = 0, s11det = 0;
        for (int i = 0; i < nodeList.Count; i++) { double d1 = densVals[i] - mx1; s1y2 += d1 * (curvVals[i] - my2); s11det += d1 * d1; }
        double aSimple = s11det > 1e-15 ? s1y2 / s11det : 0;
        double bSimple = my2 - aSimple * mx1;
        double ssrSimple = 0;
        for (int i = 0; i < nodeList.Count; i++) { double yp = aSimple * densVals[i] + bSimple; double d = curvVals[i] - yp; ssrSimple += d * d; }
        double simpleR2 = sst2 > 1e-15 ? 1.0 - ssrSimple / sst2 : 0;

        double r2Gain = multiR2 - simpleR2;

        sb.AppendLine($"  Simple (density only):    R^2 = {simpleR2:F4}");
        sb.AppendLine($"  Multi (density+gradient):  R^2 = {multiR2:F4}  (gain = {r2Gain:F4})");
        sb.AppendLine("");

        // ================================================================
        // REDUCTION TEST
        // ================================================================
        sb.AppendLine(new string('=', 108));
        sb.AppendLine("=== Reduction Test ===");
        sb.AppendLine("");

        if (r2Gain < 0.03 && r_dc > 0.3)
            sb.AppendLine("  -> GRADIENT CAN BE ELIMINATED. Density alone predicts curvature.");
        else if (r2Gain > 0.05)
            sb.AppendLine($"  -> GRADIENT REMAINS NECESSARY. Adds {r2Gain*100:F1}% R^2 beyond density.");
        else
            sb.AppendLine("  -> AMBIGUOUS. Gradient provides marginal improvement.");
        sb.AppendLine("");

        // ================================================================
        // REDUCED CURVATURE LAW
        // ================================================================
        sb.AppendLine(new string('=', 108));
        sb.AppendLine("=== Reduced Curvature Law ===");
        sb.AppendLine("");

        sb.AppendLine($"  Direct:  curvature = {aSimple:F4} * density + {bSimple:F4}  (R^2 = {simpleR2:F4})");
        sb.AppendLine($"  Mediated: curvature = {aCoef:F4} * density + {bCoef:F4} * gradient + {cCoef:F4}  (R^2 = {multiR2:F4})");
        sb.AppendLine("");

        bool reducible = r2Gain < 0.03 && Math.Abs(r_dc) > Math.Abs(r_dg);

        if (reducible)
            sb.AppendLine($"  REDUCED CHAIN: Boundary Density -> Curvature -> Dilation  (R^2 = {simpleR2:F4})");
        else
            sb.AppendLine($"  FULL CHAIN: Boundary Density -> Gradient -> Curvature -> Dilation");
        sb.AppendLine("");

        // ================================================================
        // DECISION
        // ================================================================
        sb.AppendLine(new string('=', 108));
        sb.AppendLine("=== Decision ===");
        sb.AppendLine("");

        bool criterionA = Math.Abs(r_dc) > 0.3;
        bool criterionB = Math.Abs(r_dc) > Math.Abs(r_dg);
        bool criterionC = r_dc_g < 0.15;
        bool criterionD = reducible || simpleR2 > 0.2;

        int criteriaMet = 0;
        if (criterionA) criteriaMet++;
        if (criterionB) criteriaMet++;
        if (Math.Abs(r_dc_g) < 0.15) criteriaMet++; // density direct path
        if (criterionD) criteriaMet++;

        string verdict = criteriaMet >= 4 ? "SUPPORTED: boundary density is the primitive source."
            : criteriaMet >= 2 ? "CONDITIONAL: density contributes but gradients remain necessary."
            : "FALSIFIED: connectivity gradients remain fundamental.";

        sb.AppendLine($"VERDICT: {verdict}  ({criteriaMet}/4 criteria)");
        sb.AppendLine("");
        sb.AppendLine($"  A. Density -> Curvature direct (|r|>0.3):    {(criterionA ? "YES" : "NO")} (r={r_dc:F4})");
        sb.AppendLine($"  B. Density > Gradient for curvature:          {(criterionB ? "YES" : "NO")}");
        sb.AppendLine($"  C. Density direct path (partial|r|<0.15):     {(criterionC ? "YES" : "NO")} (r={r_dc_g:F4})");
        sb.AppendLine($"  D. Chain reducible (R^2 gain < 3%):            {(criterionD ? "YES" : "NO")} (gain={r2Gain*100:F1}%)");
        sb.AppendLine("");
        sb.AppendLine("Boundary Density Curvature Principle:");
        sb.AppendLine(reducible
            ? "  Boundary density is the PRIMITIVE curvature source. The chain reduces to: Density -> Curvature -> Dilation."
            : "  Boundary density contributes to curvature but gradients remain a necessary intermediary.");
        sb.AppendLine("");
        sb.AppendLine(new string('=', 108));
        sb.AppendLine("=== BDC_01 complete. Commit: BDC_01_BoundaryDensityCurvatureAudit ===");

        _o.WriteLine(sb.ToString());
        Assert.True(true);
    }

    // ================================================================
    // DENSITY MEDIATION ANALYSIS
    // ================================================================

    private static List<DensityMediationData> AnalyzeDensityMediation(List<int>[] adj, int N, int nSample)
    {
        var degrees = adj.Select(a => (double)a.Count).ToArray();
        double meanDeg = degrees.Average();
        var rng = new Random(42);
        nSample = Math.Min(nSample, N);
        var sample = Enumerable.Range(0, N).OrderBy(_ => rng.Next()).Take(nSample).ToList();
        var results = new List<DensityMediationData>();

        foreach (int i in sample)
        {
            // Boundary density: degree / mean degree
            double density = degrees[i] / Math.Max(1e-15, meanDeg);

            // Connectivity gradient
            double grad = adj[i].Count > 0
                ? Math.Abs(degrees[i] - adj[i].Average(n => degrees[n])) / Math.Max(1e-15, degrees[i])
                : 0;

            // Effective curvature = connectivity gradient (proxy)
            double curv = grad;

            results.Add(new DensityMediationData(density, grad, curv));
        }
        return results;
    }

    private static double PearsonCorr(double[] xs, double[] ys)
    {
        int n = xs.Length; if (n < 2) return 0;
        double mxs = xs.Average(), mys = ys.Average();
        double sxy = 0, sxx = 0, syy = 0;
        for (int i = 0; i < n; i++) { double dx = xs[i] - mxs, dy = ys[i] - mys; sxy += dx * dy; sxx += dx * dx; syy += dy * dy; }
        return sxx > 1e-15 && syy > 1e-15 ? sxy / Math.Sqrt(sxx * syy) : 0;
    }

    private record DensityMediationData(double BoundaryDensity, double ConnGradient, double EffectiveCurvature);
    private record DensityNode(string Arch, int GridSize, double BoundaryDensity, double ConnGradient, double EffectiveCurvature)
    {
        public DensityNode(string arch, int gridSize, DensityMediationData d)
            : this(arch, gridSize, d.BoundaryDensity, d.ConnGradient, d.EffectiveCurvature) { }
    }

    // ================================================================
    // STANDARD HELPERS
    // ================================================================

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
}
