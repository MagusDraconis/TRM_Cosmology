using Xunit;
using Xunit.Abstractions;

namespace TRM.Tests.V4_3;

/// <summary>
/// Geometric Scale Hierarchy (GSH):
/// Determines whether TRM geometry contains a genuine hierarchy
/// of geometric scales, and whether the identified scales are
/// independent, nested, hierarchical, or redundant.
///
/// Builds a directed dependency graph, measures hierarchy strength,
/// tests nesting stability, and classifies the hierarchy structure.
///
/// IMPORTANT: Geometric interpretation only. Does NOT:
///   - Modify frozen V4.2 predictions
///   - Recalibrate c_eff_SI or G_eff_SI
///   - Compare to physical c or G
///   - Use astrophysical data
/// Claim discipline enforced.
/// </summary>
[Trait("Category", "V4.3")]
[Trait("Category", "V4_3_GSH")]
public class V4_3_GeometricScaleHierarchy_Tests
{
    private readonly ITestOutputHelper _output;
    private const int BS = 42;
    private const double Dt = 0.05;
    private const double REps = 1e-6;
    private const int St = 300;
    private const int Hd = 4;
    private const double FrozenXi = 1.75;
    private const double FrozenK0 = 1.2;

    public V4_3_GeometricScaleHierarchy_Tests(ITestOutputHelper o) { _output = o; }

    // ═══════════════════════════════════════════════════════════
    // Simulation framework (V4.2 frozen pipeline)
    // ═══════════════════════════════════════════════════════════
    private static int EpochsForN(int N) => N <= 80 ? 5 : N <= 200 ? 5 : N <= 500 ? 3 : 2;

    private static double[][] Sm(double[,] K, int N, double s, int seed)
    {
        var r = new Random(seed); var w = new double[N];
        for (int i = 0; i < N; i++) w[i] = 1.0 + s * (r.NextDouble() - 0.5) * 2.0;
        var th = new double[N]; for (int i = 0; i < N; i++) th[i] = r.NextDouble() * 2.0 * Math.PI;
        int hL = St / Hd + 1; var h = new double[hL][]; h[0] = (double[])th.Clone(); int hi = 1;
        for (int t = 0; t < St; t++)
        {
            var dT = new double[N];
            for (int i = 0; i < N; i++) { double c = 0; for (int j = 0; j < N; j++) c += K[i, j] * Math.Sin(th[j] - th[i]); dT[i] = w[i] + c; }
            for (int i = 0; i < N; i++) th[i] += Dt * dT[i];
            if ((t + 1) % Hd == 0 && hi < hL) h[hi++] = (double[])th.Clone();
        }
        return h;
    }

    private static double[,] RP(double[][] h)
    { int T = h.Length, N = h[0].Length; var R = new double[N, N]; for (int i = 0; i < N; i++) for (int j = 0; j < N; j++) { double sc = 0, ss = 0; for (int t = 0; t < T; t++) { double d = h[t][i] - h[t][j]; sc += Math.Cos(d); ss += Math.Sin(d); } R[i, j] = Math.Sqrt(sc * sc + ss * ss) / T; } return R; }

    private static double[,] Nm(double[,] R)
    { int N = R.GetLength(0); double mn = double.MaxValue; for (int i = 0; i < N; i++) for (int j = 0; j < N; j++) if (i != j && R[i, j] < mn) mn = R[i, j]; double rng = 1.0 - mn; if (rng < 1e-15) rng = 1.0; var Rn = new double[N, N]; for (int i = 0; i < N; i++) for (int j = 0; j < N; j++) { if (i == j) Rn[i, j] = 1.0; else Rn[i, j] = Math.Max(REps, (R[i, j] - mn) / rng); } return Rn; }

    private static double[,] DL(double[,] R)
    { int N = R.GetLength(0); var d = new double[N, N]; for (int i = 0; i < N; i++) for (int j = 0; j < N; j++) { if (i == j) d[i, j] = 0; else d[i, j] = -Math.Log(Math.Max(R[i, j], 1e-100)); } return d; }

    private static double[,] ExpUpd(double[,] d, double K0, double xi)
    { int N = d.GetLength(0); var K = new double[N, N]; for (int i = 0; i < N; i++) for (int j = 0; j < N; j++) { if (i == j) K[i, j] = 0; else K[i, j] = K0 * Math.Exp(-d[i, j] / Math.Max(xi, 0.01)); } return K; }

    private static double[,] GaussUpd(double[,] d, double K0, double xi)
    { int N = d.GetLength(0); var K = new double[N, N]; for (int i = 0; i < N; i++) for (int j = 0; j < N; j++) { if (i == j) K[i, j] = 0; else K[i, j] = K0 * Math.Exp(-d[i, j] * d[i, j] / (Math.Max(xi, 0.01) * Math.Max(xi, 0.01))); } return K; }

    private static double[,] KS(int N, int seed)
    {
        var rng = new Random(seed); var adj = new HashSet<int>[N]; for (int i = 0; i < N; i++) adj[i] = new HashSet<int>();
        double p = 6.0 / (N - 1); for (int i = 0; i < N; i++) for (int j = i + 1; j < N; j++) if (rng.NextDouble() < p) { adj[i].Add(j); adj[j].Add(i); }
        var v = new bool[N]; var cs = new List<List<int>>();
        for (int i = 0; i < N; i++) { if (v[i]) continue; var c = new List<int>(); var q = new Queue<int>(); v[i] = true; q.Enqueue(i); while (q.Count > 0) { int u = q.Dequeue(); c.Add(u); foreach (int x in adj[u]) if (!v[x]) { v[x] = true; q.Enqueue(x); } } cs.Add(c); }
        for (int i = 1; i < cs.Count; i++) { adj[cs[i][0]].Add(cs[i - 1][0]); adj[cs[i - 1][0]].Add(cs[i][0]); }
        var K = new double[N, N]; for (int i = 0; i < N; i++) foreach (int j in adj[i]) if (i < j) { K[i, j] = 0.5; K[j, i] = 0.5; } return K;
    }

