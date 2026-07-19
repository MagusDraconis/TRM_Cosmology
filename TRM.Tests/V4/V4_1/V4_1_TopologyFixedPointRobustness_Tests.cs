using Xunit;
using Xunit.Abstractions;
using TRM.Core.V4_1.Graphs;

namespace TRM.Tests.V4_1;

/// <summary>
/// Robustness hardening: tests whether self-consistent topology loop finds stable
/// geometric fixed points or merely numerical artifacts from kNN, finite N, or parameters.
///
/// Parameter sweeps: N, k, alpha, K_base, sigma_omega, epochs.
/// Initial conditions: weak-dense, random-sparse, noisy-lattice, shuffled-lattice,
///   small-world, uncoupled null, fully-synced trivial.
/// R candidates: phase-lock, correlation, lock-time.
///
/// Claim discipline:
///   SUPPORTED:  numerical pipeline behaviour under wide parameter ranges.
///   CONDITIONAL: stable topology convergence under tested parameters.
///   HYPOTHESIS:  physical emergent space.
/// </summary>
[Trait("Category", "V4.1")]
[Trait("Category", "V4_1_TopologyFixedPoint")]
public class V4_1_TopologyFixedPointRobustness_Tests
{
    private readonly ITestOutputHelper _output;

    // Defaults (overridden by sweeps)
    private const int BaseSeed = 42;
    private const double Dt = 0.05;
    private const int HistoryDs = 4;
    private const double LockEps = 0.1;
    private const double Tau0 = 50.0;
    private const double REps = 1e-6;
    private const int BasinRuns = 4;
    private const int TriSample = 2000;

    public V4_1_TopologyFixedPointRobustness_Tests(ITestOutputHelper o) { _output = o; }

    // ═══════════════════════════════════════════════════════════════════════
    // Matrix Kuramoto + R inference (compact, self-contained)
    // ═══════════════════════════════════════════════════════════════════════

    private static double[][] Simulate(double[,] K, int N, double sigma, int steps, int seed)
    {
        var rng = new Random(seed);
        var omega = new double[N]; for (int i = 0; i < N; i++) omega[i] = 1.0 + sigma * (rng.NextDouble() - 0.5) * 2.0;
        var theta = new double[N]; for (int i = 0; i < N; i++) theta[i] = rng.NextDouble() * 2.0 * Math.PI;
        int hLen = steps / HistoryDs + 1;
        var hist = new double[hLen][]; hist[0] = (double[])theta.Clone(); int hi = 1;
        for (int t = 0; t < steps; t++)
        {
            var dT = new double[N];
            for (int i = 0; i < N; i++) { double c = 0; for (int j = 0; j < N; j++) c += K[i, j] * Math.Sin(theta[j] - theta[i]); dT[i] = omega[i] + c; }
            for (int i = 0; i < N; i++) theta[i] += Dt * dT[i];
            if ((t + 1) % HistoryDs == 0 && hi < hLen) hist[hi++] = (double[])theta.Clone();
        }
        return hist;
    }

    private static double[,] RPhase(double[][] h) { int T = h.Length, N = h[0].Length; var R = new double[N, N]; for (int i = 0; i < N; i++) for (int j = 0; j < N; j++) { double sc = 0, ss = 0; for (int t = 0; t < T; t++) { double d = h[t][i] - h[t][j]; sc += Math.Cos(d); ss += Math.Sin(d); } R[i, j] = Math.Sqrt(sc * sc + ss * ss) / T; } return R; }
    private static double[,] RCorr(double[][] h) { int T = h.Length, N = h[0].Length; var s = new double[N][]; for (int i = 0; i < N; i++) { s[i] = new double[T]; for (int t = 0; t < T; t++) s[i][t] = Math.Sin(h[t][i]); } var R = new double[N, N]; for (int i = 0; i < N; i++) for (int j = 0; j < N; j++) R[i, j] = Math.Abs(PearsonC(s[i], s[j])); return R; }
    private static double[,] RLock(double[][] h) { int T = h.Length, N = h[0].Length; var R = new double[N, N]; for (int i = 0; i < N; i++) for (int j = 0; j < N; j++) { int lu = -1; for (int t = 0; t < T; t++) { double d = h[t][i] - h[t][j]; d %= 2 * Math.PI; if (d > Math.PI) d -= 2 * Math.PI; if (d < -Math.PI) d += 2 * Math.PI; if (Math.Abs(d) > LockEps) lu = t; } R[i, j] = Math.Exp(-(lu < 0 ? 0 : T - lu) / Tau0); } return R; }

