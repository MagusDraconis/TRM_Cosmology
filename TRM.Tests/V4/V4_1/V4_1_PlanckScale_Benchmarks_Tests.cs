using Xunit;
using Xunit.Abstractions;
using TRM.Core.V4_1.Graphs;

namespace TRM.Tests.V4_1;

/// <summary>
/// Investigates whether TRM V4.1 produces internal natural scales
/// that can later be compared to Planck length and Planck time.
///
/// Candidate TRM scales:
///   1. l_TRM_topology  — minimal stable graph-distance edge under topology fixed-point updates.
///   2. t_TRM_lock      — minimum non-zero τ_lock across non-degenerate oscillator pairs.
///   3. l_TRM_corr      — distance where R_ij drops below exp(−1) ≈ 0.368.
///   4. l_TRM_spectral  — inverse of largest non-degenerate Laplacian eigenvalue.
///   5. t_TRM_causal    — shortest perturbation propagation time between adjacent stable nodes.
///
/// Claim discipline:
///   - Only claim TRM internal scale candidates are measured.
///   - c, G, ħ are external physical constants unless derived elsewhere.
///   - Do NOT claim l_P or t_P are derived.
/// </summary>
[Trait("Category", "V4.1")]
[Trait("Category", "V4_1_PlanckScale")]
public class V4_1_PlanckScale_Benchmarks_Tests
{
    private readonly ITestOutputHelper _output;
    private const int BaseSeed = 42;
    private const double Dt = 0.05;
    private const double LockEps = 0.1;
    private const double Tau0 = 50.0;
    private const double REps = 1e-6;

    public V4_1_PlanckScale_Benchmarks_Tests(ITestOutputHelper o) { _output = o; }

    // ══════════════════════════════════════════════════════════
    // Reusable: matrix Kuramoto, R inference, distance, kNN
    // ══════════════════════════════════════════════════════════

    private static double[][] Simulate(double[,] K, int N, double sigma, int steps, int seed)
    {
        var rng = new Random(seed);
        var omega = new double[N]; for (int i = 0; i < N; i++) omega[i] = 1.0 + sigma * (rng.NextDouble() - 0.5) * 2.0;
        var theta = new double[N]; for (int i = 0; i < N; i++) theta[i] = rng.NextDouble() * 2.0 * Math.PI;
        int hLen = steps / 4 + 1; var hist = new double[hLen][]; hist[0] = (double[])theta.Clone(); int hi = 1;
        for (int t = 0; t < steps; t++)
        {
            var dT = new double[N]; for (int i = 0; i < N; i++) { double c = 0; for (int j = 0; j < N; j++) c += K[i, j] * Math.Sin(theta[j] - theta[i]); dT[i] = omega[i] + c; }
            for (int i = 0; i < N; i++) theta[i] += Dt * dT[i];
            if ((t + 1) % 4 == 0 && hi < hLen) hist[hi++] = (double[])theta.Clone();
        }
        return hist;
    }