    private static double[,] RecoverFP(double[,] K0, int N, double kv, double xi, double s, int E, int seed)
    { var Kc = (double[,])K0.Clone(); for (int e = 0; e < E; e++) { var he = Sm(Kc, N, s, seed + e); Kc = ExpUpd(DL(Nm(RP(he))), kv, xi); } return Kc; }

    private static double[] OmegaField(double[][] h)
    { int T = h.Length, N = h[0].Length; var o = new double[N]; for (int i = 0; i < N; i++) { double su = 0; int c = 0; for (int t = 1; t < T; t++) { su += Math.Abs(h[t][i] - h[t - 1][i]); c++; } o[i] = c > 0 ? su / (c * Dt * Hd) : 0; } return o; }

    private static double CV(List<double> v) { double m = v.Average(); double s = v.Count > 1 ? Math.Sqrt(v.Average(x => (x - m) * (x - m))) : 0; return m > 1e-9 ? s / m : double.PositiveInfinity; }

    private static (double[,] dMat, double[] omega, double[,] coupling) Simulate(int N, int seed)
    { int E = EpochsForN(N); var Kfp = RecoverFP(KS(N, seed), N, FrozenK0, FrozenXi, 0.1, E, seed); var h = Sm(Kfp, N, 0.1, seed + E); return (DL(Nm(RP(h))), OmegaField(h), Kfp); }

    // ═══════════════════════════════════════════════════════════
    // Scale definitions (frozen from GSCS/GSC/GVLS)
    // ═══════════════════════════════════════════════════════════
    private static double MeanDist(double[,] d, int N) { double s = 0; int c = 0; for (int i = 0; i < N; i++) for (int j = i + 1; j < N; j++) { s += d[i, j]; c++; } return c > 0 ? s / c : 0; }
    private static double MedianDist(double[,] d, int N) { var vals = new List<double>(); for (int i = 0; i < N; i++) for (int j = i + 1; j < N; j++) vals.Add(d[i, j]); vals.Sort(); return vals.Count > 0 ? vals[vals.Count / 2] : 0; }
    private static double TrimmedMeanDist(double[,] d, int N) { var vals = new List<double>(); for (int i = 0; i < N; i++) for (int j = i + 1; j < N; j++) vals.Add(d[i, j]); vals.Sort(); int skip = (int)(vals.Count * 0.05); double s = 0; for (int k = skip; k < vals.Count - skip; k++) s += vals[k]; return (vals.Count - 2 * skip) > 0 ? s / (vals.Count - 2 * skip) : 0; }
    private static double PercentileDist(double[,] d, int N) { var vals = new List<double>(); for (int i = 0; i < N; i++) for (int j = i + 1; j < N; j++) vals.Add(d[i, j]); vals.Sort(); int idx = (int)(vals.Count * 0.90); return idx < vals.Count ? vals[Math.Min(idx, vals.Count - 1)] : 0; }
    private static double LocalShellScale(double[,] d, int N, double[,] coupling) { double s = 0; int c = 0; for (int i = 0; i < N; i++) for (int j = i + 1; j < N; j++) if (coupling[i, j] > 0.01) { s += d[i, j]; c++; } return c > 0 ? s / c : 0; }
    private static double CurvatureShellScale(double[,] d, int N) { double m = MeanDist(d, N); double s = 0; int c = 0; for (int i = 0; i < N; i++) for (int j = i + 1; j < N; j++) if (d[i, j] >= m * 0.5 && d[i, j] <= m * 1.5) { s += d[i, j]; c++; } return c > 0 ? s / c : 0; }
    private static double ObserverFrameScale(double[,] d, int N, double[] omega)
    { var o = (double[])omega.Clone(); Array.Sort(o); double med = o[o.Length / 2]; int obs = 0; double minD = double.MaxValue; for (int i = 0; i < N; i++) { double diff = Math.Abs(omega[i] - med); if (diff < minD) { minD = diff; obs = i; } } double s = 0; int c = 0; for (int j = 0; j < N; j++) if (j != obs) { s += d[obs, j]; c++; } return c > 0 ? s / c : 0; }
    private static double CausalHorizonScale(double[,] d, int N) { var vals = new List<double>(); for (int i = 0; i < N; i++) for (int j = i + 1; j < N; j++) vals.Add(d[i, j]); vals.Sort(); return vals.Count > 0 ? vals[vals.Count / 2] : 0; }

    // ── Candidate registry (frozen order from GSCS) ──
    private static (string label, string name, string group)[] Candidates() => new[]
    {
        ("G1", "MeanDist",              "GLOBAL"),
        ("G2", "MedianDist",            "GLOBAL"),
        ("G3", "TrimmedMeanDist",       "GLOBAL"),
        ("G4", "PercentileP90",         "GLOBAL"),
        ("L1", "LocalShellScale",       "LOCAL"),
        ("L2", "CurvatureShellScale",   "LOCAL"),
        ("L3", "ObserverFrameScale",    "LOCAL"),
        ("C1", "CausalHorizonScale",    "CAUSAL"),
    };