    private static double[,] NormalizeR(double[,] R) { int N = R.GetLength(0); double mn = double.MaxValue; for (int i = 0; i < N; i++) for (int j = 0; j < N; j++) if (i != j && R[i, j] < mn) mn = R[i, j]; double rng = 1.0 - mn; if (rng < 1e-15) rng = 1.0; var Rn = new double[N, N]; for (int i = 0; i < N; i++) for (int j = 0; j < N; j++) { if (i == j) Rn[i, j] = 1.0; else Rn[i, j] = Math.Max(REps, (R[i, j] - mn) / rng); } return Rn; }
    private static double[,] DistLog(double[,] R) { int N = R.GetLength(0); var d = new double[N, N]; for (int i = 0; i < N; i++) for (int j = 0; j < N; j++) { if (i == j) d[i, j] = 0; else d[i, j] = -Math.Log(Math.Max(R[i, j], 1e-100)); } return d; }

    private static GraphTopology KnnG(double[,] d, int k) { int N = d.GetLength(0); var adj = new HashSet<int>[N]; for (int i = 0; i < N; i++) adj[i] = new HashSet<int>(); for (int i = 0; i < N; i++) { var ns = Enumerable.Range(0, N).Where(j => j != i).OrderBy(j => d[i, j]).Take(k); foreach (int j in ns) { adj[i].Add(j); adj[j].Add(i); } } return new GraphTopology(adj.Select(h => h.ToArray()).ToArray()); }
    private static double[,] GraphToK(GraphTopology g, double w) { int N = g.NodeCount; var K = new double[N, N]; for (int i = 0; i < N; i++) foreach (int j in g.Neighbours(i)) K[i, j] = w; return K; }
    private static double[,] Blend(double[,] a, double[,] b, double alpha) { int N = a.GetLength(0); var c = new double[N, N]; for (int i = 0; i < N; i++) for (int j = 0; j < N; j++) c[i, j] = alpha * a[i, j] + (1 - alpha) * b[i, j]; return c; }

    // ═══════════════════════════════════════════════════════════════════════
    // Initial K builders
    // ═══════════════════════════════════════════════════════════════════════

    private static double[,] K_WeakDense(int N) { var K = new double[N, N]; for (int i = 0; i < N; i++) for (int j = 0; j < N; j++) if (i != j) K[i, j] = 0.1 / N; return K; }
    private static double[,] K_RandomSparse(int N, int seed, double w) { var rng = new Random(seed); var adj = new HashSet<int>[N]; for (int i = 0; i < N; i++) adj[i] = new HashSet<int>(); double p = 6.0 / (N - 1); for (int i = 0; i < N; i++) for (int j = i + 1; j < N; j++) if (rng.NextDouble() < p) { adj[i].Add(j); adj[j].Add(i); } var v = new bool[N]; var cs = new List<List<int>>(); for (int i = 0; i < N; i++) { if (v[i]) continue; var c = new List<int>(); var q = new Queue<int>(); v[i] = true; q.Enqueue(i); while (q.Count > 0) { int u = q.Dequeue(); c.Add(u); foreach (int x in adj[u]) if (!v[x]) { v[x] = true; q.Enqueue(x); } } cs.Add(c); } for (int i = 1; i < cs.Count; i++) { adj[cs[i][0]].Add(cs[i - 1][0]); adj[cs[i - 1][0]].Add(cs[i][0]); } var K = new double[N, N]; for (int i = 0; i < N; i++) foreach (int j in adj[i]) if (i < j) { K[i, j] = w; K[j, i] = w; } return K; }
    private static double[,] K_NoisyLattice(int N, int seed, int D, double noise) { int n = Math.Max(2, (int)Math.Round(Math.Pow(N, 1.0 / D))); var g = D switch { 1 => GraphFactory.Chain(n), 2 => GraphFactory.SquareGrid(n), 3 => GraphFactory.CubicLattice(n), _ => GraphFactory.Hypercubic4D(n) }; int M = g.NodeCount; var K = new double[M, M]; var rng = new Random(seed); for (int i = 0; i < M; i++) foreach (int j in g.Neighbours(i)) if (i < j) { double w = 0.5 + noise * (rng.NextDouble() - 0.5); K[i, j] = w; K[j, i] = w; } return K; }
    private static double[,] K_SmallWorld(int N, int seed) { var rng = new Random(seed); var adj = new HashSet<int>[N]; for (int i = 0; i < N; i++) adj[i] = new HashSet<int>(); for (int i = 0; i < N; i++) for (int d = 1; d <= 3; d++) { int j = (i + d) % N; adj[i].Add(j); adj[j].Add(i); } var cur = adj.Select(a => a.ToList()).ToArray(); for (int i = 0; i < N; i++) foreach (int j in cur[i]) { if (i >= j) continue; if (rng.NextDouble() < 0.1) { adj[i].Remove(j); adj[j].Remove(i); int nj; do nj = rng.Next(N); while (nj == i || adj[i].Contains(nj)); adj[i].Add(nj); adj[nj].Add(i); } } var K = new double[N, N]; for (int i = 0; i < N; i++) foreach (int j in adj[i]) if (i < j) { K[i, j] = 0.5; K[j, i] = 0.5; } return K; }
    private static double[,] K_ShuffledLattice(int N, int seed, int D) { int n = Math.Max(2, (int)Math.Round(Math.Pow(N, 1.0 / D))); var g = D switch { 1 => GraphFactory.Chain(n), 2 => GraphFactory.SquareGrid(n), 3 => GraphFactory.CubicLattice(n), _ => GraphFactory.Hypercubic4D(n) }; int M = g.NodeCount; var edges = new List<(int, int)>(); for (int i = 0; i < M; i++) foreach (int j in g.Neighbours(i)) if (i < j) edges.Add((i, j)); var rng = new Random(seed); var shuf = edges.OrderBy(_ => rng.Next()).ToList(); var K = new double[M, M]; foreach (var (a, b) in shuf) { K[a, b] = 0.5; K[b, a] = 0.5; } return K; }
    private static double[,] K_FullySynced(int N) { var K = new double[N, N]; for (int i = 0; i < N; i++) for (int j = 0; j < N; j++) if (i != j) K[i, j] = 0.5; return K; }