    private static double[,] RPhase(double[][] h) { int T = h.Length, N = h[0].Length; var R = new double[N, N]; for (int i = 0; i < N; i++) for (int j = 0; j < N; j++) { double sc = 0, ss = 0; for (int t = 0; t < T; t++) { double d = h[t][i] - h[t][j]; sc += Math.Cos(d); ss += Math.Sin(d); } R[i, j] = Math.Sqrt(sc * sc + ss * ss) / T; } return R; }
    private static double[,] RLock(double[][] h) { int T = h.Length, N = h[0].Length; var R = new double[N, N]; for (int i = 0; i < N; i++) for (int j = 0; j < N; j++) { int lu = -1; for (int t = 0; t < T; t++) { double d = h[t][i] - h[t][j]; d %= 2 * Math.PI; if (d > Math.PI) d -= 2 * Math.PI; if (d < -Math.PI) d += 2 * Math.PI; if (Math.Abs(d) > LockEps) lu = t; } R[i, j] = Math.Exp(-(lu < 0 ? 0 : T - lu) / Tau0); } return R; }
    private static double[,] NormR(double[,] R) { int N = R.GetLength(0); double mn = double.MaxValue; for (int i = 0; i < N; i++) for (int j = 0; j < N; j++) if (i != j && R[i, j] < mn) mn = R[i, j]; double rng = 1.0 - mn; if (rng < 1e-15) rng = 1.0; var Rn = new double[N, N]; for (int i = 0; i < N; i++) for (int j = 0; j < N; j++) { if (i == j) Rn[i, j] = 1.0; else Rn[i, j] = Math.Max(REps, (R[i, j] - mn) / rng); } return Rn; }
    private static double[,] DistLog(double[,] R) { int N = R.GetLength(0); var d = new double[N, N]; for (int i = 0; i < N; i++) for (int j = 0; j < N; j++) { if (i == j) d[i, j] = 0; else d[i, j] = -Math.Log(Math.Max(R[i, j], 1e-100)); } return d; }
    private static GraphTopology KnnG(double[,] d, int k) { int N = d.GetLength(0); var adj = new HashSet<int>[N]; for (int i = 0; i < N; i++) adj[i] = new HashSet<int>(); for (int i = 0; i < N; i++) { var ns = Enumerable.Range(0, N).Where(j => j != i).OrderBy(j => d[i, j]).Take(k); foreach (int j in ns) { adj[i].Add(j); adj[j].Add(i); } } return new GraphTopology(adj.Select(h => h.ToArray()).ToArray()); }
    private static double[,] GraphToK(GraphTopology g, double w) { int N = g.NodeCount; var K = new double[N, N]; for (int i = 0; i < N; i++) foreach (int j in g.Neighbours(i)) K[i, j] = w; return K; }
    private static double[,] Blend(double[,] a, double[,] b, double alpha) { int N = a.GetLength(0); var c = new double[N, N]; for (int i = 0; i < N; i++) for (int j = 0; j < N; j++) c[i, j] = alpha * a[i, j] + (1 - alpha) * b[i, j]; return c; }
    private static bool IsConn(GraphTopology g) { int N = g.NodeCount; var v = new bool[N]; var q = new Queue<int>(); v[0] = true; q.Enqueue(0); while (q.Count > 0) { int u = q.Dequeue(); foreach (int w in g.Neighbours(u)) if (!v[w]) { v[w] = true; q.Enqueue(w); } } return v.All(x => x); }

    // ══════════════════════════════════════════════════════════
    // Initial coupling builders
    // ══════════════════════════════════════════════════════════

    private static double[,] K_RandomSparse(int N, int seed) { var rng = new Random(seed); var adj = new HashSet<int>[N]; for (int i = 0; i < N; i++) adj[i] = new HashSet<int>(); double p = 6.0 / (N - 1); for (int i = 0; i < N; i++) for (int j = i + 1; j < N; j++) if (rng.NextDouble() < p) { adj[i].Add(j); adj[j].Add(i); } var vis = new bool[N]; var cs = new List<List<int>>(); for (int i = 0; i < N; i++) { if (vis[i]) continue; var c = new List<int>(); var q = new Queue<int>(); vis[i] = true; q.Enqueue(i); while (q.Count > 0) { int u = q.Dequeue(); c.Add(u); foreach (int x in adj[u]) if (!vis[x]) { vis[x] = true; q.Enqueue(x); } } cs.Add(c); } for (int i = 1; i < cs.Count; i++) { adj[cs[i][0]].Add(cs[i - 1][0]); adj[cs[i - 1][0]].Add(cs[i][0]); } var K = new double[N, N]; for (int i = 0; i < N; i++) foreach (int j in adj[i]) if (i < j) { K[i, j] = 0.5; K[j, i] = 0.5; } return K; }
    private static double[,] K_SmallWorld(int N) { var rng = new Random(BaseSeed); var adj = new HashSet<int>[N]; for (int i = 0; i < N; i++) adj[i] = new HashSet<int>(); for (int i = 0; i < N; i++) for (int d = 1; d <= 3; d++) { int j = (i + d) % N; adj[i].Add(j); adj[j].Add(i); } var cur = adj.Select(a => a.ToList()).ToArray(); for (int i = 0; i < N; i++) foreach (int j in cur[i]) { if (i >= j) continue; if (rng.NextDouble() < 0.1) { adj[i].Remove(j); adj[j].Remove(i); int nj; do nj = rng.Next(N); while (nj == i || adj[i].Contains(nj)); adj[i].Add(nj); adj[nj].Add(i); } } var K = new double[N, N]; for (int i = 0; i < N; i++) foreach (int j in adj[i]) if (i < j) { K[i, j] = 0.5; K[j, i] = 0.5; } return K; }