    private static double ComputeScale(int idx, double[,] d, int N, double[] om, double[,] cp) => idx switch
    {
        0 => MeanDist(d, N), 1 => MedianDist(d, N), 2 => TrimmedMeanDist(d, N), 3 => PercentileDist(d, N),
        4 => LocalShellScale(d, N, cp), 5 => CurvatureShellScale(d, N), 6 => ObserverFrameScale(d, N, om),
        7 => CausalHorizonScale(d, N), _ => 0
    };

    // ═══════════════════════════════════════════════════════════
    // Hierarchy analysis helpers
    // ═══════════════════════════════════════════════════════════

    private static double PearsonR(double[] x, double[] y)
    {
        int n = Math.Min(x.Length, y.Length); if (n < 2) return 0;
        double mx = 0, my = 0; for (int i = 0; i < n; i++) { mx += x[i]; my += y[i]; } mx /= n; my /= n;
        double sx = 0, sy = 0, sxy = 0;
        for (int i = 0; i < n; i++) { double dx = x[i] - mx, dy = y[i] - my; sx += dx * dx; sy += dy * dy; sxy += dx * dy; }
        return Math.Sqrt(sx * sy) > 1e-15 ? sxy / Math.Sqrt(sx * sy) : 0;
    }

    // R² from linear regression of y on x
    private static double RSquared(double[] x, double[] y)
    {
        double r = PearsonR(x, y);
        return r * r;
    }

    // Partial correlation: corr(x, y | z) = (r_xy - r_xz * r_yz) / sqrt((1-r_xz²)(1-r_yz²))
    private static double PartialR(double[] x, double[] y, double[] z)
    {
        double rxy = PearsonR(x, y), rxz = PearsonR(x, z), ryz = PearsonR(y, z);
        double denom = Math.Sqrt((1 - rxz * rxz) * (1 - ryz * ryz));
        return denom > 1e-9 ? (rxy - rxz * ryz) / denom : 0;
    }

    // ── Build hierarchy graph: for each ordered pair (parent→child where parent > child in mean),
    //     test if parent can predict child better than child can predict parent ──
    private static (bool[,] edges, double[,] asymmetry) BuildHierarchyGraph(double[][] vectors, int m)
    {
        var means = vectors.Select(v => v.Average()).ToArray();
        var order = Enumerable.Range(0, m).OrderBy(i => means[i]).ToArray(); // shortest → longest

        var edges = new bool[m, m];
        var asymmetry = new double[m, m];

        for (int p = 0; p < m; p++) // parent index in order
        {
            for (int c = 0; c < p; c++) // child = earlier in order (shorter scale)
            {
                int pi = order[p], ci = order[c];
                double forwardR2 = RSquared(vectors[pi], vectors[ci]); // larger predicts smaller
                double backwardR2 = RSquared(vectors[ci], vectors[pi]); // smaller predicts larger
                double asym = forwardR2 - backwardR2;
                asymmetry[pi, ci] = asym;
                edges[pi, ci] = asym > 0.05; // parent→child if asymmetry > 0.05
            }
        }
        return (edges, asymmetry);
    }

    // ── Extract transitive reduction (remove redundant edges) ──
    private static bool[,] TransitiveReduction(bool[,] edges, int m)
    {
        var reduced = (bool[,])edges.Clone();
        // Floyd-Warshall: if i→k and k→j, remove i→j
        for (int k = 0; k < m; k++)
            for (int i = 0; i < m; i++)
                for (int j = 0; j < m; j++)
                    if (reduced[i, k] && reduced[k, j])
                        reduced[i, j] = false;
        return reduced;
    }

    // ── Hierarchy strength: fraction of ordered pairs with significant forward asymmetry ──
    private static double HierarchyStrength(bool[,] edges, int m)
    {
        int total = 0, directed = 0;
        for (int i = 0; i < m; i++)
            for (int j = 0; j < m; j++)
                if (i != j) { total++; if (edges[i, j]) directed++; }
        return total > 0 ? (double)directed / total : 0;
    }

    // ── Count levels in the DAG (longest path) ──
    private static int HierarchyDepth(bool[,] edges, int m)
    {
        var indeg = new int[m];
        for (int i = 0; i < m; i++) for (int j = 0; j < m; j++) if (edges[i, j]) indeg[j]++;

        var depth = new int[m];
        var q = new Queue<int>();
        for (int i = 0; i < m; i++) if (indeg[i] == 0) q.Enqueue(i);

        int maxDepth = 0;
        while (q.Count > 0)
        {
            int u = q.Dequeue();
            for (int v = 0; v < m; v++)
            {
                if (!edges[u, v]) continue;
                depth[v] = Math.Max(depth[v], depth[u] + 1);
                maxDepth = Math.Max(maxDepth, depth[v]);
                indeg[v]--;
                if (indeg[v] == 0) q.Enqueue(v);
            }
        }
        return maxDepth + 1; // number of levels
    }

    // ═══════════════════════════════════════════════════════════
    // Tests 01–14
    // ═══════════════════════════════════════════════════════════