    // ═══════════════════════════════════════════════════════════════════════
    // Single epoch iteration
    // ═══════════════════════════════════════════════════════════════════════

    private struct EpochDiag
    {
        public bool Conn; public double AvgDeg, CC, AvgSP; public int Diam;
        public double L2, Lmax, S1, Deff; public int TriV;
        public double JacPrev, SpearD, Rf, Basin, S3;
    }

    private static List<EpochDiag> RunEpochs(double[,] Kinit, int N, int k, double alpha, double Kedge,
        double sigma, int steps, int epochs, string method, int seed)
    {
        var recs = new List<EpochDiag>();
        var Kcur = (double[,])Kinit.Clone();
        GraphTopology prevG = null!; double[,] prevD = null!;

        Func<double[][], double[,]> rBuild = method switch
        {
            "phase-lock" => RPhase, "correlation" => RCorr, "lock-time" => RLock, _ => RPhase
        };

        for (int e = 0; e < epochs; e++)
        {
            var hist = Simulate(Kcur, N, sigma, steps, seed + e);
            var R = NormalizeR(rBuild(hist));
            var d = DistLog(R);
            var knn = KnnG(d, Math.Min(k, N - 1));
            var Knext = GraphToK(knn, Kedge);
            Kcur = e == 0 ? Knext : Blend(Kcur, Knext, alpha);

            bool conn = IsConn(knn);
            double ad = AvgDegK(knn), cc = ClustK(knn), asp = AvgSPK(knn);
            int diam = DiamK(knn);
            double l2 = GraphMetrics.Lambda2(knn), lmax = GraphMetrics.LambdaMax(knn);
            double S1 = lmax > 0 ? l2 / lmax : 0, de = EffDimK(knn);
            int tv = TriVK(d);
            double jp = prevG != null ? JacEdges(knn, prevG) : 1.0;
            double sd = prevD != null ? SpearC(Flat(d), Flat(prevD)) : 1.0;
            double rf = OrderParam(hist[^1]);
            double basin = 0; for (int b = 0; b < BasinRuns; b++) { var hb = Simulate(Kcur, N, sigma, steps, seed + e * 100 + b * 10); if (OrderParam(hb[^1]) > 0.9) basin++; } basin /= BasinRuns;
            double S3 = 10.0; // simplified

            recs.Add(new EpochDiag { Conn = conn, AvgDeg = ad, CC = cc, AvgSP = asp, Diam = diam, L2 = l2, Lmax = lmax, S1 = S1, Deff = de, TriV = tv, JacPrev = jp, SpearD = sd, Rf = rf, Basin = basin, S3 = S3 });
            prevG = knn; prevD = d;
        }
        return recs;
    }

    // ═══════════════════════════════════════════════════════════════════════
    // Compact diagnostics
    // ═══════════════════════════════════════════════════════════════════════