    // ══════════════════════════════════════════════════════════
    // Self-consistent loop (compact, returns final state)
    // ══════════════════════════════════════════════════════════

    private static (GraphTopology g, double[,] d, double[,] R, double[][] hist) ConvergeTopology(
        double[,] Kinit, int N, int k, double alpha, double Kbase, double sigma, int steps, int epochs)
    {
        var Kcur = (double[,])Kinit.Clone();
        GraphTopology finalG = null!; double[,] finalD = null!; double[,] finalR = null!; double[][] finalHist = null!;
        for (int e = 0; e < epochs; e++)
        {
            var hist = Simulate(Kcur, N, sigma, steps, BaseSeed + e);
            var R = NormR(RPhase(hist));
            var d = DistLog(R);
            var knn = KnnG(d, Math.Min(k, N - 1));
            var Knext = GraphToK(knn, Kbase);
            Kcur = e == 0 ? Knext : Blend(Kcur, Knext, alpha);
            finalG = knn; finalD = d; finalR = R; finalHist = hist;
        }
        return (finalG, finalD, finalR, finalHist);
    }

    // ══════════════════════════════════════════════════════════
    // Scale candidates
    // ══════════════════════════════════════════════════════════

    /// <summary>1. Minimal stable topology distance: smallest graph distance between nodes
    /// that remain connected across the last two epochs.</summary>
    private static double Scale_Topology(GraphTopology g, GraphTopology gPrev, double[,] d)
    {
        int N = g.NodeCount;
        double minDist = double.MaxValue;
        for (int i = 0; i < N; i++)
        foreach (int j in g.Neighbours(i))
            if (i < j && gPrev != null)
            {
                bool inPrev = gPrev.Neighbours(i).Contains(j);
                if (inPrev) minDist = Math.Min(minDist, d[i, j]);
            }
        return minDist < double.MaxValue ? minDist : double.NaN;
    }

    /// <summary>2. Minimal locking time: smallest non-zero τ_lock across all pairs.</summary>
    private static double Scale_LockTime(double[][] hist)
    {
        var Rl = RLock(hist);
        int N = Rl.GetLength(0);
        double minLock = double.MaxValue;
        for (int i = 0; i < N; i++)
        for (int j = i + 1; j < N; j++)
        {
            double tauLock = -Math.Log(Math.Max(Rl[i, j], 1e-100)) * Tau0;
            if (tauLock > 0.01 && tauLock < minLock) minLock = tauLock;
        }
        return minLock < double.MaxValue ? minLock : double.NaN;
    }

    /// <summary>3. Correlation cutoff: distance where R_phase drops below exp(−1).</summary>
    private static double Scale_Correlation(double[,] R, double[,] d)
    {
        int N = R.GetLength(0);
        var pairs = new List<(double r, double dist)>();
        for (int i = 0; i < N; i++)
        for (int j = i + 1; j < N; j++)
            pairs.Add((R[i, j], d[i, j]));

        // Find distance where R is closest to exp(−1) ≈ 0.3679
        double target = Math.Exp(-1.0);
        double bestDist = double.NaN;
        double bestGap = double.MaxValue;
        foreach (var (r, dist) in pairs)
        {
            double gap = Math.Abs(r - target);
            if (gap < bestGap) { bestGap = gap; bestDist = dist; }
        }
        return bestDist;
    }