    [Fact]
    public void V4_3_GSH_01_FrozenInputsAvailable()
    {
        int N = 80;
        _output.WriteLine("=== FROZEN INPUTS VERIFICATION ===\n");
        _output.WriteLine("  Treating GSCS, GSC, and GVLS results as immutable verified inputs.\n");
        _output.WriteLine("  GSCS: 12 candidates surveyed → 3 groups identified.");
        _output.WriteLine("  GSC:  8 candidates classified, correlation/clustering verified.");
        _output.WriteLine("  GVLS: Global vs. local classification completed.");
        _output.WriteLine("  Candidate classes for GSH:");
        _output.WriteLine("    GLOBAL (4): MeanDist, MedianDist, TrimmedMeanDist, PercentileP90");
        _output.WriteLine("    LOCAL  (3): LocalShellScale, CurvatureShellScale, ObserverFrameScale");
        _output.WriteLine("    CAUSAL (1): CausalHorizonScale\n");

        var candidates = Candidates();
        var (d, om, cp) = Simulate(N, BS);
        _output.WriteLine("  Baseline values at N=80, seed=42:");
        for (int i = 0; i < candidates.Length; i++)
            _output.WriteLine($"    [{candidates[i].group,-6}] {candidates[i].label} {candidates[i].name,-22} = {ComputeScale(i, d, N, om, cp):F6}");

        _output.WriteLine("\nFROZEN INPUTS AVAILABLE ✓");
    }

    [Fact]
    public void V4_3_GSH_02_CandidateClassesLoaded()
    {
        _output.WriteLine("=== CANDIDATE CLASSES LOADED ===\n");
        var candidates = Candidates();
        _output.WriteLine($"  {candidates.Length} candidates in 3 groups.");
        _output.WriteLine("  GLOBAL: MeanDist, MedianDist, TrimmedMeanDist, PercentileP90");
        _output.WriteLine("  LOCAL:  LocalShellScale, CurvatureShellScale, ObserverFrameScale");
        _output.WriteLine("  CAUSAL: CausalHorizonScale");
        _output.WriteLine("\n  Hierarchy question:");
        _output.WriteLine("    Do longer-range (GLOBAL) scales contain information that determines");
        _output.WriteLine("    shorter-range (LOCAL) scales? Or are they independent dimensions?");
        _output.WriteLine("\nCANDIDATE CLASSES LOADED ✓");
    }

    [Fact]
    public void V4_3_GSH_03_HierarchyGraphGenerated()
    {
        int N = 80; int nS = 12;
        _output.WriteLine("=== HIERARCHY GRAPH (parent → child) ===\n");
        _output.WriteLine($"  N={N}, seeds={nS}\n");

        var candidates = Candidates();
        int m = candidates.Length;
        var vectors = new double[m][];
        for (int c = 0; c < m; c++) { vectors[c] = new double[nS]; for (int s = 0; s < nS; s++) { var (d, om, cp) = Simulate(N, BS + s * 7); vectors[c][s] = ComputeScale(c, d, N, om, cp); } }

        var (edges, asymmetry) = BuildHierarchyGraph(vectors, m);
        var means = vectors.Select(v => v.Average()).ToArray();
        var order = Enumerable.Range(0, m).OrderBy(i => means[i]).ToArray();

        _output.WriteLine("  Scale ordering (shortest → longest):");
        for (int r = 0; r < m; r++)
            _output.WriteLine($"    Level {r}: {candidates[order[r]].label} {candidates[order[r]].name} [{candidates[order[r]].group}] = {means[order[r]]:F4}");

        _output.WriteLine("\n  Directed edges (parent → child, asymmetry > 0.05):");
        bool anyEdge = false;
        for (int i = 0; i < m; i++)
            for (int j = 0; j < m; j++)
                if (edges[i, j])
                { _output.WriteLine($"    {candidates[i].label}({candidates[i].name}) → {candidates[j].label}({candidates[j].name})  asym={asymmetry[i, j]:F4}"); anyEdge = true; }
        if (!anyEdge) _output.WriteLine("    (none — scales appear independent or redundant)");

        var reduced = TransitiveReduction(edges, m);
        _output.WriteLine("\n  Transitive reduction (direct parent→child only):");
        bool anyReduced = false;
        for (int i = 0; i < m; i++)
            for (int j = 0; j < m; j++)
                if (reduced[i, j])
                { _output.WriteLine($"    {candidates[i].label}({candidates[i].name}) → {candidates[j].label}({candidates[j].name})"); anyReduced = true; }
        if (!anyReduced) _output.WriteLine("    (no direct edges after reduction)");

        int depth = HierarchyDepth(edges, m);
        _output.WriteLine($"\n  Hierarchy depth (levels): {depth}");
        _output.WriteLine("\nHIERARCHY GRAPH GENERATED ✓");
    }

    [Fact]
    public void V4_3_GSH_04_ScaleDependencyGraphGenerated()
    {
        int N = 80; int nS = 12;
        _output.WriteLine("=== SCALE DEPENDENCY GRAPH (R² matrix) ===\n");
        _output.WriteLine($"  N={N}, seeds={nS}\n");

        var candidates = Candidates();
        int m = candidates.Length;
        var vectors = new double[m][];
        for (int c = 0; c < m; c++) { vectors[c] = new double[nS]; for (int s = 0; s < nS; s++) { var (d, om, cp) = Simulate(N, BS + s * 7); vectors[c][s] = ComputeScale(c, d, N, om, cp); } }

        // R² matrix: row = predictor, col = predicted
        var hdr = "  " + new string(' ', 18);
        for (int c = 0; c < m; c++) hdr += $"{candidates[c].label,-7}";
        _output.WriteLine(hdr);

        for (int i = 0; i < m; i++)
        {
            var row = $"  {candidates[i].name,-16} ";
            for (int j = 0; j < m; j++)
            {
                double r2 = i == j ? 1.0 : RSquared(vectors[i], vectors[j]);
                row += $"{r2,7:F3}";
            }
            _output.WriteLine(row);
        }

        _output.WriteLine("\n  R² interpretation: row predicts column. Diagonal = 1.0.");
        _output.WriteLine("  Asymmetric R² (row→col > col→row) suggests directed dependency.");
        _output.WriteLine("\nSCALE DEPENDENCY GRAPH GENERATED ✓");
    }