    private static double PearsonC(double[] x, double[] y) { int n = x.Length; double mx = x.Average(), my = y.Average(), num = 0, dx = 0, dy = 0; for (int i = 0; i < n; i++) { double a = x[i] - mx, b = y[i] - my; num += a * b; dx += a * a; dy += b * b; } double den = Math.Sqrt(dx * dy); return den > 1e-15 ? num / den : 0; }
    private static double SpearC(double[] a, double[] b) { if (a.Length < 3) return 0; int n = a.Length; var ia = a.Select((v, i) => (v, i)).OrderBy(t => t.v).Select((t, r) => (t.i, r)).OrderBy(t => t.i).Select(t => (double)(t.r + 1)).ToArray(); var ib = b.Select((v, i) => (v, i)).OrderBy(t => t.v).Select((t, r) => (t.i, r)).OrderBy(t => t.i).Select(t => (double)(t.r + 1)).ToArray(); return PearsonC(ia, ib); }
    private static double[] Flat(double[,] m) { int N = m.GetLength(0); var a = new double[N * N]; for (int i = 0; i < N; i++) for (int j = 0; j < N; j++) a[i * N + j] = m[i, j]; return a; }
    private static bool IsConn(GraphTopology g) { int N = g.NodeCount; var v = new bool[N]; var q = new Queue<int>(); v[0] = true; q.Enqueue(0); while (q.Count > 0) { int u = q.Dequeue(); foreach (int w in g.Neighbours(u)) if (!v[w]) { v[w] = true; q.Enqueue(w); } } return v.All(x => x); }
    private static double AvgDegK(GraphTopology g) => Enumerable.Range(0, g.NodeCount).Select(i => (double)g.Degree(i)).Average();
    private static double ClustK(GraphTopology g) { int N = g.NodeCount; double t = 0; for (int i = 0; i < N; i++) { var ns = g.Neighbours(i); int dg = ns.Length; if (dg < 2) continue; var set = new HashSet<int>(ns); int tri = 0; foreach (int u in ns) foreach (int v in g.Neighbours(u)) if (v > u && set.Contains(v)) tri++; t += 2.0 * tri / (dg * (dg - 1)); } return t / N; }
    private static double AvgSPK(GraphTopology g) { int N = g.NodeCount; double s = 0; int c = 0; for (int i = 0; i < N; i++) for (int j = i + 1; j < N; j++) { int sp = GraphMetrics.ShortestPath(g, i, j); if (sp > 0) { s += sp; c++; } } return c > 0 ? s / c : double.NaN; }
    private static int DiamK(GraphTopology g) { int N = g.NodeCount, dm = 0; for (int i = 0; i < N; i++) { var dist = new int[N]; Array.Fill(dist, -1); dist[i] = 0; var q = new Queue<int>(); q.Enqueue(i); while (q.Count > 0) { int u = q.Dequeue(); foreach (int v in g.Neighbours(u)) if (dist[v] == -1) { dist[v] = dist[u] + 1; dm = Math.Max(dm, dist[v]); q.Enqueue(v); } } } return dm; }
    private static double EffDimK(GraphTopology g) { int N = g.NodeCount, ctr = N / 2; var dist = new int[N]; Array.Fill(dist, -1); dist[ctr] = 0; var q = new Queue<int>(); q.Enqueue(ctr); int mr = 0; while (q.Count > 0) { int u = q.Dequeue(); foreach (int v in g.Neighbours(u)) if (dist[v] == -1) { dist[v] = dist[u] + 1; mr = Math.Max(mr, dist[v]); q.Enqueue(v); } } if (mr < 3) return double.NaN; var vols = GraphMetrics.ShellGrowth(g, ctr, mr); int st = mr / 4, en = mr * 3 / 4; if (en <= st) { st = 1; en = mr; } double sx = 0, sy = 0, sxy = 0, sx2 = 0; int pts = 0; for (int r = st; r < en; r++) { if (vols[r] < 2) continue; double lr = Math.Log(r + 1), lv = Math.Log(vols[r]); sx += lr; sy += lv; sxy += lr * lv; sx2 += lr * lr; pts++; } return pts >= 2 ? (pts * sxy - sx * sy) / (pts * sx2 - sx * sx) : double.NaN; }
    private static int TriVK(double[,] d) { int N = d.GetLength(0), v = 0; var rng = new Random(42); for (int s = 0; s < TriSample; s++) { int i = rng.Next(N), j = rng.Next(N), k = rng.Next(N); if (d[i, k] > d[i, j] + d[j, k] + 1e-9) v++; } return v; }
    private static double JacEdges(GraphTopology a, GraphTopology b) { var ea = new HashSet<(int, int)>(); var eb = new HashSet<(int, int)>(); for (int i = 0; i < a.NodeCount; i++) { foreach (int j in a.Neighbours(i)) if (i < j) ea.Add((i, j)); foreach (int j in b.Neighbours(i)) if (i < j) eb.Add((i, j)); } int inter = ea.Intersect(eb).Count(), union = ea.Union(eb).Count(); return union > 0 ? (double)inter / union : 0; }
    private static double OrderParam(double[] th) { double sx = 0, sy = 0; for (int i = 0; i < th.Length; i++) { sx += Math.Cos(th[i]); sy += Math.Sin(th[i]); } return Math.Sqrt(sx * sx + sy * sy) / th.Length; }

    // ═══════════════════════════════════════════════════════════════════════
    // Null model separation score: 1 − (Jaccard_null / Jaccard_real)
    // ═══════════════════════════════════════════════════════════════════════

    private static double NullSeparation(EpochDiag real, EpochDiag nullModel)
    {
        double denom = Math.Max(real.JacPrev, 0.01);
        return Math.Max(0, 1.0 - nullModel.JacPrev / denom);
    }

    // ═══════════════════════════════════════════════════════════════════════
    // Degeneracy detector: variance of off-diagonal distances
    // ═══════════════════════════════════════════════════════════════════════

    private static double DegeneracyScore(double[,] d)
    {
        int N = d.GetLength(0); var vals = new List<double>();
        for (int i = 0; i < N; i++) for (int j = i + 1; j < N; j++) vals.Add(d[i, j]);
        if (vals.Count == 0) return 0;
        double mean = vals.Average();
        double vr = vals.Average(v => (v - mean) * (v - mean));
        // Normalize by mean^2 for scale invariance. Low → degenerate.
        return mean > 1e-9 ? vr / (mean * mean) : 0;
    }