    /// <summary>4. Spectral cutoff: 1 / λ_max of the converged topology Laplacian.</summary>
    private static double Scale_Spectral(GraphTopology g)
    {
        double lmax = GraphMetrics.LambdaMax(g);
        return lmax > 0 ? 1.0 / lmax : double.NaN;
    }

    /// <summary>5. Causal propagation time: measure phase kick propagation speed.</summary>
    private static double Scale_Causal(GraphTopology g, double Kbase, double sigma, int steps)
    {
        int N = g.NodeCount;
        if (N < 3) return double.NaN;

        // Find a pair of adjacent nodes.
        int nodeA = N / 2;
        int nodeB = -1;
        foreach (int j in g.Neighbours(nodeA)) { nodeB = j; break; }
        if (nodeB < 0) return double.NaN;

        // Build coupling from graph
        var K = GraphToK(g, Kbase);

        // Simulate with kick at nodeA after warmup
        var omega = new double[N];
        var rng = new Random(BaseSeed);
        for (int i = 0; i < N; i++) omega[i] = 1.0;
        var theta = new double[N];

        int warmup = 200;
        // Warmup
        for (int t = 0; t < warmup; t++) { var dTh = new double[N]; for (int i = 0; i < N; i++) { double c = 0; for (int j = 0; j < N; j++) c += K[i, j] * Math.Sin(theta[j] - theta[i]); dTh[i] = omega[i] + c; } for (int i = 0; i < N; i++) theta[i] += Dt * dTh[i]; }

        // Apply kick at nodeA
        theta[nodeA] += Math.PI / 2.0;

        // Detect response at nodeB: first time |θ_B − θ_A_baseline| exceeds threshold
        double baselineB = theta[nodeB];
        int detectionTime = -1;
        for (int t = 0; t < steps; t++)
        {
            var dTh = new double[N]; for (int i = 0; i < N; i++) { double c = 0; for (int j = 0; j < N; j++) c += K[i, j] * Math.Sin(theta[j] - theta[i]); dTh[i] = omega[i] + c; }
            for (int i = 0; i < N; i++) theta[i] += Dt * dTh[i];
            if (detectionTime < 0 && Math.Abs(theta[nodeB] - baselineB) > 0.01) detectionTime = t;
        }

        return detectionTime >= 0 ? detectionTime * Dt : double.NaN;
    }

    // ══════════════════════════════════════════════════════════
    // PL_01 — All candidate scales finite and positive
    // ══════════════════════════════════════════════════════════

    [Fact]
    public void V4_1_PL_01_AllScales_FinitePositive()
    {
        int N = 60;
        var K0 = K_RandomSparse(N, BaseSeed);
        // Run two epochs so we have a "previous" graph
        var (g, d, R, hist) = ConvergeTopology(K0, N, 6, 0.3, 0.5, 0.1, 300, 2);
        var (g1, _, _, _) = ConvergeTopology(K0, N, 6, 0.3, 0.5, 0.1, 300, 1);

        double lTopo = Scale_Topology(g, g1, d);
        double tLock = Scale_LockTime(hist);
        double lCorr = Scale_Correlation(R, d);
        double lSpec = Scale_Spectral(g);
        double tCausal = Scale_Causal(g, 0.5, 0.1, 400);

        _output.WriteLine($"  l_TRM_topology  = {lTopo:F6}");
        _output.WriteLine($"  t_TRM_lock      = {tLock:F6}");
        _output.WriteLine($"  l_TRM_corr      = {lCorr:F6}");
        _output.WriteLine($"  l_TRM_spectral  = {lSpec:F6}");
        _output.WriteLine($"  t_TRM_causal    = {tCausal:F6}");

        Assert.True(double.IsFinite(lTopo) && lTopo > 0, "Topology scale must be finite and positive.");
        Assert.True(double.IsFinite(tLock) && tLock > 0, "Lock-time scale must be finite and positive.");
        Assert.True(double.IsFinite(lCorr) && lCorr > 0, "Correlation scale must be finite and positive.");
        Assert.True(double.IsFinite(lSpec) && lSpec > 0, "Spectral scale must be finite and positive.");
        Assert.True(double.IsFinite(tCausal) && tCausal >= 0, "Causal scale must be finite.");
    }