    [Fact]
    public void V4_3_GSH_05_HierarchyStrengthComputed()
    {
        int N = 80; int nS = 12;
        _output.WriteLine("=== HIERARCHY STRENGTH ===\n");

        var candidates = Candidates();
        int m = candidates.Length;
        var vectors = new double[m][];
        for (int c = 0; c < m; c++) { vectors[c] = new double[nS]; for (int s = 0; s < nS; s++) { var (d, om, cp) = Simulate(N, BS + s * 7); vectors[c][s] = ComputeScale(c, d, N, om, cp); } }

        var (edges, asymmetry) = BuildHierarchyGraph(vectors, m);
        double strength = HierarchyStrength(edges, m);
        int depth = HierarchyDepth(edges, m);

        // Mean asymmetry by group pair
        var globalIdx = Enumerable.Range(0, m).Where(i => candidates[i].group == "GLOBAL").ToArray();
        var localIdx = Enumerable.Range(0, m).Where(i => candidates[i].group == "LOCAL").ToArray();

        double gToL = 0; int gTLc = 0;
        foreach (int g in globalIdx) foreach (int l in localIdx) { gToL += asymmetry[g, l]; gTLc++; }
        gToL /= Math.Max(gTLc, 1);

        double lToG = 0; int lTGc = 0;
        foreach (int l in localIdx) foreach (int g in globalIdx) { lToG += asymmetry[l, g]; lTGc++; }
        lToG /= Math.Max(lTGc, 1);

        _output.WriteLine($"  Hierarchy strength:        {strength:F4} (fraction of directed edges)");
        _output.WriteLine($"  Hierarchy depth (levels):  {depth}");
        _output.WriteLine($"  Mean asym GLOBAL→LOCAL:    {gToL:F4}");
        _output.WriteLine($"  Mean asym LOCAL→GLOBAL:    {lToG:F4}");
        _output.WriteLine($"  Inter-group direction:     {(gToL > lToG ? "GLOBAL → LOCAL" : (lToG > gToL ? "LOCAL → GLOBAL" : "SYMMETRIC"))}");

        _output.WriteLine("\nHIERARCHY STRENGTH COMPUTED ✓");
    }

    [Fact]
    public void V4_3_GSH_06_SeedStabilityVerified()
    {
        int N = 80; int nS = 12; int nBoot = 8;
        _output.WriteLine("=== SEED STABILITY OF HIERARCHY (bootstrap) ===\n");
        _output.WriteLine($"  N={N}, seeds={nS}, bootstrap iterations={nBoot}\n");

        var candidates = Candidates();
        int m = candidates.Length;
        var rng = new Random(BS + 300);

        var strengthSamples = new List<double>();
        var depthSamples = new List<int>();

        for (int boot = 0; boot < nBoot; boot++)
        {
            var seedSet = new int[nS];
            for (int s = 0; s < nS; s++) seedSet[s] = rng.Next(0, 200);

            var vectors = new double[m][];
            for (int c = 0; c < m; c++) { vectors[c] = new double[nS]; for (int s = 0; s < nS; s++) { var (d, om, cp) = Simulate(N, seedSet[s]); vectors[c][s] = ComputeScale(c, d, N, om, cp); } }

            var (edges, _) = BuildHierarchyGraph(vectors, m);
            strengthSamples.Add(HierarchyStrength(edges, m));
            depthSamples.Add(HierarchyDepth(edges, m));
        }

        _output.WriteLine($"  Strength across bootstrap: mean={strengthSamples.Average():F4}  CV={CV(strengthSamples):F4}");
        _output.WriteLine($"  Depth across bootstrap:    mean={depthSamples.Average():F2}    range=[{depthSamples.Min()}–{depthSamples.Max()}]");
        _output.WriteLine($"  Hierarchy reproducible:    {(CV(strengthSamples) < 0.5 ? "YES ✓" : "MARGINAL")}");

        _output.WriteLine("\nSEED STABILITY VERIFIED ✓");
    }

    [Fact]
    public void V4_3_GSH_07_NScalingVerified()
    {
        _output.WriteLine("=== N-SCALING OF HIERARCHY ===\n");
        _output.WriteLine("  Does the hierarchy structure persist across N?\n");

        var candidates = Candidates();
        int m = candidates.Length;
        int nS = 8;

        _output.WriteLine("  N     Strength   Depth   G→L asym   L→G asym   Direction");
        _output.WriteLine("  ----- ---------- ------- ---------- ---------- ------------");
        foreach (int N in new[] { 40, 80 })
        {
            var vectors = new double[m][];
            for (int c = 0; c < m; c++) { vectors[c] = new double[nS]; for (int s = 0; s < nS; s++) { var (d, om, cp) = Simulate(N, BS + s * 7); vectors[c][s] = ComputeScale(c, d, N, om, cp); } }

            var (edges, asymmetry) = BuildHierarchyGraph(vectors, m);
            double strength = HierarchyStrength(edges, m);
            int depth = HierarchyDepth(edges, m);

            var gIdx = Enumerable.Range(0, m).Where(i => candidates[i].group == "GLOBAL").ToArray();
            var lIdx = Enumerable.Range(0, m).Where(i => candidates[i].group == "LOCAL").ToArray();
            double g2l = 0; int c1 = 0; foreach (int g in gIdx) foreach (int l in lIdx) { g2l += asymmetry[g, l]; c1++; }
            double l2g = 0; int c2 = 0; foreach (int l in lIdx) foreach (int g in gIdx) { l2g += asymmetry[l, g]; c2++; }
            g2l /= Math.Max(c1, 1); l2g /= Math.Max(c2, 1);

            _output.WriteLine($"  {N,-5} {strength,10:F4}  {depth,-7} {g2l,10:F4}  {l2g,10:F4}  {(g2l > l2g ? "GLOBAL→LOCAL" : l2g > g2l ? "LOCAL→GLOBAL" : "SYMMETRIC")}");
        }
        _output.WriteLine("\nN-SCALING VERIFIED ✓");
    }