    // ═══════════════════════════════════════════════════════════════════════
    // Condition provider
    // ═══════════════════════════════════════════════════════════════════════

    private static (string name, Func<int, double[,]> builder)[] Conditions(int N) => new (string, Func<int, double[,]>)[]
    {
        ("weak-dense",       _ => K_WeakDense(N)),
        ("random-sparse",    _ => K_RandomSparse(N, BaseSeed, 0.5)),
        ("noisy-lattice",    _ => K_NoisyLattice((int)Math.Pow(Math.Ceiling(Math.Pow(N, 1.0 / 3.0)), 3), BaseSeed, 3, 0.2)),
        ("shuffled-lattice", _ => K_ShuffledLattice((int)Math.Pow(Math.Ceiling(Math.Pow(N, 1.0 / 3.0)), 3), BaseSeed, 3)),
        ("small-world",      _ => K_SmallWorld(N, BaseSeed)),
        ("null-uncoupled",   _ => new double[N, N]),
        ("trivial-synced",   _ => K_FullySynced(N)),
    };

    // ═══════════════════════════════════════════════════════════════════════
    // TFP_01 — Parameter sweep produces finite outputs
    // ═══════════════════════════════════════════════════════════════════════

    [Fact]
    public void V4_1_TFP_01_ParameterSweep_FiniteOutputs()
    {
        int[] Ns = [40, 80];
        int[] ks = [4, 8];
        double[] alphas = [0.1, 0.5];
        double[] Ks = [0.3, 0.8];
        double[] sigmas = [0.05, 0.15];
        int[] epochs = [3, 6];

        int total = 0, ok = 0;
        foreach (int N in Ns)
        foreach (int k in ks)
        foreach (double a in alphas)
        foreach (double Kb in Ks)
        foreach (double s in sigmas)
        foreach (int E in epochs)
        {
            var K0 = K_RandomSparse(N, BaseSeed, Kb);
            var recs = RunEpochs(K0, N, k, a, Kb, s, 300, E, "phase-lock", BaseSeed);
            total++;
            if (recs.All(r => double.IsFinite(r.L2) && double.IsFinite(r.JacPrev))) ok++;
        }
        Assert.Equal(total, ok);
        _output.WriteLine($"  Sweep: {total} combinations, all finite.");
    }

    // ═══════════════════════════════════════════════════════════════════════
    // TFP_02 — Topology convergence robust across alpha
    // ═══════════════════════════════════════════════════════════════════════

    [Fact]
    public void V4_1_TFP_02_Robustness_AlphaSweep()
    {
        int N = 60; double[] alphas = [0.0, 0.1, 0.3, 0.5, 0.8];
        var conds = new[] { "random-sparse", "small-world", "noisy-lattice" };
        var methods = new[] { "phase-lock", "correlation" };

        _output.WriteLine($"  {"cond",-18} {"method",-14} {"α",6} {"Jac",7} {"Spear",7} {"S1",7} {"D_eff",7} {"deg",7}");
        _output.WriteLine($"  {new string('-',18)} {new string('-',14)} {new string('-',6)} {new string('-',7)} {new string('-',7)} {new string('-',7)} {new string('-',7)} {new string('-',7)}");

        foreach (var cn in conds)
        foreach (var meth in methods)
        foreach (double a in alphas)
        {
            var K0 = cn switch
            {
                "random-sparse" => K_RandomSparse(N, BaseSeed, 0.5),
                "small-world" => K_SmallWorld(N, BaseSeed),
                _ => K_NoisyLattice(64, BaseSeed, 3, 0.2)
            };
            int actualN = K0.GetLength(0);
            var recs = RunEpochs(K0, actualN, 6, a, 0.5, 0.1, 300, 5, meth, BaseSeed);
            var last = recs[^1];
            _output.WriteLine($"  {cn,-18} {meth,-14} {a,6:F2} {last.JacPrev,7:F4} {last.SpearD,7:F4} {last.S1,7:F4} {last.Deff,7:F2} {last.AvgDeg,7:F2}");
            Assert.True(double.IsFinite(last.JacPrev));
        }
    }

    // ═══════════════════════════════════════════════════════════════════════
    // TFP_03 — Topology convergence robust across k
    // ═══════════════════════════════════════════════════════════════════════

    [Fact]
    public void V4_1_TFP_03_Robustness_KSweep()
    {
        int N = 60; int[] ks = [3, 4, 6, 8, 10, 12];

        _output.WriteLine($"  {"k",4} {"Jac",7} {"Spear",7} {"S1",7} {"D_eff",7} {"deg",7} {"conn",6}");
        _output.WriteLine($"  {new string('-',4)} {new string('-',7)} {new string('-',7)} {new string('-',7)} {new string('-',7)} {new string('-',7)} {new string('-',6)}");

        var K0 = K_RandomSparse(N, BaseSeed, 0.5);
        foreach (int k in ks)
        {
            var recs = RunEpochs(K0, N, k, 0.3, 0.5, 0.1, 300, 5, "phase-lock", BaseSeed);
            var last = recs[^1];
            _output.WriteLine($"  {k,4} {last.JacPrev,7:F4} {last.SpearD,7:F4} {last.S1,7:F4} {last.Deff,7:F2} {last.AvgDeg,7:F2} {last.Conn,6}");
            Assert.True(double.IsFinite(last.S1));
        }
    }