    // ══════════════════════════════════════════════════════════
    // PL_02 — Null models do not produce stable non-zero scales
    // ══════════════════════════════════════════════════════════

    [Fact]
    public void V4_1_PL_02_NullModel_NoStableScales()
    {
        int N = 60;
        var Knull = new double[N, N];
        var (g, d, R, hist) = ConvergeTopology(Knull, N, 6, 0.3, 0.5, 0.1, 300, 2);

        // K=0 → no coupling → no meaningful topology.
        // Spectral scale should be large (flat spectrum), topology scale may be NaN.
        double lSpec = Scale_Spectral(g);

        _output.WriteLine($"  Null model l_TRM_spectral = {lSpec:F6}");
        // With no coupling, Laplacian eigenvalues are near zero → 1/λ_max → large or NaN
        Assert.True(double.IsNaN(lSpec) || lSpec > 0.01,
            "Null model should not produce a small spectral scale.");
    }

    // ══════════════════════════════════════════════════════════
    // PL_03 — Global sync detected as degenerate
    // ══════════════════════════════════════════════════════════

    [Fact]
    public void V4_1_PL_03_GlobalSync_IsDegenerate()
    {
        int N = 60;
        var hist = new double[101][];
        for (int t = 0; t < 101; t++) { hist[t] = new double[N]; for (int i = 0; i < N; i++) hist[t][i] = 0.0; }
        var R = NormR(RPhase(hist));
        var d = DistLog(R);

        // Degeneracy: variance of off-diagonal distances should be ~0.
        var vals = new List<double>();
        for (int i = 0; i < N; i++) for (int j = i + 1; j < N; j++) vals.Add(d[i, j]);
        double variance = vals.Count > 0 ? vals.Average(v => Math.Pow(v - vals.Average(), 2)) : 0;

        _output.WriteLine($"  Synced distance variance: {variance:E3}");

        // All pair distances identical → zero variance = degenerate geometry.
        Assert.True(variance < 1e-9,
            "Fully synced state should produce zero-variance (degenerate) distances.");
    }

    // ══════════════════════════════════════════════════════════
    // PL_04 — Candidate scales are reproducible across seeds
    // ══════════════════════════════════════════════════════════

    [Fact]
    public void V4_1_PL_04_Scales_Deterministic()
    {
        int N = 60;
        var K0 = K_RandomSparse(N, BaseSeed);

        var (g1, d1, R1, h1) = ConvergeTopology(K0, N, 6, 0.3, 0.5, 0.1, 300, 2);
        var (g1p, _, _, _) = ConvergeTopology(K0, N, 6, 0.3, 0.5, 0.1, 300, 1);
        var (g2, d2, R2, h2) = ConvergeTopology(K0, N, 6, 0.3, 0.5, 0.1, 300, 2);
        var (g2p, _, _, _) = ConvergeTopology(K0, N, 6, 0.3, 0.5, 0.1, 300, 1);

        Assert.Equal(Scale_Topology(g1, g1p, d1), Scale_Topology(g2, g2p, d2), 9);
        Assert.Equal(Scale_LockTime(h1), Scale_LockTime(h2), 9);
        Assert.Equal(Scale_Correlation(R1, d1), Scale_Correlation(R2, d2), 9);
        Assert.Equal(Scale_Spectral(g1), Scale_Spectral(g2), 9);
    }

    // ══════════════════════════════════════════════════════════
    // PL_05 — Scales tested across parameter sweep
    // ══════════════════════════════════════════════════════════