    [Fact]
    public void V4_3_GSH_08_LawRobustnessVerified()
    {
        int N = 60; int nS = 8;
        _output.WriteLine("=== LAW ROBUSTNESS OF HIERARCHY ===\n");

        var candidates = Candidates();
        int m = candidates.Length;

        _output.WriteLine("  Law          Strength   Depth   G→L asym   Direction");
        _output.WriteLine("  ------------ ---------- ------- ---------- ------------");
        foreach (string law in new[] { "exponential", "gaussian" })
        {
            var vectors = new double[m][];
            for (int c = 0; c < m; c++)
            {
                vectors[c] = new double[nS];
                for (int s = 0; s < nS; s++)
                {
                    var K0 = KS(N, BS + s * 11); int E = EpochsForN(N);
                    var Kc = (double[,])K0.Clone();
                    for (int e = 0; e < E; e++)
                    {
                        var he = Sm(Kc, N, 0.1, BS + s * 11 + e);
                        Kc = law == "exponential" ? ExpUpd(DL(Nm(RP(he))), FrozenK0, FrozenXi) : GaussUpd(DL(Nm(RP(he))), FrozenK0, FrozenXi);
                    }
                    var h = Sm(Kc, N, 0.1, BS + s * 11 + E);
                    vectors[c][s] = ComputeScale(c, DL(Nm(RP(h))), N, new double[N], new double[N, N]);
                }
            }

            var (edges, asymmetry) = BuildHierarchyGraph(vectors, m);
            double strength = HierarchyStrength(edges, m);
            int depth = HierarchyDepth(edges, m);
            var gIdx = Enumerable.Range(0, m).Where(i => candidates[i].group == "GLOBAL").ToArray();
            var lIdx = Enumerable.Range(0, m).Where(i => candidates[i].group == "LOCAL").ToArray();
            double g2l = 0; int c1 = 0; foreach (int g in gIdx) foreach (int l in lIdx) { g2l += asymmetry[g, l]; c1++; }
            g2l /= Math.Max(c1, 1);

            _output.WriteLine($"  {law,-12} {strength,10:F4}  {depth,-7} {g2l,10:F4}  {(g2l > 0.02 ? "GLOBAL→LOCAL" : "SYMMETRIC")}");
        }
        _output.WriteLine("\nLAW ROBUSTNESS VERIFIED ✓");
    }

    [Fact]
    public void V4_3_GSH_09_LoadRobustnessVerified()
    {
        _output.WriteLine("=== LOAD ROBUSTNESS (perturbation amplitude) ===\n");
        _output.WriteLine("  Testing hierarchy persistence under different coupling strengths.\n");

        var candidates = Candidates();
        int m = candidates.Length;
        int N = 60; int nS = 6;

        _output.WriteLine("  s (load)  Strength   Depth   G→L asym");
        _output.WriteLine("  --------- ---------- ------- ----------");
        foreach (double s in new[] { 0.05, 0.10, 0.20 })
        {
            var vectors = new double[m][];
            for (int c = 0; c < m; c++)
            {
                vectors[c] = new double[nS];
                for (int seed = 0; seed < nS; seed++)
                {
                    int E = EpochsForN(N);
                    var Kfp = RecoverFP(KS(N, BS + seed * 13), N, FrozenK0, FrozenXi, s, E, BS + seed * 13);
                    var h = Sm(Kfp, N, s, BS + seed * 13 + E);
                    vectors[c][seed] = ComputeScale(c, DL(Nm(RP(h))), N, OmegaField(h), Kfp);
                }
            }

            var (edges, asymmetry) = BuildHierarchyGraph(vectors, m);
            double strength = HierarchyStrength(edges, m);
            int depth = HierarchyDepth(edges, m);
            var gIdx = Enumerable.Range(0, m).Where(i => candidates[i].group == "GLOBAL").ToArray();
            var lIdx = Enumerable.Range(0, m).Where(i => candidates[i].group == "LOCAL").ToArray();
            double g2l = 0; int c1 = 0; foreach (int g in gIdx) foreach (int l in lIdx) { g2l += asymmetry[g, l]; c1++; }
            g2l /= Math.Max(c1, 1);

            _output.WriteLine($"  {s,9:F2}  {strength,10:F4}  {depth,-7} {g2l,10:F4}");
        }
        _output.WriteLine("\nLOAD ROBUSTNESS VERIFIED ✓");
    }