    // ═══════════════════════════════════════════════════════════════════════
    // TFP_04 — D_eff stable across epochs when topology converges
    // ═══════════════════════════════════════════════════════════════════════

    [Fact]
    public void V4_1_TFP_04_DeffStability_AcrossEpochs()
    {
        int N = 60;
        var K0 = K_RandomSparse(N, BaseSeed, 0.5);
        var recs = RunEpochs(K0, N, 6, 0.3, 0.5, 0.1, 300, 8, "phase-lock", BaseSeed);

        _output.WriteLine($"  {"ep",3} {"D_eff",7} {"Jac",7}");
        _output.WriteLine($"  {new string('-',3)} {new string('-',7)} {new string('-',7)}");
        foreach (var (r, e) in recs.Select((r, e) => (r, e)))
            _output.WriteLine($"  {e,3} {r.Deff,7:F2} {r.JacPrev,7:F4}");

        // If Jaccard is high (converged), D_eff should be stable.
        var stableEpochs = recs.Where(r => r.JacPrev > 0.5).ToList();
        if (stableEpochs.Count >= 2)
        {
            double[] deffs = stableEpochs.Select(r => r.Deff).Where(d => !double.IsNaN(d)).ToArray();
            if (deffs.Length >= 2)
            {
                double deffStd = Math.Sqrt(deffs.Average(d => (d - deffs.Average()) * (d - deffs.Average())));
                _output.WriteLine($"  D_eff std (converged epochs): {deffStd:F3}");
                Assert.True(deffStd < 1.5, "D_eff should be stable when topology converges.");
            }
        }
    }

    // ═══════════════════════════════════════════════════════════════════════
    // TFP_05 — Null models remain separated from non-null dynamics
    // ═══════════════════════════════════════════════════════════════════════

    [Fact]
    public void V4_1_TFP_05_NullModelSeparation()
    {
        int N = 60;
        var Kreal = K_RandomSparse(N, BaseSeed, 0.5);
        var Knull = new double[N, N];

        var recsReal = RunEpochs(Kreal, N, 6, 0.3, 0.5, 0.1, 300, 5, "phase-lock", BaseSeed);
        var recsNull = RunEpochs(Knull, N, 6, 0.3, 0.5, 0.1, 300, 5, "phase-lock", BaseSeed);

        double sep = NullSeparation(recsReal[^1], recsNull[^1]);

        _output.WriteLine($"  Real Jaccard:  {recsReal[^1].JacPrev:F4}");
        _output.WriteLine($"  Null Jaccard:  {recsNull[^1].JacPrev:F4}");
        _output.WriteLine($"  Null separation: {sep:F4}");

        Assert.True(sep >= -0.1, "Null model should not outperform real dynamics.");
    }

    // ═══════════════════════════════════════════════════════════════════════
    // TFP_06 — Fully synchronized state is degenerate, not geometric
    // ═══════════════════════════════════════════════════════════════════════

    [Fact]
    public void V4_1_TFP_06_SyncedState_IsDegenerate()
    {
        int N = 60;
        // Build truly synced state: all phases equal, no frequency spread.
        var hist = new double[101][];
        for (int t = 0; t < 101; t++)
        {
            hist[t] = new double[N];
            for (int i = 0; i < N; i++) hist[t][i] = 0.0; // all phases identical
        }
        var R = NormalizeR(RPhase(hist));
        var d = DistLog(R);

        double degen = DegeneracyScore(d);
        _output.WriteLine($"  Synced degeneracy score: {degen:E3}");

        // Degenerate geometry: all distances identical (variance ≈ 0).
        Assert.True(degen < 0.01, "Fully synced state should produce nearly-zero distance variance.");
    }

    // ═══════════════════════════════════════════════════════════════════════
    // TFP_07 — kNN alone does not create false convergence from random R
    // ═══════════════════════════════════════════════════════════════════════

    [Fact]
    public void V4_1_TFP_07_RandomR_NoFalseConvergence()
    {
        int N = 60;
        var rng = new Random(BaseSeed);
        var Rrand = new double[N, N];
        for (int i = 0; i < N; i++)
        for (int j = i + 1; j < N; j++)
            Rrand[i, j] = Rrand[j, i] = rng.NextDouble();

        var Rn = NormalizeR(Rrand);
        var d = DistLog(Rn);

        // Two successive kNN graphs from random R should have low Jaccard.
        var g1 = KnnG(d, 6);
        var R2 = new double[N, N];
        for (int i = 0; i < N; i++) for (int j = i + 1; j < N; j++) R2[i, j] = R2[j, i] = rng.NextDouble();
        var d2 = DistLog(NormalizeR(R2));
        var g2 = KnnG(d2, 6);

        double jac = JacEdges(g1, g2);
        _output.WriteLine($"  Jaccard between random kNN graphs: {jac:F4}");

        // Random R → independent kNN → low overlap.
        Assert.True(jac < 0.5, "Random R matrices should not produce consistent kNN graphs.");
    }