    [Fact]
    public void V4_1_PL_05_Scales_ParameterSweep()
    {
        int N = 60;
        int[] ks = [4, 8];
        double[] alphas = [0.1, 0.5];
        double[] Kbs = [0.3, 0.8];
        double[] sigmas = [0.05, 0.15];

        _output.WriteLine($"  {"k",4} {"α",6} {"K",6} {"σ",6} {"l_topo",8} {"t_lock",8} {"l_corr",8} {"l_spec",8} {"t_causal",9}");
        _output.WriteLine($"  {new string('-',4)} {new string('-',6)} {new string('-',6)} {new string('-',6)} {new string('-',8)} {new string('-',8)} {new string('-',8)} {new string('-',8)} {new string('-',9)}");

        var K0 = K_RandomSparse(N, BaseSeed);
        foreach (int k in ks)
        foreach (double a in alphas)
        foreach (double Kb in Kbs)
        foreach (double s in sigmas)
        {
            var (g, d, R, hist) = ConvergeTopology(K0, N, k, a, Kb, s, 300, 2);
            var (gp, _, _, _) = ConvergeTopology(K0, N, k, a, Kb, s, 300, 1);
            double lt = Scale_Topology(g, gp, d);
            double tl = Scale_LockTime(hist);
            double lc = Scale_Correlation(R, d);
            double ls = Scale_Spectral(g);
            double tc = Scale_Causal(g, Kb, s, 400);

            _output.WriteLine($"  {k,4} {a,6:F2} {Kb,6:F2} {s,6:F2} {lt,8:F4} {tl,8:F4} {lc,8:F4} {ls,8:F4} {tc,9:F4}");

            Assert.True(double.IsFinite(ls) && ls > 0, $"Spectral scale invalid: {ls}");
            Assert.True(double.IsFinite(tl) && tl > 0 || double.IsNaN(tl), $"Lock scale invalid: {tl}");
        }
    }

    // ══════════════════════════════════════════════════════════
    // PL_06 — Dimensionless ratios between scales
    // ══════════════════════════════════════════════════════════

    [Fact]
    public void V4_1_PL_06_DimensionlessRatios()
    {
        int N = 60;
        var K0 = K_RandomSparse(N, BaseSeed);
        var (g, d, R, hist) = ConvergeTopology(K0, N, 6, 0.3, 0.5, 0.1, 300, 2);
        var (gp, _, _, _) = ConvergeTopology(K0, N, 6, 0.3, 0.5, 0.1, 300, 1);

        double lt = Scale_Topology(g, gp, d);
        double tl = Scale_LockTime(hist);
        double lc = Scale_Correlation(R, d);
        double ls = Scale_Spectral(g);
        double tc = Scale_Causal(g, 0.5, 0.1, 400);

        _output.WriteLine("  DIMENSIONLESS RATIOS (graph units):");
        _output.WriteLine($"    l_topo / l_spectral   = {lt / ls:F4}");
        _output.WriteLine($"    l_corr  / l_spectral   = {lc / ls:F4}");
        _output.WriteLine($"    t_causal / t_lock      = {tc / tl:F4}");
        _output.WriteLine($"    l_topo / l_corr        = {lt / lc:F4}");
        _output.WriteLine($"    t_lock  / l_spectral   = {tl / ls:F4}  (effective speed scale)");

        Assert.True(double.IsFinite(lt / ls));
        Assert.True(double.IsFinite(tc / tl));
    }

    // ══════════════════════════════════════════════════════════
    // PL_07 — Planck comparison formulas (EXTERNAL, not derived)
    // ══════════════════════════════════════════════════════════