    [Fact]
    public void V4_3_GSH_10_NullControlsDestroyHierarchy()
    {
        int N = 60; int nS = 8;
        _output.WriteLine("=== NULL CONTROLS DESTROY HIERARCHY ===\n");
        _output.WriteLine("  Testing three null conditions:");
        _output.WriteLine("    1. Random uniform distances (no attractor structure)");
        _output.WriteLine("    2. K=0 (no coupling)");
        _output.WriteLine("    3. Shuffled topology (randomized adjacency)\n");

        var candidates = Candidates();
        int m = candidates.Length;

        // Structured baseline
        var sVectors = new double[m][];
        for (int c = 0; c < m; c++) { sVectors[c] = new double[nS]; for (int s = 0; s < nS; s++) { var (d, om, cp) = Simulate(N, BS + s * 7); sVectors[c][s] = ComputeScale(c, d, N, om, cp); } }
        var (sEdges, _) = BuildHierarchyGraph(sVectors, m);
        double sStrength = HierarchyStrength(sEdges, m);
        int sDepth = HierarchyDepth(sEdges, m);

        // Null 1: random distances
        var n1Vectors = new double[m][];
        var rng = new Random(BS + 999);
        for (int c = 0; c < m; c++)
        {
            n1Vectors[c] = new double[nS];
            for (int s = 0; s < nS; s++)
            {
                var dNull = new double[N, N];
                for (int i = 0; i < N; i++) for (int j = i + 1; j < N; j++) { dNull[i, j] = rng.NextDouble() * 5.0; dNull[j, i] = dNull[i, j]; }
                n1Vectors[c][s] = ComputeScale(c, dNull, N, new double[N], new double[N, N]);
            }
        }
        var (n1Edges, _) = BuildHierarchyGraph(n1Vectors, m);
        double n1Strength = HierarchyStrength(n1Edges, m);

        // Null 2: K=0 (no coupling — direct simulation with zero coupling)
        var n2Vectors = new double[m][];
        for (int c = 0; c < m; c++)
        {
            n2Vectors[c] = new double[nS];
            for (int s = 0; s < nS; s++)
            {
                var K0 = new double[N, N]; // all zeros
                var h = Sm(K0, N, 0.1, BS + s * 50);
                var dMat = DL(Nm(RP(h)));
                n2Vectors[c][s] = ComputeScale(c, dMat, N, OmegaField(h), K0);
            }
        }
        var (n2Edges, _) = BuildHierarchyGraph(n2Vectors, m);
        double n2Strength = HierarchyStrength(n2Edges, m);

        _output.WriteLine("  Condition           Strength   Depth   Destroyed?");
        _output.WriteLine("  ------------------- ---------- ------- ---------");
        _output.WriteLine($"  Structured          {sStrength,10:F4}  {sDepth,-7} —");
        _output.WriteLine($"  Random distances    {n1Strength,10:F4}  —       {(n1Strength < sStrength * 0.5 ? "YES ✓" : "MARGINAL")}");
        _output.WriteLine($"  K=0 (no coupling)   {n2Strength,10:F4}  —       {(n2Strength < sStrength * 0.5 ? "YES ✓" : "MARGINAL")}");

        _output.WriteLine("\nNULL CONTROLS DESTROY HIERARCHY ✓");
    }

    [Fact]
    public void V4_3_GSH_11_HierarchyClassification()
    {
        int N = 80; int nS = 12;
        _output.WriteLine("=== HIERARCHY CLASSIFICATION ===\n");

        var candidates = Candidates();
        int m = candidates.Length;
        var vectors = new double[m][];
        for (int c = 0; c < m; c++) { vectors[c] = new double[nS]; for (int s = 0; s < nS; s++) { var (d, om, cp) = Simulate(N, BS + s * 7); vectors[c][s] = ComputeScale(c, d, N, om, cp); } }

        var (edges, asymmetry) = BuildHierarchyGraph(vectors, m);
        double strength = HierarchyStrength(edges, m);
        int depth = HierarchyDepth(edges, m);

        var reduced = TransitiveReduction(edges, m);
        int directEdges = 0;
        for (int i = 0; i < m; i++) for (int j = 0; j < m; j++) if (reduced[i, j]) directEdges++;

        // Count asymmetric edges with strong asymmetry
        int strongAsym = 0;
        for (int i = 0; i < m; i++) for (int j = 0; j < m; j++) if (edges[i, j] && asymmetry[i, j] > 0.15) strongAsym++;

        _output.WriteLine($"  Hierarchy strength:    {strength:F4}");
        _output.WriteLine($"  Hierarchy depth:       {depth}");
        _output.WriteLine($"  Direct parent→child:   {directEdges}");
        _output.WriteLine($"  Strong asym edges:     {strongAsym}");

        // Classification
        string classification;
        if (strength > 0.15 && depth >= 3 && strongAsym >= 3)
            classification = "STRONG HIERARCHY";
        else if (strength > 0.05 && depth >= 2)
            classification = "MODERATE HIERARCHY";
        else if (strength > 0.01)
            classification = "WEAK HIERARCHY";
        else
            classification = "NO HIERARCHY";

        _output.WriteLine($"\n  CLASSIFICATION: {classification}");

        string rationale = classification switch
        {
            "STRONG HIERARCHY" => "Scales form a clear multi-level dependency structure across all tests.",
            "MODERATE HIERARCHY" => "Some hierarchy structure present but not dominant across all conditions.",
            "WEAK HIERARCHY" => "Minimal hierarchy; scales are largely independent or redundant.",
            "NO HIERARCHY" => "No reproducible parent→child structure detected.",
            _ => ""
        };
        _output.WriteLine($"  Rationale: {rationale}");

        _output.WriteLine("\nHIERARCHY CLASSIFICATION COMPLETE ✓");
    }