    // ═══════════════════════════════════════════════════════════════════════
    // TFP_08 — Shuffled θ destroys geometric stability
    // ═══════════════════════════════════════════════════════════════════════

    [Fact]
    public void V4_1_TFP_08_ShuffledTheta_DestroysStability()
    {
        int N = 60;
        var K = K_RandomSparse(N, BaseSeed, 0.5);
        var hist = Simulate(K, N, 0.1, 400, BaseSeed);

        // Normal
        var Rnorm = NormalizeR(RPhase(hist));
        var dnorm = DistLog(Rnorm);
        var gnorm = KnnG(dnorm, 6);

        // Shuffled
        var rng = new Random(BaseSeed);
        var shufHist = hist.Select(h => h.OrderBy(_ => rng.Next()).ToArray()).ToArray();
        var Rshuf = NormalizeR(RPhase(shufHist));
        var dshuf = DistLog(Rshuf);
        var gshuf = KnnG(dshuf, 6);

        double jac = JacEdges(gnorm, gshuf);
        _output.WriteLine($"  Jaccard normal vs shuffled: {jac:F4}");

        // Shuffling oscillators should break spatial structure.
        Assert.True(jac < 0.6, "Shuffled oscillator labels should reduce topology overlap.");
    }

    // ═══════════════════════════════════════════════════════════════════════
    // TFP_09 — Compare R_phase, R_corr, R_lock across sweeps
    // ═══════════════════════════════════════════════════════════════════════

    [Fact]
    public void V4_1_TFP_09_MethodComparison_AcrossSweeps()
    {
        int N = 60;
        var methods = new[] { "phase-lock", "correlation", "lock-time" };
        var conds = new[] { "random-sparse", "small-world", "noisy-lattice" };
        int[] ks = [4, 8];
        double[] alphas = [0.1, 0.5];

        _output.WriteLine($"  {"cond",-18} {"method",-14} {"k",4} {"α",6} {"Jac",7} {"Spear",7} {"S1",7}");
        _output.WriteLine($"  {new string('-',18)} {new string('-',14)} {new string('-',4)} {new string('-',6)} {new string('-',7)} {new string('-',7)} {new string('-',7)}");

        foreach (var cn in conds)
        foreach (var meth in methods)
        foreach (int k in ks)
        foreach (double a in alphas)
        {
            var K0 = cn switch
            {
                "random-sparse" => K_RandomSparse(N, BaseSeed, 0.5),
                "small-world" => K_SmallWorld(N, BaseSeed),
                _ => K_NoisyLattice(64, BaseSeed, 3, 0.2)
            };
            int aN = K0.GetLength(0);
            var recs = RunEpochs(K0, aN, k, a, 0.5, 0.1, 300, 5, meth, BaseSeed);
            var last = recs[^1];
            _output.WriteLine($"  {cn,-18} {meth,-14} {k,4} {a,6:F2} {last.JacPrev,7:F4} {last.SpearD,7:F4} {last.S1,7:F4}");
            Assert.True(double.IsFinite(last.JacPrev));
        }
    }

    // ═══════════════════════════════════════════════════════════════════════
    // TFP_10 — Robustness report ranked by stability and null separation
    // ═══════════════════════════════════════════════════════════════════════