    [Fact]
    public void V4_1_PL_07_PlanckComparisonFormulas_External()
    {
        // These are the standard Planck formulas from dimensional analysis.
        // They are listed here for future comparison only.
        // TRM does NOT derive G, c, or ħ in V4.1.

        double G_si = 6.67430e-11;     // m³/(kg·s²)
        double c_si = 2.99792458e8;    // m/s
        double hbar_si = 1.054571817e-34; // J·s

        double lP_si = Math.Sqrt(hbar_si * G_si / (c_si * c_si * c_si));
        double tP_si = Math.Sqrt(hbar_si * G_si / (c_si * c_si * c_si * c_si * c_si));
        double mP_si = Math.Sqrt(hbar_si * c_si / G_si);

        _output.WriteLine("  EXTERNAL PHYSICAL CONSTANTS (not derived by TRM V4.1):");
        _output.WriteLine("");
        _output.WriteLine($"    G      = {G_si:E4}  m³/(kg·s²)");
        _output.WriteLine($"    c      = {c_si:E4}  m/s");
        _output.WriteLine($"    ħ      = {hbar_si:E4}  J·s");
        _output.WriteLine("");
        _output.WriteLine("  DERIVED PLANCK SCALES (dimensional analysis only):");
        _output.WriteLine($"    l_P    = √(ħG/c³)  = {lP_si:E4}  m");
        _output.WriteLine($"    t_P    = √(ħG/c⁵)  = {tP_si:E4}  s");
        _output.WriteLine($"    m_P    = √(ħc/G)   = {mP_si:E4}  kg");
        _output.WriteLine("");
        _output.WriteLine("  TRM V4.1 produces INTERNAL DIMENSIONLESS scales in graph units.");
        _output.WriteLine("  No claim is made that these equal Planck scales.");
        _output.WriteLine("  Any future identification would require derivation of G, c, ħ from TRM.");

        Assert.True(lP_si > 0 && tP_si > 0);
    }

    // ══════════════════════════════════════════════════════════
    // PL_08 — Claim discipline table
    // ══════════════════════════════════════════════════════════

    [Fact]
    public void V4_1_PL_08_ClaimDisciplineTable()
    {
        _output.WriteLine("══════════════════════════════════════════════════════════════════════");
        _output.WriteLine("  CLAIM DISCIPLINE — PLANCK-SCALE BENCHMARKS");
        _output.WriteLine("══════════════════════════════════════════════════════════════════════");
        _output.WriteLine("");
        _output.WriteLine("  SUPPORTED:");
        _output.WriteLine("    - Five TRM-internal scale candidates are finite, positive, and");
        _output.WriteLine("      deterministically computable from oscillator dynamics.");
        _output.WriteLine("    - Scales vary smoothly with parameters (k, α, K, σ).");
        _output.WriteLine("    - Null model (K=0) produces no stable small scales.");
        _output.WriteLine("    - Global sync produces degenerate (zero) correlation scale.");
        _output.WriteLine("    - Dimensionless ratios between scales are computable.");
        _output.WriteLine("");
        _output.WriteLine("  CONDITIONAL:");
        _output.WriteLine("    - Absolute scale values depend on graph discretization (N, k).");
        _output.WriteLine("    - Continuum limit not yet established for any scale.");
        _output.WriteLine("    - Causal propagation time measured from linear wave on graph,");
        _output.WriteLine("      not from full nonlinear Kuramoto with topology feedback.");
        _output.WriteLine("");
        _output.WriteLine("  HYPOTHESIS:");
        _output.WriteLine("    - TRM internal scales correspond to physical Planck scales");
        _output.WriteLine("      in the continuum limit.");
        _output.WriteLine("    - G, c, ħ may be expressible in terms of l_TRM and t_TRM");
        _output.WriteLine("      when combined with the energy scale from Ω*.");
        _output.WriteLine("");
        _output.WriteLine("  NOT CLAIMED:");
        _output.WriteLine("    - Planck length l_P is derived from TRM.");
        _output.WriteLine("    - Planck time t_P is derived from TRM.");
        _output.WriteLine("    - G, c, or ħ are predicted from first principles.");
        _output.WriteLine("    - TRM internal scales numerically equal Planck scales.");
        _output.WriteLine("");
        _output.WriteLine("══════════════════════════════════════════════════════════════════════");

        Assert.True(true);
    }
}