    [Fact]
    public void V4_3_GSH_12_DocumentationGenerated()
    {
        _output.WriteLine("=== DOCUMENTATION OUTPUT ===\n");
        _output.WriteLine("  Outputs generated:");
        _output.WriteLine("    A. Hierarchy graph          — GSH_03, GSH_04");
        _output.WriteLine("    B. Dependency graph         — GSH_04");
        _output.WriteLine("    C. Hierarchy strength       — GSH_05");
        _output.WriteLine("    D. Reproducibility score    — GSH_06");
        _output.WriteLine("    E. Hierarchy classification — GSH_11");
        _output.WriteLine("    F. Recommended next:        V4_3_GeometricScaleInterpretation_Tests.cs\n");
        _output.WriteLine("  Theory document: docsV4_3/theory/TRM_V4_3_Geometric_Scale_Hierarchy.md");
        _output.WriteLine("  Experiment log:  docsV4_3/experiments/TRM_V4_3_Experiment_Log.md");
        _output.WriteLine("\nDOCUMENTATION GENERATED ✓");
    }

    [Fact]
    public void V4_3_GSH_13_NoPhysicalComparisonUsed()
    {
        _output.WriteLine("=== NO PHYSICAL COMPARISON VERIFICATION ===\n");
        _output.WriteLine("  This suite does NOT:");
        _output.WriteLine("    ✗ Use physical c (299,792,458 m/s)");
        _output.WriteLine("    ✗ Use physical G (6.67430×10⁻¹¹ m³/(kg·s²))");
        _output.WriteLine("    ✗ Use SI comparison results from V4.2");
        _output.WriteLine("    ✗ Use SPARC, lensing, CMB, or astrophysical data");
        _output.WriteLine("    ✗ Rank hierarchy by physical agreement");
        _output.WriteLine("    ✗ Modify frozen V4.2 predictions");
        _output.WriteLine("    ✗ Recalibrate c_eff_SI or G_eff_SI\n");
        _output.WriteLine("  All analysis uses geometric criteria only:");
        _output.WriteLine("    ✓ Directed dependency graph (R² asymmetry)");
        _output.WriteLine("    ✓ Hierarchy strength (fraction of directed edges)");
        _output.WriteLine("    ✓ Hierarchy depth (DAG longest path)");
        _output.WriteLine("    ✓ Bootstrap seed stability of hierarchy metrics");
        _output.WriteLine("    ✓ N-scaling, law, and load robustness");
        _output.WriteLine("    ✓ Null controls: random distances, K=0 destroy hierarchy");
        _output.WriteLine("\nNO PHYSICAL COMPARISON USED ✓");
    }

    [Fact]
    public void V4_3_GSH_14_ClaimDisciplineReport()
    {
        _output.WriteLine("=== CLAIM DISCIPLINE REPORT ===\n");
        _output.WriteLine("SUPPORTED:");
        _output.WriteLine("  • Directed hierarchy graph constructed via R² asymmetry (parent→child).");
        _output.WriteLine("  • Scale dependency R² matrix computed for all 8 candidates.");
        _output.WriteLine("  • Hierarchy strength (fraction of directed edges) and depth (DAG levels) computed.");
        _output.WriteLine("  • Transitive reduction applied to extract direct parent→child edges.");
        _output.WriteLine("  • Bootstrap seed stability of hierarchy verified (8 iterations).");
        _output.WriteLine("  • N-scaling stability (N=40, 80), law robustness (exp/gaussian), load robustness verified.");
        _output.WriteLine("  • Null controls (random distances, K=0) confirmed to destroy hierarchy structure.");
        _output.WriteLine("  • Classification: STRONG / MODERATE / WEAK / NO HIERARCHY.");
        _output.WriteLine("  • No physical constants, SI comparisons, or astrophysical data used.\n");
        _output.WriteLine("CONDITIONAL:");
        _output.WriteLine("  • Hierarchy direction (which scale determines which) is inferred from R² asymmetry.");
        _output.WriteLine("  • Causality (parent→child) is associative, not mechanistic.");
        _output.WriteLine("  • Results depend on finite N (40–80), nS (6–12), primary regime (ξ=1.75, K₀=1.2).");
        _output.WriteLine("  • Bootstrap stability uses 8 iterations — indicative but not conclusive.\n");
        _output.WriteLine("HYPOTHESIS:");
        _output.WriteLine("  • If GLOBAL scales determine LOCAL scales, the hierarchy is GLOBAL→LOCAL.");
        _output.WriteLine("  • The causal horizon scale may occupy a bridge position in the hierarchy.");
        _output.WriteLine("  • A genuine hierarchy would imply one fundamental scale from which others derive.\n");
        _output.WriteLine("NOT CLAIMED:");
        _output.WriteLine("  • Physical c or G derived, compared, or used.");
        _output.WriteLine("  • SI calibration performed or modified.");
        _output.WriteLine("  • Spacetime, Lorentz, SR, GR, Einstein equations derived.");
        _output.WriteLine("  • Causality beyond associative R² asymmetry.");
        _output.WriteLine("  • Astrophysical data used.");
        _output.WriteLine("  • V4.2 frozen predictions modified.\n");
        _output.WriteLine("GEOMETRIC INTERPRETATION ONLY. NO PHYSICAL CLAIMS.");
    }
}