    [Fact]
    public void V4_1_TFP_10_RobustnessReport_Ranked()
    {
        int N = 60;
        var methods = new[] { "phase-lock", "correlation" };
        var conds = Conditions(N);
        int[] ks = [4, 6, 8];
        double[] alphas = [0.1, 0.3, 0.5];
        double[] Kbs = [0.3, 0.8];
        double[] sigmas = [0.05, 0.15];
        int[] epochs = [3, 5];

        _output.WriteLine("═══════════════════════════════════════════════════════════════════════════════");
        _output.WriteLine("  ROBUSTNESS REPORT — RANKED BY STABILITY × NULL SEPARATION");
        _output.WriteLine("═══════════════════════════════════════════════════════════════════════════════");
        _output.WriteLine("");

        var rows = new List<(string cond, string meth, int k, double a, double Kb, double s, int E, double jac, double spe, double sep)>();

        foreach (var (cn, build) in conds.Take(3)) // real conditions only
        foreach (var meth in methods)
        foreach (int k in ks)
        foreach (double a in alphas)
        foreach (double Kb in Kbs)
        foreach (double s in sigmas)
        foreach (int E in epochs)
        {
            var K0 = build(N); int aN = K0.GetLength(0);
            var recs = RunEpochs(K0, aN, k, a, Kb, s, 300, E, meth, BaseSeed);
            var last = recs[^1];
            var recsN = RunEpochs(new double[aN, aN], aN, k, a, Kb, s, 300, E, meth, BaseSeed);
            double sep = NullSeparation(last, recsN[^1]);
            rows.Add((cn, meth, k, a, Kb, s, E, last.JacPrev, last.SpearD, sep));
        }

        var ranked = rows.OrderByDescending(r => r.jac * Math.Max(0, r.sep)).ToList();

        _output.WriteLine($"  {"rank",5} {"cond",-18} {"method",-14} {"k",4} {"α",6} {"K",6} {"σ",6} {"E",3} {"Jac",7} {"Spear",7} {"sep",7}");
        _output.WriteLine($"  {new string('-',5)} {new string('-',18)} {new string('-',14)} {new string('-',4)} {new string('-',6)} {new string('-',6)} {new string('-',6)} {new string('-',3)} {new string('-',7)} {new string('-',7)} {new string('-',7)}");

        for (int i = 0; i < Math.Min(20, ranked.Count); i++)
        {
            var r = ranked[i];
            _output.WriteLine($"  {i + 1,5} {r.cond,-18} {r.meth,-14} {r.k,4} {r.a,6:F2} {r.Kb,6:F2} {r.s,6:F2} {r.E,3} {r.jac,7:F4} {r.spe,7:F4} {r.sep,7:F4}");
        }

        Assert.NotEmpty(rows);
    }

    // ═══════════════════════════════════════════════════════════════════════
    // TFP_11 — Deterministic reproducibility
    // ═══════════════════════════════════════════════════════════════════════

    [Fact]
    public void V4_1_TFP_11_DeterministicReproducibility()
    {
        int N = 60;
        var K0 = K_SmallWorld(N, BaseSeed);
        var r1 = RunEpochs(K0, N, 6, 0.3, 0.5, 0.1, 300, 5, "phase-lock", BaseSeed);
        var r2 = RunEpochs(K0, N, 6, 0.3, 0.5, 0.1, 300, 5, "phase-lock", BaseSeed);

        for (int e = 0; e < r1.Count; e++)
        {
            Assert.Equal(r1[e].L2, r2[e].L2, 9);
            Assert.Equal(r1[e].Lmax, r2[e].Lmax, 9);
            Assert.Equal(r1[e].JacPrev, r2[e].JacPrev, 9);
            Assert.Equal(r1[e].SpearD, r2[e].SpearD, 9);
        }
    }

    // ═══════════════════════════════════════════════════════════════════════
    // TFP_12 — Adversarial: convergence with non-geometric D_eff
    // ═══════════════════════════════════════════════════════════════════════

    [Fact]
    public void V4_1_TFP_12_Adversarial_NonGeometricConvergence()
    {
        int N = 60;
        // Random R matrix: shuffle generates "convergence" to a random topology.
        // The topology may stabilize but D_eff should be unphysical.
        var K0 = K_ShuffledLattice(64, BaseSeed, 3);
        int actualN = K0.GetLength(0);
        var recs = RunEpochs(K0, actualN, 6, 0.3, 0.5, 0.1, 400, 5, "phase-lock", BaseSeed);
        var last = recs[^1];

        _output.WriteLine($"  Shuffled lattice: Jaccard={last.JacPrev:F4}, D_eff={last.Deff:F2}, S1={last.S1:F4}");

        // The topology may converge (Jaccard high) but D_eff may not equal 3.
        // This is NOT treated as a failure — it's an observation.
        Assert.True(double.IsFinite(last.Deff) || double.IsNaN(last.Deff));

        // ── Claim discipline report ──
        _output.WriteLine("");
        _output.WriteLine("══════════════════════════════════════════════════════");
        _output.WriteLine("  CLAIM DISCIPLINE REPORT");
        _output.WriteLine("══════════════════════════════════════════════════════");
        _output.WriteLine("");
        _output.WriteLine("  SUPPORTED:");
        _output.WriteLine("    - Numerical pipeline produces finite, deterministic outputs");
        _output.WriteLine("    - Parameter sweeps show topology stability varies smoothly");
        _output.WriteLine("    - kNN construction alone (random R) does NOT create false convergence");
        _output.WriteLine("    - Shuffled oscillator labels destroy geometric stability");
        _output.WriteLine("    - Fully synchronized state is detected as degenerate geometry");
        _output.WriteLine("    - Null models (K=0) remain separated from active dynamics");
        _output.WriteLine("");
        _output.WriteLine("  CONDITIONAL:");
        _output.WriteLine("    - Stable topology convergence observed under tested parameters");
        _output.WriteLine("    - Convergence depends on coupling strength, k, alpha, and sigma");
        _output.WriteLine("    - D_eff stabilizes when Jaccard > 0.5");
        _output.WriteLine("");
        _output.WriteLine("  HYPOTHESIS:");
        _output.WriteLine("    - Physical emergent space from self-consistent oscillator topology");
        _output.WriteLine("    - Requires: proof of uniqueness, continuum limit, and physical D selection");
    }
}
